using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using Trazo.Web.Domain;
using Trazo.Web.Models;
using Trazo.Web.Services;

namespace Trazo.Tests;

public class YouTubeTests
{
    [Theory]
    [InlineData("https://youtu.be/abcdefghijk")]
    [InlineData("https://www.youtube.com/watch?v=abcdefghijk&t=1")]
    [InlineData("https://m.youtube.com/shorts/abcdefghijk")]
    [InlineData("https://youtube.com/embed/abcdefghijk")]
    public void Extrae_solo_identificador(string url) => Assert.Equal("abcdefghijk", RecursosService.ExtraerYouTubeId(url));

    [Theory]
    [InlineData("http://youtu.be/abcdefghijk")]
    [InlineData("https://youtube.com.evil.invalid/watch?v=abcdefghijk")]
    [InlineData("https://youtube.com@evil.invalid/watch?v=abcdefghijk")]
    [InlineData("https://user@youtube.com/watch?v=abcdefghijk")]
    [InlineData("https://youtube.com:444/watch?v=abcdefghijk")]
    [InlineData("https://youtube.com/watch?v=abcdefghijk&v=zyxwvutsrqp")]
    [InlineData("https://youtu.be/short")]
    [InlineData("<iframe src='https://youtu.be/abcdefghijk'></iframe>")]
    public void Rechaza_fuentes_no_permitidas(string url) => Assert.Throws<PublicacionException>(() => RecursosService.ExtraerYouTubeId(url));
}

[Collection("mysql")]
public class MediosIntegrationTests(MySqlApp app) : IClassFixture<MySqlApp>
{
    private const string Url = "/panel-json/publicaciones";
    private async Task<HttpClient> Creador(string email = "ana@trazo.test")
    {
        var client = app.Browser();
        await MySqlApp.Login(client, email);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", await MySqlApp.Token(client, "/panel/publicaciones"));
        return client;
    }
    private static async Task<PublicacionEditor> Leer(HttpResponseMessage response, HttpStatusCode status = HttpStatusCode.OK)
    {
        Assert.True(response.StatusCode == status, $"{response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<PublicacionEditor>())!;
    }
    private static Task<PublicacionEditor> Crear(HttpClient client) => CrearAsync(client);
    private static async Task<PublicacionEditor> CrearAsync(HttpClient client) => await Leer(await client.PostAsJsonAsync(Url, new GuardarPublicacionModel
    { Titulo = "Prueba de recursos", Texto = "Obra de prueba multimedia.", Tipo = TipoPublicacion.Obra, EstadoEditorial = EstadoEditorial.Borrador, Visibilidad = Visibilidad.Publica }), HttpStatusCode.Created);
    private static EditarPublicacionModel Editar(PublicacionEditor p, EstadoEditorial estado, Visibilidad visibility = Visibilidad.Publica) => new()
    { Version = p.Version, Titulo = p.Titulo, Texto = p.Texto, Tipo = p.Tipo, EstadoEditorial = estado, Visibilidad = visibility };
    private static byte[] Png()
    {
        using var image = new SKBitmap(64, 48);
        image.Erase(SKColors.CornflowerBlue);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
    private static Task<HttpResponseMessage> Subir(HttpClient c, PublicacionEditor p, byte[]? bytes = null, string name = "prueba.png", string alt = "Estudio azul")
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(p.Version), "Version");
        content.Add(new StringContent(alt), "TextoAlternativo");
        content.Add(new StringContent("Autoría de prueba"), "Creditos");
        content.Add(new ByteArrayContent(bytes ?? Png()), "Archivo", name);
        return c.PostAsync($"{Url}/{p.Id}/recursos/archivo", content);
    }
    private static Task<HttpResponseMessage> Borrar(HttpClient c, PublicacionEditor p, long? resource = null) => c.SendAsync(new HttpRequestMessage(HttpMethod.Delete,
        $"{Url}/{p.Id}" + (resource.HasValue ? $"/recursos/{resource}" : "")) { Content = JsonContent.Create(new { p.Version }) });
    private static Task<HttpResponseMessage> Youtube(HttpClient c, PublicacionEditor p) => c.PostAsJsonAsync($"{Url}/{p.Id}/recursos/youtube",
        new { p.Version, Url = "https://youtu.be/abcdefghijk", TextoAlternativo = "Video de prueba", Creditos = "" });

