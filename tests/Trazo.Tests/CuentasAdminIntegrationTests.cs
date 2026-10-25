using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Trazo.Web.Domain;
using Trazo.Web.Models;

namespace Trazo.Tests;

[Collection("mysql")]
public class CuentasAdminIntegrationTests(MySqlApp app) : IClassFixture<MySqlApp>
{
    private async Task<HttpClient> Cuenta(string name = "admin", bool csrf = true)
    {
        var c = app.Browser(); await MySqlApp.Login(c, name + "@trazo.test");
        if (csrf) c.DefaultRequestHeaders.Add("X-CSRF-TOKEN", await MySqlApp.Token(c, "/panel"));
        return c;
    }
    private Task<Usuario> User(string name = "bruno") => app.Db(db => db.Users.SingleAsync(u => u.UserName == name));
    private static Task<HttpResponseMessage> Post(HttpClient c, string url, params (string Key, string Value)[] data) =>
        c.PostAsync(url, new FormUrlEncodedContent(data.Select(v => new KeyValuePair<string, string>(v.Key, v.Value))));
    private static Task<HttpResponseMessage> Cambiar(HttpClient c, Usuario u, int state = 1, string reason = "Decisión administrativa de prueba.") =>
        Post(c, $"/administracion/cuentas/{u.Id}/estado", ("Version", u.ConcurrencyStamp!), ("EstadoNuevo", state.ToString()),
            ("Motivo", reason), ("AdministradorId", "falso"), ("UsuarioId", "falso"), ("SecurityStamp", "falso"));
    private Task Limpiar(string id) => app.Db(async db => {
        await db.CambiosEstadoCuenta.Where(c => c.UsuarioId == id).ExecuteDeleteAsync();
        return await db.Users.Where(u => u.Id == id).ExecuteUpdateAsync(s => s.SetProperty(u => u.Estado, EstadoCuenta.Activa));
    });
    private static Task<HttpResponseMessage> Emitir(HttpClient c) => c.PostAsJsonAsync("/api/v1/auth/token", new { Email = "bruno@trazo.test", Password = "Trazo.Demo2026!" });

    [MySqlFact]
    public async Task Suspender_y_reactivar_revoca_cookies_y_jwt_sin_resucitar_sesiones()
    {
        using var admin = await Cuenta(); using var active = await Cuenta("bruno"); using var untouched = await Cuenta("bruno");
        using var ana = await Cuenta("ana"); using var anon = app.Browser(); using var api = app.Browser();
        var user = await User();
        var token = (await (await Emitir(api)).Content.ReadFromJsonAsync<TokenRespuesta>())!;
        api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        var post = await app.Db(db => db.Publicaciones.Where(p => p.AutorId == user.Id).Select(p => p.Id).FirstAsync());
        try {
            Assert.Equal(HttpStatusCode.Redirect, (await Post(ana, $"/favoritos/{post}/guardar")).StatusCode);
            Assert.Equal(HttpStatusCode.Redirect, (await Cambiar(admin, user)).StatusCode);
            var suspended = await User();
            Assert.Equal(EstadoCuenta.Suspendida, suspended.Estado);
            Assert.NotEqual(user.SecurityStamp, suspended.SecurityStamp); Assert.NotEqual(user.ConcurrencyStamp, suspended.ConcurrencyStamp);
            Assert.Equal(HttpStatusCode.Redirect, (await active.GetAsync("/panel")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await api.GetAsync("/api/v1/publicaciones")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await Emitir(anon)).StatusCode);
            foreach (var url in new[] { "/p/bruno", $"/publicaciones/{post}" }) Assert.Equal(HttpStatusCode.NotFound, (await anon.GetAsync(url)).StatusCode);
            Assert.DoesNotContain($"href=\"/publicaciones/{post}\"", await ana.GetStringAsync("/mis-favoritos"));
            using var login = app.Browser();
            var csrf = await MySqlApp.Token(login, "/cuenta/ingresar");
            var blocked = await Post(login, "/cuenta/ingresar", ("Email", "bruno@trazo.test"), ("Password", "Trazo.Demo2026!"), ("__RequestVerificationToken", csrf));
            Assert.Equal(HttpStatusCode.OK, blocked.StatusCode); Assert.Contains("No pudimos iniciar", await blocked.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.Redirect, (await Cambiar(admin, suspended, 0)).StatusCode);
            // Esta cookie no se usó durante la suspensión: debe seguir invalidada al reactivar.
            Assert.Equal(HttpStatusCode.Redirect, (await untouched.GetAsync("/panel")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await api.GetAsync("/api/v1/publicaciones")).StatusCode);
            await MySqlApp.Login(login, "bruno@trazo.test"); Assert.Equal(HttpStatusCode.OK, (await login.GetAsync("/panel")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await Emitir(anon)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await anon.GetAsync($"/publicaciones/{post}")).StatusCode);
            var history = await app.Db(db => db.CambiosEstadoCuenta.Include(c => c.Administrador).Where(c => c.UsuarioId == user.Id).OrderBy(c => c.Id).ToListAsync());
            Assert.Equal(2, history.Count); Assert.All(history, h => Assert.Equal("admin", h.Administrador.UserName));
            Assert.Equal(EstadoCuenta.Suspendida, history[0].EstadoNuevo); Assert.Equal(EstadoCuenta.Activa, history[1].EstadoNuevo);
        } finally {
            await Post(ana, $"/favoritos/{post}/quitar"); await Limpiar(user.Id);
        }
    }

