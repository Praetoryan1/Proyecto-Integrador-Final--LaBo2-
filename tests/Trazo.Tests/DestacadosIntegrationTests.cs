using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Trazo.Web.Domain;
using Trazo.Web.Models;
using Trazo.Web.Services;

namespace Trazo.Tests;

[Collection("mysql")]
public class DestacadosIntegrationTests(MySqlApp app) : IClassFixture<MySqlApp>
{
    private async Task<HttpClient> Cuenta(string name = "ana", bool csrf = true)
    {
        var client = app.Browser(); await MySqlApp.Login(client, name + "@trazo.test");
        if (csrf) client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", await MySqlApp.Token(client, "/panel"));
        return client;
    }
    private Task<string> Id(string name = "ana") => app.Db(db => db.Users.Where(u => u.UserName == name).Select(u => u.Id).SingleAsync());
    private Task<List<Publicacion>> Obras(string actor, int count = 1) => app.Db(async db => {
        var posts = Enumerable.Range(1, count).Select(i => new Publicacion { AutorId = actor, Titulo = "Obra de selección " + i,
            Texto = "Descripción de una obra ficticia.", Tipo = TipoPublicacion.Obra, EstadoEditorial = EstadoEditorial.Publicada, PublicadaUtc = DateTime.UtcNow,
            Recursos = [new RecursoMultimedia { Tipo = TipoRecurso.YouTube, YouTubeId = "abcdefghijk", EstadoProcesamiento = EstadoProcesamiento.Listo, TextoAlternativo = "Obra de prueba" }] }).ToList();
        db.Publicaciones.AddRange(posts); await db.SaveChangesAsync(); return posts;
    });
    private Task<DestacadosModel> Panel(string actor) => app.Db(db => new DestacadosService(db).PanelAsync(actor, null, 1, default));
    private async Task<DestacadoEntrada> Entrada(string actor, long id, int position)
    {
        var version = await app.Db(db => db.Publicaciones.Where(p => p.Id == id).Select(p => p.Version).SingleAsync());
        return new() { Version = version, VersionSeleccion = (await Panel(actor)).VersionSeleccion, Posicion = position };
    }
    private static Task<HttpResponseMessage> Save(HttpClient c, long id, DestacadoEntrada model) => c.PostAsync($"/mi-portafolio/destacados/{id}",
        new FormUrlEncodedContent(new Dictionary<string, string> { ["Version"] = model.Version, ["VersionSeleccion"] = model.VersionSeleccion, ["Posicion"] = model.Posicion.ToString(), ["AutorId"] = "falso" }));
    private Task Limpiar(IEnumerable<Publicacion> posts) { var ids = posts.Select(p => p.Id).ToArray(); return app.Db(db => db.Publicaciones.Where(p => ids.Contains(p.Id)).ExecuteDeleteAsync()); }
    private static string Highlights(string html) => Regex.Match(html, "<section id=\"obras-destacadas\"[\\s\\S]*?</section>").Value;

    [MySqlFact]
    public async Task Seleccion_se_limita_a_seis_reemplaza_ordena_y_quita_sin_duplicar_actividad()
    {
        using var owner = await Cuenta(); using var anon = app.Browser(); var actor = await Id(); var posts = await Obras(actor, 7);
        try {
            for (var i = 0; i < 6; i++) Assert.Equal(HttpStatusCode.Redirect, (await Save(owner, posts[i].Id, await Entrada(actor, posts[i].Id, i + 1))).StatusCode);
            var html = await anon.GetStringAsync("/p/ana"); var highlighted = Highlights(html);
            Assert.Equal(6, Regex.Matches(highlighted, "class=\"work-card\"").Count);
            Assert.True(highlighted.IndexOf($"/publicaciones/{posts[0].Id}\"") < highlighted.IndexOf($"/publicaciones/{posts[5].Id}\""));
            foreach (var p in posts.Take(6)) Assert.Single(Regex.Matches(html, $"href=\"/publicaciones/{p.Id}\""));
            Assert.Equal(HttpStatusCode.Redirect, (await Save(owner, posts[6].Id, await Entrada(actor, posts[6].Id, 1))).StatusCode);
            Assert.Equal(6, (await Panel(actor)).Seleccion.Count);
            Assert.Null(await app.Db(db => db.Publicaciones.Where(p => p.Id == posts[0].Id).Select(p => p.OrdenDestacado).SingleAsync()));
            Assert.Equal(HttpStatusCode.Redirect, (await Save(owner, posts[6].Id, await Entrada(actor, posts[6].Id, 2))).StatusCode);
            Assert.Equal(5, (await Panel(actor)).Seleccion.Count);
            Assert.Equal(HttpStatusCode.Redirect, (await Save(owner, posts[6].Id, await Entrada(actor, posts[6].Id, 0))).StatusCode);
            Assert.Equal(4, (await Panel(actor)).Seleccion.Count);
            Assert.Equal(7, await app.Db(db => db.Publicaciones.CountAsync(p => posts.Select(x => x.Id).Contains(p.Id))));
        } finally { await Limpiar(posts); }
    }

