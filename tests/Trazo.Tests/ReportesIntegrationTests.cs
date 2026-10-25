using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Trazo.Web.Domain;
using Trazo.Web.Models;
using Trazo.Web.Services;

namespace Trazo.Tests;

[Collection("mysql")]
public class ReportesIntegrationTests(MySqlApp app) : IClassFixture<MySqlApp>
{
    private async Task<HttpClient> Cuenta(string name = "ana", bool csrf = true)
    {
        var c = app.Browser(); await MySqlApp.Login(c, name + "@trazo.test");
        if (csrf) c.DefaultRequestHeaders.Add("X-CSRF-TOKEN", await MySqlApp.Token(c, "/panel"));
        return c;
    }
    private Task<Publicacion> Crear() => app.Db(async db =>
    {
        var p = new Publicacion { AutorId = await db.Users.Where(u => u.UserName == "bruno").Select(u => u.Id).SingleAsync(),
            Titulo = "Revisión de comunidad " + Guid.NewGuid().ToString("N"), Texto = "Contenido de prueba sin datos personales.",
            Tipo = TipoPublicacion.Actualizacion, EstadoEditorial = EstadoEditorial.Publicada, PublicadaUtc = DateTime.UtcNow };
        db.Publicaciones.Add(p); await db.SaveChangesAsync(); return p;
    });
    private Task Limpiar(long id) => app.Db(async db =>
    {
        await db.Reportes.Where(r => r.PublicacionId == id).ExecuteDeleteAsync();
        return await db.Publicaciones.Where(p => p.Id == id).ExecuteDeleteAsync();
    });
    private static Task<HttpResponseMessage> Post(HttpClient c, string url, params (string Key, string Value)[] data) =>
        c.PostAsync(url, new FormUrlEncodedContent(data.Select(v => new KeyValuePair<string, string>(v.Key, v.Value))));
    private static Task<HttpResponseMessage> Denunciar(HttpClient c, long id, string motivo = "Spam o engaño") =>
        Post(c, $"/publicaciones/{id}/reportar", ("Motivo", motivo), ("Detalle", "Descripción suficientemente detallada para su revisión."), ("Estado", "1"), ("ResueltoPorId", "falso"));
    private Task<long> ReportId(long id) => app.Db(db => db.Reportes.Where(r => r.PublicacionId == id).OrderByDescending(r => r.Id).Select(r => r.Id).FirstAsync());
    private Task<string> Version(long id) => app.Db(db => db.Publicaciones.Where(p => p.Id == id).Select(p => p.Version).SingleAsync());
    private static Task<HttpResponseMessage> Resolver(HttpClient c, long id, string version, int decision = 1) =>
        Post(c, $"/administracion/reportes/{id}/resolver", ("Version", version), ("Decision", decision.ToString()), ("Resolucion", "Se revisó el contenido y se registró esta decisión."));

