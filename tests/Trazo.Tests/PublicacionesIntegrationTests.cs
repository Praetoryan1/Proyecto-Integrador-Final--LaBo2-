using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Trazo.Web.Domain;
using Trazo.Web.Models;

namespace Trazo.Tests;

[Collection("mysql")]
public class PublicacionesIntegrationTests(MySqlApp app) : IClassFixture<MySqlApp>
{
    private const string Url = "/panel-json/publicaciones";
    private async Task<HttpClient> Creador(string email = "ana@trazo.test")
    {
        var browser = app.Browser();
        await MySqlApp.Login(browser, email);
        browser.DefaultRequestHeaders.Add("X-CSRF-TOKEN", await MySqlApp.Token(browser, "/panel/publicaciones"));
        return browser;
    }
    private static GuardarPublicacionModel Nuevo(string titulo = "Una publicación de prueba") => new()
    { Titulo = titulo, Texto = "Texto de prueba para el editor.", Tipo = TipoPublicacion.Actualizacion, EstadoEditorial = EstadoEditorial.Borrador, Visibilidad = Visibilidad.Privada };
    private static EditarPublicacionModel Editar(PublicacionEditor post) => new()
    {
        Titulo = post.Titulo,
        Texto = post.Texto,
        Tipo = post.Tipo,
        EstadoEditorial = post.EstadoEditorial,
        Visibilidad = post.Visibilidad,
        EtiquetaIds = post.Etiquetas.Select(e => e.Id).ToArray(),
        Version = post.Version
    };
    private static async Task<PublicacionEditor> Crear(HttpClient browser, GuardarPublicacionModel model)
    {
        var response = await browser.PostAsJsonAsync(Url, model);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var post = (await response.Content.ReadFromJsonAsync<PublicacionEditor>())!;
        Assert.EndsWith($"/{post.Id}", response.Headers.Location!.ToString());
        return post;
    }
    private static Task<HttpResponseMessage> Borrar(HttpClient browser, PublicacionEditor post) => browser.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"{Url}/{post.Id}")
    { Content = JsonContent.Create(new EliminarPublicacionModel { Version = post.Version }) });

    [MySqlFact]
    public async Task Abm_json_persiste_etiquetas_y_estados_con_privacidad_real()
    {
        using var owner = await Creador(); using var visitor = app.Browser();
        var tags = await app.Db(db => db.Etiquetas.OrderBy(e => e.Id).Take(2).Select(e => e.Id).ToArrayAsync());
        var model = Nuevo(); model.EtiquetaIds = tags;
        var post = await Crear(owner, model);
        Assert.Equal(2, post.Etiquetas.Count);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync($"/publicaciones/{post.Id}")).StatusCode);
        var edit = Editar(post); edit.Titulo = "Proceso compartido"; edit.EstadoEditorial = EstadoEditorial.Publicada;
        edit.Visibilidad = Visibilidad.Publica; edit.EtiquetaIds = [tags[1]];
        var response = await owner.PutAsJsonAsync($"{Url}/{post.Id}", edit);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = (await response.Content.ReadFromJsonAsync<PublicacionEditor>())!;
        Assert.Single(saved.Etiquetas); Assert.Equal(tags[1], saved.Etiquetas[0].Id);
        Assert.NotEqual(post.Version, saved.Version);
        Assert.Equal(HttpStatusCode.OK, (await visitor.GetAsync($"/publicaciones/{post.Id}")).StatusCode);
        edit = Editar(saved); edit.EstadoEditorial = EstadoEditorial.Archivada;
        response = await owner.PutAsJsonAsync($"{Url}/{post.Id}", edit);
        saved = (await response.Content.ReadFromJsonAsync<PublicacionEditor>())!;
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync($"/publicaciones/{post.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Borrar(owner, saved)).StatusCode);
        Assert.False(await app.Db(db => db.PublicacionEtiquetas.AnyAsync(e => e.PublicacionId == post.Id)));
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"{Url}/{post.Id}")).StatusCode);
    }

    [MySqlFact]
    public async Task Json_exige_sesion_csrf_y_propiedad_incluso_para_administrador()
    {
        using var anonymous = app.Browser();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Url)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/panel-json/etiquetas")).StatusCode);
        using var owner = await Creador();
        var post = await Crear(owner, Nuevo());
        using var noCsrf = app.Browser(); await MySqlApp.Login(noCsrf, "ana@trazo.test");
        Assert.Equal(HttpStatusCode.BadRequest, (await noCsrf.PostAsJsonAsync(Url, Nuevo())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await noCsrf.PutAsJsonAsync($"{Url}/{post.Id}", Editar(post))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Borrar(noCsrf, post)).StatusCode);
        foreach (var email in new[] { "bruno@trazo.test", "admin@trazo.test" })
        {
            using var other = await Creador(email);
            Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"{Url}/{post.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await other.PutAsJsonAsync($"{Url}/{post.Id}", Editar(post))).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await Borrar(other, post)).StatusCode);
        }
        Assert.Equal(HttpStatusCode.NoContent, (await Borrar(owner, post)).StatusCode);
    }

    [MySqlFact]
    public async Task Ediciones_simultaneas_y_bajas_desactualizadas_no_pisan_cambios()
    {
        using var owner = await Creador();
        var post = await Crear(owner, Nuevo());
        var edit1 = Editar(post); edit1.Titulo = "Cambio de pestaña uno";
        var edit2 = Editar(post); edit2.Titulo = "Cambio de pestaña dos";
        var responses = await Task.WhenAll(owner.PutAsJsonAsync($"{Url}/{post.Id}", edit1), owner.PutAsJsonAsync($"{Url}/{post.Id}", edit2));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(HttpStatusCode.Conflict, (await Borrar(owner, post)).StatusCode);
        var saved = (await owner.GetFromJsonAsync<PublicacionEditor>($"{Url}/{post.Id}"))!;
        Assert.NotEqual(post.Version, saved.Version);
        Assert.Contains(saved.Titulo, new[] { edit1.Titulo, edit2.Titulo });
        Assert.Equal(HttpStatusCode.NoContent, (await Borrar(owner, saved)).StatusCode);
    }

    [MySqlFact]
    public async Task Validacion_no_publica_obras_sin_recursos_ni_admite_etiquetas_invalidas()
    {
        using var owner = await Creador();
        var tag = await app.Db(db => db.Etiquetas.Select(e => e.Id).FirstAsync());
        var model = Nuevo(); model.Tipo = TipoPublicacion.Obra; model.EstadoEditorial = EstadoEditorial.Publicada;
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync(Url, model)).StatusCode);
        model.EstadoEditorial = EstadoEditorial.Borrador;
        var draft = await Crear(owner, model);
        Assert.Equal(TipoPublicacion.Obra, draft.Tipo);
        Assert.Equal(HttpStatusCode.NoContent, (await Borrar(owner, draft)).StatusCode);
        foreach (var ids in new[] { new long[] { -1 }, new long[] { long.MaxValue }, new long[] { tag, tag }, Enumerable.Range(1, 9).Select(i => (long)i).ToArray() })
        {
            model = Nuevo(); model.EtiquetaIds = ids;
            Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync(Url, model)).StatusCode);
        }
        model = Nuevo(); model.Visibilidad = (Visibilidad)999;
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync(Url, model)).StatusCode);
        model = Nuevo(); model.Titulo = " ";
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync(Url, model)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.GetAsync(Url + "?estado=999")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.GetAsync(Url + "?pagina=abc")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.GetAsync("/panel-json/etiquetas?q=" + new string('x', 41))).StatusCode);
    }

    [MySqlFact]
    public async Task Campos_privilegiados_y_moderacion_no_se_sobrescriben_desde_json()
    {
        using var owner = await Creador(); using var visitor = app.Browser();
        var response = await owner.PostAsJsonAsync(Url, new { titulo = "Prueba overposting", texto = "Texto", tipo = 1, estadoEditorial = 0, visibilidad = 0, etiquetaIds = Array.Empty<long>(), autorId = "otro", estadoModeracion = 1, ordenDestacado = 1 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var post = (await response.Content.ReadFromJsonAsync<PublicacionEditor>())!;
        Assert.True(await app.Db(db => db.Publicaciones.AnyAsync(p => p.Id == post.Id && p.Autor.UserName == "ana" && p.OrdenDestacado == null)));
        Assert.Equal(EstadoModeracion.Visible, post.EstadoModeracion);
        await app.Db(async db => { var p = await db.Publicaciones.FindAsync(post.Id); p!.EstadoModeracion = EstadoModeracion.Oculta; return await db.SaveChangesAsync(); });
        response = await owner.PutAsJsonAsync($"{Url}/{post.Id}", new { titulo = "Cambio permitido", texto = "Texto", tipo = 1, estadoEditorial = 1, visibilidad = 0, etiquetaIds = Array.Empty<long>(), version = post.Version, estadoModeracion = 0, autorId = "otro" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        post = (await response.Content.ReadFromJsonAsync<PublicacionEditor>())!;
        Assert.Equal(EstadoModeracion.Oculta, post.EstadoModeracion);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync($"/publicaciones/{post.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Borrar(owner, post)).StatusCode);
    }

    [MySqlFact]
    public async Task Listados_paginados_y_etiquetas_inactivas_respetan_el_contrato()
    {
        using var owner = await Creador();
        await app.Db(async db =>
        {
            var autorId = await db.Users.Where(u => u.UserName == "ana").Select(u => u.Id).SingleAsync();
            db.Publicaciones.AddRange(Enumerable.Range(1, 13).Select(i => new Publicacion { AutorId = autorId, Titulo = $"PaginadoJSON {i:00}", Texto = "Prueba", Tipo = TipoPublicacion.Actualizacion }));
            db.Etiquetas.AddRange(Enumerable.Range(1, 21).Select(i => new Etiqueta { Nombre = $"SelectorJSON {i:00}", NombreNormalizado = $"SELECTORJSON {i:00}" }));
            return await db.SaveChangesAsync();
        });
        var first = (await owner.GetFromJsonAsync<Pagina<PublicacionResumen>>(Url + "?q=PaginadoJSON"))!;
        var second = (await owner.GetFromJsonAsync<Pagina<PublicacionResumen>>(Url + "?q=PaginadoJSON&pagina=2"))!;
        Assert.Equal(13, first.Total); Assert.Equal(12, first.Items.Count); Assert.Single(second.Items);
        Assert.Empty(first.Items.Select(p => p.Id).Intersect(second.Items.Select(p => p.Id)));
        using var other = await Creador("bruno@trazo.test");
        Assert.Equal(0, (await other.GetFromJsonAsync<Pagina<PublicacionResumen>>(Url + "?q=PaginadoJSON"))!.Total);
        var tags = (await owner.GetFromJsonAsync<Pagina<EtiquetaEditor>>("/panel-json/etiquetas?q=SelectorJSON"))!;
        Assert.Equal(21, tags.Total); Assert.Equal(10, tags.Items.Count);
        var lastTags = (await owner.GetFromJsonAsync<Pagina<EtiquetaEditor>>("/panel-json/etiquetas?q=SelectorJSON&pagina=3"))!;
        Assert.Single(lastTags.Items);
        var tagId = tags.Items[0].Id;
        var model = Nuevo(); model.EtiquetaIds = [tagId];
        var post = await Crear(owner, model);
        await app.Db(async db => { var tag = await db.Etiquetas.FindAsync(tagId); tag!.Activa = false; return await db.SaveChangesAsync(); });
        tags = (await owner.GetFromJsonAsync<Pagina<EtiquetaEditor>>("/panel-json/etiquetas?q=SelectorJSON"))!;
        Assert.Equal(20, tags.Total); Assert.DoesNotContain(tags.Items, t => t.Id == tagId);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync(Url, model)).StatusCode);
        var response = await owner.PutAsJsonAsync($"{Url}/{post.Id}", Editar(post));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // Se permite conservar una asociación existente.
        post = (await response.Content.ReadFromJsonAsync<PublicacionEditor>())!;
        Assert.False(post.Etiquetas[0].Activa);
        Assert.Equal(HttpStatusCode.NoContent, (await Borrar(owner, post)).StatusCode);
    }

    [MySqlFact]
    public async Task Una_publicacion_con_reporte_puede_archivarse_pero_no_borrarse()
    {
        using var owner = await Creador();
        var post = await Crear(owner, Nuevo());
        await app.Db(async db =>
        {
            var denunciante = await db.Users.Where(u => u.UserName == "bruno").Select(u => u.Id).SingleAsync();
            db.Reportes.Add(new Reporte { PublicacionId = post.Id, DenuncianteId = denunciante, Motivo = "Prueba" });
            return await db.SaveChangesAsync();
        });
        Assert.Equal(HttpStatusCode.Conflict, (await Borrar(owner, post)).StatusCode);
        var edit = Editar(post); edit.EstadoEditorial = EstadoEditorial.Archivada;
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync($"{Url}/{post.Id}", edit)).StatusCode);
        Assert.True(await app.Db(db => db.Reportes.AnyAsync(r => r.PublicacionId == post.Id)));
    }
}