    [MySqlFact]
    public async Task Privacidad_estados_y_recursos_filtran_destacados_sin_perder_la_seleccion()
    {
        using var owner = await Cuenta(); using var registered = await Cuenta("bruno"); using var anon = app.Browser(); var actor = await Id(); var posts = await Obras(actor); var id = posts[0].Id;
        try {
            await Save(owner, id, await Entrada(actor, id, 1));
            await app.Db(db => db.Publicaciones.Where(p => p.Id == id).ExecuteUpdateAsync(s => s.SetProperty(p => p.Visibilidad, Visibilidad.Registrados)));
            Assert.DoesNotContain($"/publicaciones/{id}\"", Highlights(await anon.GetStringAsync("/p/ana")));
            Assert.Contains($"/publicaciones/{id}\"", Highlights(await registered.GetStringAsync("/p/ana")));
            foreach (var state in new[] { "privada", "borrador", "archivada", "oculta", "sinrecurso", "actualizacion" }) {
                await app.Db(async db => {
                    var p = await db.Publicaciones.Include(p => p.Recursos).SingleAsync(p => p.Id == id);
                    p.Visibilidad = state == "privada" ? Visibilidad.Privada : Visibilidad.Publica;
                    p.EstadoEditorial = state == "borrador" ? EstadoEditorial.Borrador : state == "archivada" ? EstadoEditorial.Archivada : EstadoEditorial.Publicada;
                    p.EstadoModeracion = state == "oculta" ? EstadoModeracion.Oculta : EstadoModeracion.Visible;
                    p.Tipo = state == "actualizacion" ? TipoPublicacion.Actualizacion : TipoPublicacion.Obra;
                    p.Recursos[0].EstadoProcesamiento = state == "sinrecurso" ? EstadoProcesamiento.Rechazado : EstadoProcesamiento.Listo;
                    return await db.SaveChangesAsync();
                });
                Assert.DoesNotContain($"/publicaciones/{id}\"", Highlights(await owner.GetStringAsync("/p/ana")));
                Assert.DoesNotContain($"/publicaciones/{id}\"", Highlights(await anon.GetStringAsync("/p/ana")));
                Assert.False(Assert.Single((await Panel(actor)).Seleccion).Disponible);
                Assert.Equal(HttpStatusCode.BadRequest, (await Save(owner, id, await Entrada(actor, id, 2))).StatusCode);
            }
            Assert.Equal(HttpStatusCode.Redirect, (await Save(owner, id, await Entrada(actor, id, 0))).StatusCode);
            Assert.Empty((await Panel(actor)).Seleccion);
        } finally { await Limpiar(posts); }
    }