    [MySqlFact]
    public async Task Denuncia_oculta_sin_filtrar_identidad_y_revision_restaura_con_auditoria()
    {
        using var ana = await Cuenta(); using var admin = await Cuenta("admin"); using var bruno = await Cuenta("bruno"); using var anon = app.Browser();
        var p = await Crear();
        try
        {
            Assert.Equal(HttpStatusCode.Redirect, (await Denunciar(ana, p.Id)).StatusCode);
            var id = await ReportId(p.Id);
            Assert.Equal(HttpStatusCode.Redirect, (await Denunciar(ana, p.Id)).StatusCode);
            Assert.Equal(1, await app.Db(db => db.Reportes.CountAsync(r => r.PublicacionId == p.Id)));
            Assert.Equal(HttpStatusCode.OK, (await anon.GetAsync($"/publicaciones/{p.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await bruno.GetAsync($"/mis-reportes/{id}")).StatusCode);
            Assert.Equal(HttpStatusCode.Redirect, (await Resolver(admin, id, await Version(p.Id))).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await anon.GetAsync($"/publicaciones/{p.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/publicaciones/{p.Id}")).StatusCode);
            var authorPage = WebUtility.HtmlDecode(await bruno.GetStringAsync($"/publicaciones/{p.Id}"));
            Assert.Contains("Oculta por moderación", authorPage); Assert.Contains("Se revisó el contenido", authorPage);
            Assert.DoesNotContain("Descripción suficientemente", authorPage); Assert.DoesNotContain("ana@trazo.test", authorPage);
            var stored = await app.Db(db => db.Reportes.Include(r => r.ResueltoPor).SingleAsync(r => r.Id == id));
            Assert.Equal(EstadoReporte.Resuelto, stored.Estado); Assert.Equal("admin", stored.ResueltoPor!.UserName); Assert.NotNull(stored.ResueltoUtc);
            // Editar mediante el panel no elimina la moderación.
            var editor = (await bruno.GetFromJsonAsync<PublicacionEditor>($"/panel-json/publicaciones/{p.Id}"))!;
            Assert.Equal(HttpStatusCode.OK, (await bruno.PutAsJsonAsync($"/panel-json/publicaciones/{p.Id}", new EditarPublicacionModel {
                Version = editor.Version, Titulo = p.Titulo, Texto = p.Texto, Tipo = p.Tipo, EstadoEditorial = EstadoEditorial.Publicada, Visibilidad = Visibilidad.Publica })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await anon.GetAsync($"/publicaciones/{p.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.Redirect, (await Denunciar(bruno, p.Id, ReportesService.Revision)).StatusCode);
            var appeal = await ReportId(p.Id);
            Assert.NotEqual(id, appeal);
            Assert.Equal(HttpStatusCode.Redirect, (await Resolver(admin, appeal, await Version(p.Id), 2)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await anon.GetAsync($"/publicaciones/{p.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await Resolver(admin, id, await Version(p.Id), 0)).StatusCode);
        }
        finally { await Limpiar(p.Id); }
    }

    [MySqlFact]
    public async Task Permisos_csrf_motivos_y_campos_privilegiados_se_validan()
    {
        using var ana = await Cuenta(); using var admin = await Cuenta("admin"); using var noCsrf = await Cuenta("ana", false); using var anon = app.Browser();
        var p = await Crear();
        try
        {
            Assert.Equal(HttpStatusCode.Redirect, (await Denunciar(anon, p.Id)).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await Denunciar(noCsrf, p.Id)).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await Denunciar(ana, p.Id, "<script>inventado</script>")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await Denunciar(ana, p.Id, ReportesService.Revision)).StatusCode);
            Assert.Equal(HttpStatusCode.Redirect, (await Denunciar(ana, p.Id)).StatusCode);
            var id = await ReportId(p.Id); var version = await Version(p.Id);
            var state = await app.Db(db => db.Reportes.SingleAsync(r => r.Id == id));
            Assert.Equal(EstadoReporte.Pendiente, state.Estado); Assert.Null(state.ResueltoPorId);
            var denied = await ana.GetAsync("/administracion/reportes");
            Assert.Equal(HttpStatusCode.Redirect, denied.StatusCode); Assert.Contains("sin-acceso", denied.Headers.Location!.ToString());
            Assert.Equal(HttpStatusCode.Redirect, (await Resolver(ana, id, version)).StatusCode);
            Assert.Equal(HttpStatusCode.MethodNotAllowed, (await admin.GetAsync($"/administracion/reportes/{id}/resolver")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await Resolver(admin, id, version, 99)).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await Resolver(admin, id, version, 2)).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/administracion/reportes?estado=99")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await ana.GetAsync("/mis-reportes?pagina=no")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/administracion/reportes?q=" + new string('a', 101))).StatusCode);
            using var adminNoCsrf = await Cuenta("admin", false);
            Assert.Equal(HttpStatusCode.BadRequest, (await Resolver(adminNoCsrf, id, version)).StatusCode);
        }
        finally { await Limpiar(p.Id); }
    }

    [MySqlFact]
    public async Task Privacidad_actual_redacta_titulos_busquedas_y_medios_incluso_para_admin()
    {
        using var ana = await Cuenta(); using var admin = await Cuenta("admin"); using var bruno = await Cuenta("bruno");
        var p = await Crear();
        var key = Guid.NewGuid().ToString("N") + ".png";
        Directory.CreateDirectory(app.MediaRoot); await File.WriteAllBytesAsync(Path.Combine(app.MediaRoot, key), [1, 2, 3, 4]);
        var resource = await app.Db(async db => { var r = new RecursoMultimedia { PublicacionId = p.Id, ClaveAlmacenamiento = key, Mime = "image/png", EstadoProcesamiento = EstadoProcesamiento.Listo }; db.Recursos.Add(r); await db.SaveChangesAsync(); return r.Id; });
        try
        {
            await Denunciar(ana, p.Id); var id = await ReportId(p.Id);
            var path = $"/administracion/reportes/{id}/medios/{resource}/archivo";
            Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.Redirect, (await ana.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.Redirect, (await Resolver(admin, id, await Version(p.Id))).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await ana.GetAsync($"/medios/{resource}/archivo")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync(path)).StatusCode);
            foreach (var scenario in new[] { "post", "perfil", "borrador", "archivo", "suspension" })
            {
                await app.Db(async db => {
                    var post = await db.Publicaciones.Include(x => x.Autor.Perfil).SingleAsync(x => x.Id == p.Id);
                    post.Visibilidad = scenario == "post" ? Visibilidad.Privada : Visibilidad.Publica;
                    post.Autor.Perfil.Visibilidad = scenario == "perfil" ? Visibilidad.Privada : Visibilidad.Publica;
                    post.EstadoEditorial = scenario == "borrador" ? EstadoEditorial.Borrador : scenario == "archivo" ? EstadoEditorial.Archivada : EstadoEditorial.Publicada;
                    post.Autor.Estado = scenario == "suspension" ? EstadoCuenta.Suspendida : EstadoCuenta.Activa;
                    return await db.SaveChangesAsync();
                });
                var detail = WebUtility.HtmlDecode(await admin.GetStringAsync($"/administracion/reportes/{id}"));
                Assert.DoesNotContain(p.Titulo, detail); Assert.DoesNotContain(p.Texto, detail);
                Assert.DoesNotContain(p.Titulo, WebUtility.HtmlDecode(await admin.GetStringAsync("/administracion/reportes?estado=")));
                Assert.Contains("0 resultados", await admin.GetStringAsync("/administracion/reportes?estado=&q=" + Uri.EscapeDataString(p.Titulo)));
                Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync(path)).StatusCode);
                Assert.Equal(HttpStatusCode.NotFound, (await admin.SendAsync(new HttpRequestMessage(HttpMethod.Head, path))).StatusCode);
                using var range = new HttpRequestMessage(HttpMethod.Get, path); range.Headers.Range = new(0, 1);
                Assert.Equal(HttpStatusCode.NotFound, (await admin.SendAsync(range)).StatusCode);
                Assert.Equal(HttpStatusCode.NotFound, (await Denunciar(ana, p.Id)).StatusCode);
            }
        }
        finally {
            await app.Db(async db => { var u = await db.Users.Include(u => u.Perfil).SingleAsync(u => u.UserName == "bruno"); u.Estado = EstadoCuenta.Activa; u.Perfil.Visibilidad = Visibilidad.Publica; return await db.SaveChangesAsync(); });
            await Limpiar(p.Id); File.Delete(Path.Combine(app.MediaRoot, key));
        }
    }