    [MySqlFact]
    public async Task Imagen_normalizada_obedece_privacidad_en_archivo_miniatura_head_y_range()
    {
        using var owner = await Creador(); using var visitor = app.Browser(); using var admin = await Creador("admin@trazo.test");
        var post = await Leer(await Subir(owner, await Crear(owner)));
        var resource = Assert.Single(post.Recursos);
        Assert.Equal(TipoRecurso.Imagen, resource.Tipo);
        foreach (var url in new[] { resource.ArchivoUrl!, resource.VistaPreviaUrl! })
        {
            Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync(url)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync(url)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync(url)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await visitor.SendAsync(new HttpRequestMessage(HttpMethod.Head, url))).StatusCode);
        }
        post = await Leer(await owner.PutAsJsonAsync($"{Url}/{post.Id}", Editar(post, EstadoEditorial.Publicada)));
        var response = await visitor.GetAsync(resource.ArchivoUrl);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType!.MediaType);
        Assert.True(response.Headers.CacheControl!.NoStore);
        using var range = new HttpRequestMessage(HttpMethod.Get, resource.ArchivoUrl);
        range.Headers.Range = new RangeHeaderValue(0, 9);
        Assert.Equal(HttpStatusCode.PartialContent, (await visitor.SendAsync(range)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Borrar(owner, post, resource.Id)).StatusCode);
        post = await Leer(await owner.PutAsJsonAsync($"{Url}/{post.Id}", Editar(post, EstadoEditorial.Publicada, Visibilidad.Privada)));
        using var deniedRange = new HttpRequestMessage(HttpMethod.Get, resource.ArchivoUrl);
        deniedRange.Headers.Range = new RangeHeaderValue(0, 9);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.SendAsync(deniedRange)).StatusCode);
        var key = await app.Db(db => db.Recursos.Where(r => r.Id == resource.Id).Select(r => r.ClaveAlmacenamiento).SingleAsync());
        Assert.Equal(HttpStatusCode.NoContent, (await Borrar(owner, post)).StatusCode);
        Assert.False(File.Exists(Path.Combine(app.MediaRoot, key!)));
        Assert.False(File.Exists(Path.Combine(app.MediaRoot, Path.GetFileNameWithoutExtension(key!) + ".preview.png")));
    }

    [MySqlFact]
    public async Task Archivos_falsos_texto_alternativo_y_propiedad_se_validan_antes_de_guardar()
    {
        using var owner = await Creador(); using var other = await Creador("bruno@trazo.test");
        using var noCsrf = app.Browser(); await MySqlApp.Login(noCsrf, "ana@trazo.test");
        var post = await Crear(owner);
        Assert.Equal(HttpStatusCode.BadRequest, (await Subir(noCsrf, post)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Subir(other, post)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Youtube(other, post)).StatusCode);
        foreach (var name in new[] { "fake.jpg", "fake.gif", "fake.mp4", "fake.svg" })
            Assert.Equal(HttpStatusCode.BadRequest, (await Subir(owner, post, Png(), name)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Subir(owner, post, "not an image"u8.ToArray())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Subir(owner, post, alt: " ")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Subir(owner, post, new byte[10 * 1024 * 1024 + 1])).StatusCode);
        Assert.False(await app.Db(db => db.Recursos.AnyAsync(r => r.PublicacionId == post.Id)));
        Assert.Equal(HttpStatusCode.NoContent, (await Borrar(owner, post)).StatusCode);
    }

    [MySqlFact]
    public async Task Orden_descripcion_baja_y_version_compartida_funcionan_con_indice_unico()
    {
        using var owner = await Creador();
        var original = await Crear(owner);
        var post = await Leer(await Subir(owner, original));
        post = await Leer(await Youtube(owner, post));
        var ids = post.Recursos.Select(r => r.Id).Reverse().ToArray();
        post = await Leer(await owner.PutAsJsonAsync($"{Url}/{post.Id}/recursos/orden", new { post.Version, RecursoIds = ids }));
        Assert.Equal(ids, post.Recursos.Select(r => r.Id));
        Assert.Equal(new[] { 0, 1 }, post.Recursos.Select(r => r.Orden));
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PutAsJsonAsync($"{Url}/{post.Id}/recursos/orden", new { post.Version, RecursoIds = new[] { ids[0], ids[0] } })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Youtube(owner, original)).StatusCode);
        post = await Leer(await owner.PutAsJsonAsync($"{Url}/{post.Id}/recursos/{ids[0]}", new { post.Version, TextoAlternativo = "Descripción corregida", Creditos = "Equipo" }));
        Assert.Equal("Descripción corregida", post.Recursos[0].TextoAlternativo);
        Assert.Equal(HttpStatusCode.NotFound, (await Borrar(owner, post, long.MaxValue)).StatusCode);
        post = await Leer(await Borrar(owner, post, ids[0]));
        post = await Leer(await Borrar(owner, post, ids[1]));
        Assert.Empty(post.Recursos);
        Assert.Equal(HttpStatusCode.NoContent, (await Borrar(owner, post)).StatusCode);
    }

    [MySqlFact]
    public async Task Cuota_limite_de_recursos_y_cargas_simultaneas_no_dejan_archivos_huerfanos()
    {
        using var owner = await Creador();
        var post = await Crear(owner);
        var before = Directory.Exists(app.MediaRoot) ? Directory.GetFiles(app.MediaRoot).Length : 0;
        var responses = await Task.WhenAll(Subir(owner, post), Subir(owner, post));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        post = await Leer(responses.Single(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(before + 2, Directory.GetFiles(app.MediaRoot).Length);
        var resourceId = post.Recursos[0].Id;
        await app.Db(db => db.Recursos.Where(r => r.Id == resourceId).ExecuteUpdateAsync(set => set.SetProperty(r => r.Bytes, AlmacenMedios.CuotaCuenta)));
        Assert.Equal(HttpStatusCode.BadRequest, (await Subir(owner, post)).StatusCode);
        Assert.Equal(before + 2, Directory.GetFiles(app.MediaRoot).Length);
        for (var i = 1; i < 10; i++) post = await Leer(await Youtube(owner, post));
        Assert.Equal(HttpStatusCode.BadRequest, (await Youtube(owner, post)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Borrar(owner, post)).StatusCode);
        Assert.Equal(before, Directory.GetFiles(app.MediaRoot).Length);
    }

    [MySqlFact]
    public async Task Medios_respetan_perfil_registrados_moderacion_y_cuenta_suspendida()
    {
        using var owner = await Creador(); using var other = await Creador("bruno@trazo.test"); using var visitor = app.Browser();
        var post = await Leer(await Subir(owner, await Crear(owner)));
        post = await Leer(await owner.PutAsJsonAsync($"{Url}/{post.Id}", Editar(post, EstadoEditorial.Publicada, Visibilidad.Registrados)));
        var url = post.Recursos[0].VistaPreviaUrl!;
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await other.GetAsync(url)).StatusCode);
        var author = await app.Db(db => db.Publicaciones.Where(p => p.Id == post.Id).Select(p => p.AutorId).SingleAsync());
        try
        {
            await app.Db(db => db.Perfiles.Where(p => p.UsuarioId == author).ExecuteUpdateAsync(s => s.SetProperty(p => p.Visibilidad, Visibilidad.Privada)));
            Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(url)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync(url)).StatusCode);
            await app.Db(db => db.Perfiles.Where(p => p.UsuarioId == author).ExecuteUpdateAsync(s => s.SetProperty(p => p.Visibilidad, Visibilidad.Publica)));
            await app.Db(db => db.Publicaciones.Where(p => p.Id == post.Id).ExecuteUpdateAsync(s => s.SetProperty(p => p.EstadoModeracion, EstadoModeracion.Oculta)));
            Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(url)).StatusCode);
            await app.Db(db => db.Users.Where(u => u.Id == author).ExecuteUpdateAsync(s => s.SetProperty(u => u.Estado, EstadoCuenta.Suspendida)));
            Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync(url)).StatusCode);
        }
        finally
        {
            await app.Db(db => db.Users.Where(u => u.Id == author).ExecuteUpdateAsync(s => s.SetProperty(u => u.Estado, EstadoCuenta.Activa)));
            await app.Db(db => db.Perfiles.Where(p => p.UsuarioId == author).ExecuteUpdateAsync(s => s.SetProperty(p => p.Visibilidad, Visibilidad.Publica)));
            using var restored = await Creador();
            Assert.Equal(HttpStatusCode.NoContent, (await Borrar(restored, post)).StatusCode);
        }
    }

    [MySqlFact]
    public async Task Mp4_rechaza_codec_duracion_y_dimensiones_fuera_de_limite()
    {
        using var owner = await Creador();
        var post = await Crear(owner);
        var process = app.Services.GetRequiredService<ProcesoMultimedia>();
        Directory.CreateDirectory(app.MediaRoot);
        var path = Path.Combine(app.MediaRoot, "invalid-fixture.mp4");
        foreach (var (size, duration, codec) in new[] { ("160x120", "1", "mpeg4"), ("160x120", "61", "libx264"), ("2000x100", "1", "libx264") })
        {
            try
            {
                await process.EjecutarAsync("ffmpeg", ["-v", "error", "-y", "-f", "lavfi", "-i", $"color=c=blue:size={size}:rate=1", "-t", duration, "-threads", "1", "-c:v", codec, path], CancellationToken.None);
                Assert.Equal(HttpStatusCode.BadRequest, (await Subir(owner, post, await File.ReadAllBytesAsync(path), "invalid.mp4")).StatusCode);
            }
            finally { File.Delete(path); }
        }
        Assert.Empty((await owner.GetFromJsonAsync<PublicacionEditor>($"{Url}/{post.Id}"))!.Recursos);
        Assert.Equal(HttpStatusCode.NoContent, (await Borrar(owner, post)).StatusCode);
    }

    [MySqlFact]
    public async Task Gif_y_mp4_se_procesan_con_ffmpeg_y_video_acepta_rangos()
    {
        using var owner = await Creador(); using var visitor = app.Browser();
        var post = await Crear(owner);
        var process = app.Services.GetRequiredService<ProcesoMultimedia>();
        Directory.CreateDirectory(app.MediaRoot);
        foreach (var extension in new[] { "gif", "mp4" })
        {
            var path = Path.Combine(app.MediaRoot, "fixture." + extension);
            var args = new List<string> { "-v", "error", "-y", "-f", "lavfi", "-i", "testsrc2=size=160x120:rate=10", "-t", "1", "-threads", "1" };
            if (extension == "mp4") args.AddRange(["-c:v", "libx264", "-pix_fmt", "yuv420p"]);
            args.Add(path);
            try
            {
                await process.EjecutarAsync("ffmpeg", args, CancellationToken.None);
                post = await Leer(await Subir(owner, post, await File.ReadAllBytesAsync(path), "fixture." + extension));
            }
            finally { File.Delete(path); }
        }
        Assert.Equal(new[] { TipoRecurso.Gif, TipoRecurso.Video }, post.Recursos.Select(r => r.Tipo));
        post = await Leer(await owner.PutAsJsonAsync($"{Url}/{post.Id}", Editar(post, EstadoEditorial.Publicada)));
        var video = post.Recursos[1];
        using var range = new HttpRequestMessage(HttpMethod.Get, video.ArchivoUrl);
        range.Headers.Range = new RangeHeaderValue(0, 99);
        var response = await visitor.SendAsync(range);
        Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
        Assert.Equal(100, (await response.Content.ReadAsByteArrayAsync()).Length);
        Assert.Contains(post.Titulo, await visitor.GetStringAsync("/?medio=2"));
        Assert.Equal(HttpStatusCode.BadRequest, (await visitor.GetAsync("/?medio=999")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Borrar(owner, post)).StatusCode);
    }
}