    [MySqlFact]
    public async Task Autoria_csrf_validacion_y_concurrencia_protegen_la_seleccion()
    {
        using var owner = await Cuenta(); using var admin = await Cuenta("admin"); using var noCsrf = await Cuenta("ana", false); using var anon = app.Browser();
        var actor = await Id(); var posts = await Obras(actor, 2);
        try {
            var a = await Entrada(actor, posts[0].Id, 1); var b = await Entrada(actor, posts[1].Id, 1);
            Assert.Equal(HttpStatusCode.Redirect, (await anon.GetAsync("/mi-portafolio/destacados")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await Save(noCsrf, posts[0].Id, a)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await Save(admin, posts[0].Id, a)).StatusCode);
            Assert.Equal(HttpStatusCode.MethodNotAllowed, (await owner.GetAsync($"/mi-portafolio/destacados/{posts[0].Id}")).StatusCode);
            a.Posicion = 7; Assert.Equal(HttpStatusCode.BadRequest, (await Save(owner, posts[0].Id, a)).StatusCode); a.Posicion = 1;
            var responses = await Task.WhenAll(Save(owner, posts[0].Id, a), Save(owner, posts[1].Id, b));
            Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Redirect)); Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
            Assert.Single((await Panel(actor)).Seleccion);
            var current = await Entrada(actor, posts[0].Id, 2);
            await app.Db(db => db.Publicaciones.Where(p => p.Id == posts[0].Id).ExecuteUpdateAsync(s => s.SetProperty(p => p.Version, Guid.NewGuid().ToString("N"))));
            Assert.Equal(HttpStatusCode.Conflict, (await Save(owner, posts[0].Id, current)).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await owner.GetAsync("/mi-portafolio/destacados?q=" + new string('x', 101))).StatusCode);
        } finally { await Limpiar(posts); }
    }

    [MySqlFact]
    public async Task Catalogo_y_actividad_paginan_sin_descargar_todas_las_obras()
    {
        using var owner = await Cuenta(); using var anon = app.Browser(); var actor = await Id(); var posts = await Obras(actor, 14);
        try {
            var html = await owner.GetStringAsync("/mi-portafolio/destacados");
            Assert.Equal(12, Regex.Matches(html, "class=\"featured-candidate report-card\"").Count);
            Assert.Equal(2, Regex.Matches(await owner.GetStringAsync("/mi-portafolio/destacados?pagina=2"), "class=\"featured-candidate report-card\"").Count);
            Assert.Contains("1 resultados", await owner.GetStringAsync("/mi-portafolio/destacados?q=Obra%20de%20selecci%C3%B3n%2014"));
            await Save(owner, posts[0].Id, await Entrada(actor, posts[0].Id, 1));
            html = await anon.GetStringAsync("/p/ana"); var second = await anon.GetStringAsync("/p/ana?pagina=2");
            Assert.Equal(13, Regex.Matches(html, "class=\"work-card\"").Count); // 1 destacado + 12 de actividad.
            Assert.Equal(11, Regex.Matches(second, "class=\"work-card\"").Count); // 1 destacado + 10 de actividad.
            await app.Db(db => db.Perfiles.Where(p => p.UsuarioId == actor).ExecuteUpdateAsync(s => s.SetProperty(p => p.Visibilidad, Visibilidad.Privada)));
            Assert.Equal(HttpStatusCode.NotFound, (await anon.GetAsync("/p/ana")).StatusCode);
        } finally {
            await app.Db(db => db.Perfiles.Where(p => p.UsuarioId == actor).ExecuteUpdateAsync(s => s.SetProperty(p => p.Visibilidad, Visibilidad.Publica)));
            await Limpiar(posts);
        }
    }

    [MySqlFact]
    public async Task Apariencia_se_guarda_con_opciones_cerradas_sin_inyectar_css()
    {
        using var owner = await Cuenta(); using var anon = app.Browser(); var actor = await Id();
        async Task<HttpResponseMessage> Perfil(string theme, string layout, string color) => await owner.PostAsync("/mi-perfil", new FormUrlEncodedContent(new Dictionary<string, string> {
            ["NombreVisible"] = "Ana Rivera", ["Tema"] = theme, ["Disposicion"] = layout, ["ColorAcento"] = color, ["Visibilidad"] = "0", ["VisibilidadContacto"] = "2" }));
        var original = await app.Db(db => db.Perfiles.AsNoTracking().SingleAsync(p => p.UsuarioId == actor));
        try {
            Assert.Equal(HttpStatusCode.Redirect, (await Perfil("1", "1", "#fda4af")).StatusCode);
            Assert.Contains("theme-claro accent-3 layout-lista", await anon.GetStringAsync("/p/ana"));
            foreach (var input in new[] { ("99", "1", "#fda4af"), ("1", "99", "#fda4af"), ("0", "0", "#000000"), ("0", "0", "red; background:url(https://example.invalid)") })
                Assert.Equal(HttpStatusCode.OK, (await Perfil(input.Item1, input.Item2, input.Item3)).StatusCode);
            var stored = await app.Db(db => db.Perfiles.SingleAsync(p => p.UsuarioId == actor));
            Assert.Equal(TemaPerfil.Claro, stored.Tema); Assert.Equal(DisposicionPerfil.Lista, stored.Disposicion); Assert.Equal("#fda4af", stored.ColorAcento);
        } finally { await app.Db(async db => { db.Perfiles.Update(original); return await db.SaveChangesAsync(); }); }
    }
}