    [MySqlFact]
    public async Task Cambios_concurrentes_no_pisan_edicion_ni_duplican_resoluciones()
    {
        using var ana = await Cuenta(); using var admin = await Cuenta("admin");
        var p = await Crear();
        try
        {
            var results = await Task.WhenAll(Denunciar(ana, p.Id), Denunciar(ana, p.Id));
            Assert.All(results, r => Assert.Contains(r.StatusCode, new[] { HttpStatusCode.Redirect, HttpStatusCode.Conflict }));
            Assert.Equal(1, await app.Db(db => db.Reportes.CountAsync(r => r.PublicacionId == p.Id)));
            var id = await ReportId(p.Id); var old = await Version(p.Id);
            await app.Db(db => db.Publicaciones.Where(x => x.Id == p.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Version, Guid.NewGuid().ToString("N")).SetProperty(x => x.Visibilidad, Visibilidad.Privada)));
            Assert.Equal(HttpStatusCode.Conflict, (await Resolver(admin, id, old)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await Resolver(admin, id, await Version(p.Id))).StatusCode);
            var version = await Version(p.Id);
            results = await Task.WhenAll(Resolver(admin, id, version, 0), Resolver(admin, id, version, 0));
            Assert.Single(results, r => r.StatusCode == HttpStatusCode.Redirect);
            Assert.Single(results, r => r.StatusCode == HttpStatusCode.Conflict);
            Assert.Equal(EstadoModeracion.Visible, await app.Db(db => db.Publicaciones.Where(x => x.Id == p.Id).Select(x => x.EstadoModeracion).SingleAsync()));
        }
        finally { await Limpiar(p.Id); }
    }

    [MySqlFact]
    public async Task Listas_paginan_filtran_y_aislan_reportes_sin_ejecutar_html()
    {
        using var ana = await Cuenta(); using var bruno = await Cuenta("bruno"); using var admin = await Cuenta("admin");
        var p = await Crear();
        try
        {
            await app.Db(async db => {
                var actor = await db.Users.Where(u => u.UserName == "ana").Select(u => u.Id).SingleAsync();
                for (var i = 0; i < 14; i++) db.Reportes.Add(new Reporte { PublicacionId = p.Id, DenuncianteId = actor, Motivo = "Otro", Detalle = "<script>alert('x')</script>", Estado = i == 13 ? EstadoReporte.Desestimado : EstadoReporte.Pendiente });
                return await db.SaveChangesAsync();
            });
            foreach (var root in new[] { "/mis-reportes", "/administracion/reportes" })
            {
                var client = root.StartsWith("/mis") ? ana : admin;
                var first = await client.GetStringAsync(root + "?estado=0"); var second = await client.GetStringAsync(root + "?estado=0&pagina=2");
                Assert.Equal(12, Regex.Matches(first, "class=\"report-card\"").Count); Assert.Single(Regex.Matches(second, "class=\"report-card\""));
                Assert.Contains("13 resultados", first);
                Assert.Contains("1 resultados", await client.GetStringAsync(root + "?estado=2"));
                Assert.Contains("0 resultados", await client.GetStringAsync(root + "?q=inexistente"));
            }
            Assert.DoesNotContain("class=\"report-card\"", await bruno.GetStringAsync("/mis-reportes"));
            var id = await ReportId(p.Id);
            var html = await admin.GetStringAsync($"/administracion/reportes/{id}");
            Assert.Contains("&lt;script&gt;", html); Assert.DoesNotContain("<script>alert", html);
        }
        finally { await Limpiar(p.Id); }
    }
}