    [MySqlFact]
    public async Task Rol_csrf_validaciones_y_metodo_protegen_los_cambios()
    {
        using var admin = await Cuenta(); using var creator = await Cuenta("ana"); using var noCsrf = await Cuenta("admin", false); using var anon = app.Browser();
        var user = await User();
        try {
            Assert.Equal(HttpStatusCode.Redirect, (await anon.GetAsync("/administracion/cuentas")).StatusCode);
            var denied = await creator.GetAsync("/administracion/cuentas"); Assert.Contains("sin-acceso", denied.Headers.Location!.ToString());
            Assert.Equal(HttpStatusCode.Redirect, (await Cambiar(creator, user)).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await Cambiar(noCsrf, user)).StatusCode);
            Assert.Equal(HttpStatusCode.MethodNotAllowed, (await admin.GetAsync($"/administracion/cuentas/{user.Id}/estado")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await Cambiar(admin, user, 99)).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await Cambiar(admin, user, reason: "          ")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await Cambiar(admin, user, reason: new string('x', 1001))).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/administracion/cuentas?estado=99")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/administracion/cuentas?pagina=no")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/administracion/cuentas?q=" + new string('x', 101))).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/administracion/cuentas/no-existe")).StatusCode);
            Assert.Equal(HttpStatusCode.Redirect, (await Cambiar(admin, user, reason: "<script>alert('motivo')</script>")).StatusCode);
            var detail = await admin.GetStringAsync($"/administracion/cuentas/{user.Id}");
            Assert.Contains("&lt;script&gt;", detail); Assert.DoesNotContain("<script>alert", detail);
            Assert.DoesNotContain(user.Email!, detail); Assert.DoesNotContain(user.SecurityStamp!, detail);
        } finally { await Limpiar(user.Id); }
    }

    [MySqlFact]
    public async Task Cuentas_administradoras_y_propia_no_pueden_suspendese()
    {
        using var admin = await Cuenta(); var self = await User("admin"); var other = await User("ana");
        var role = await app.Db(db => db.Roles.Where(r => r.Name == Roles.Administrador).Select(r => r.Id).SingleAsync());
        await app.Db(async db => { db.UserRoles.Add(new IdentityUserRole<string> { UserId = other.Id, RoleId = role }); return await db.SaveChangesAsync(); });
        try {
            foreach (var u in new[] { self, other }) {
                Assert.Equal(HttpStatusCode.BadRequest, (await Cambiar(admin, u)).StatusCode);
                Assert.Contains("están protegidas", WebUtility.HtmlDecode(await admin.GetStringAsync($"/administracion/cuentas/{u.Id}")));
                Assert.Equal(EstadoCuenta.Activa, (await User(u.UserName!)).Estado);
                Assert.False(await app.Db(db => db.CambiosEstadoCuenta.AnyAsync(c => c.UsuarioId == u.Id)));
            }
        } finally { await app.Db(db => db.UserRoles.Where(r => r.UserId == other.Id && r.RoleId == role).ExecuteDeleteAsync()); }
    }

    [MySqlFact]
    public async Task Concurrencia_y_reenvios_conservan_una_decision_atomica()
    {
        using var admin = await Cuenta(); var user = await User();
        try {
            var results = await Task.WhenAll(Cambiar(admin, user), Cambiar(admin, user));
            Assert.Single(results, r => r.StatusCode == HttpStatusCode.Redirect); Assert.Single(results, r => r.StatusCode == HttpStatusCode.Conflict);
            Assert.Equal(1, await app.Db(db => db.CambiosEstadoCuenta.CountAsync(c => c.UsuarioId == user.Id)));
            var changed = await User();
            Assert.Equal(HttpStatusCode.Conflict, (await Cambiar(admin, user, 0)).StatusCode);
            Assert.Equal(changed.SecurityStamp, (await User()).SecurityStamp);
            Assert.Equal(HttpStatusCode.Redirect, (await Cambiar(admin, changed, 0)).StatusCode);
            Assert.Equal(2, await app.Db(db => db.CambiosEstadoCuenta.CountAsync(c => c.UsuarioId == user.Id)));
        } finally { await Limpiar(user.Id); }
    }

    [MySqlFact]
    public async Task Cuentas_e_historial_paginan_en_mysql_y_filtran_antes_de_contar()
    {
        using var admin = await Cuenta(); var user = await User(); var actor = await User("admin"); var prefix = "zzfixture" + Guid.NewGuid().ToString("N")[..5];
        var ids = await app.Db(async db => {
            var users = Enumerable.Range(0, 14).Select(i => new Usuario { UserName = prefix + i, NormalizedUserName = (prefix + i).ToUpperInvariant(), Estado = i == 13 ? EstadoCuenta.Suspendida : EstadoCuenta.Activa }).ToArray();
            db.Users.AddRange(users);
            for (var i = 0; i < 13; i++) db.CambiosEstadoCuenta.Add(new CambioEstadoCuenta { UsuarioId = user.Id, AdministradorId = actor.Id, EstadoAnterior = (EstadoCuenta)(i % 2), EstadoNuevo = (EstadoCuenta)(1 - i % 2), Motivo = "Historial de prueba " + i });
            await db.SaveChangesAsync(); return users.Select(u => u.Id).ToArray();
        });
        try {
            var first = await admin.GetStringAsync($"/administracion/cuentas?q={prefix}&estado=0");
            var second = await admin.GetStringAsync($"/administracion/cuentas?q={prefix}&estado=0&pagina=2");
            Assert.Equal(12, Regex.Matches(first, "class=\"account-card report-card\"").Count); Assert.Single(Regex.Matches(second, "class=\"account-card report-card\""));
            Assert.Contains("13 resultados", first); Assert.Contains("1 resultados", await admin.GetStringAsync($"/administracion/cuentas?q={prefix}&estado=1"));
            first = await admin.GetStringAsync($"/administracion/cuentas/{user.Id}"); second = await admin.GetStringAsync($"/administracion/cuentas/{user.Id}?pagina=2");
            Assert.Equal(12, Regex.Matches(first, "class=\"account-history report-card\"").Count); Assert.Single(Regex.Matches(second, "class=\"account-history report-card\""));
            // Integridad de la tabla de historial, incluso sin pasar por el servicio.
            await Assert.ThrowsAsync<DbUpdateException>(() => app.Db(async db => { db.CambiosEstadoCuenta.Add(new CambioEstadoCuenta { UsuarioId = user.Id, AdministradorId = actor.Id, EstadoAnterior = EstadoCuenta.Activa, EstadoNuevo = EstadoCuenta.Activa }); return await db.SaveChangesAsync(); }));
        } finally { await Limpiar(user.Id); await app.Db(db => db.Users.Where(u => ids.Contains(u.Id)).ExecuteDeleteAsync()); }
    }
}
