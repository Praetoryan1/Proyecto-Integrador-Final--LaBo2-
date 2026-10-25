using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using SkiaSharp;
using Trazo.Web.Data;
using Trazo.Web.Domain;

namespace Trazo.Tests;

public sealed class MySqlFactAttribute : FactAttribute
{
    public MySqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TRAZO_TEST_CONNECTION")))
            Skip = "Requiere MySQL real. Ejecutar tools/Probar.ps1 -Integracion.";
    }
}

[CollectionDefinition("mysql", DisableParallelization = true)]
public class MySqlCollection;

public class MySqlApp : WebApplicationFactory<Program>, IAsyncLifetime
{
    public string AvatarRoot { get; } = Path.Combine(Path.GetTempPath(), "trazo-tests", Guid.NewGuid().ToString("N"));
    public string MediaRoot => Path.Combine(AvatarRoot, "medios");
    public string JwtKey { get; } = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        var connection = Environment.GetEnvironmentVariable("TRAZO_TEST_CONNECTION")!;
        builder.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:Trazo"] = connection, ["Storage:Root"] = AvatarRoot, ["Media:Root"] = MediaRoot, ["Jwt:SigningKey"] = JwtKey }));
    }
    public async Task InitializeAsync()
    {
        var connection = Environment.GetEnvironmentVariable("TRAZO_TEST_CONNECTION");
        if (string.IsNullOrEmpty(connection)) return;
        var cs = new MySqlConnectionStringBuilder(connection);
        if (cs.Database != "trazo_tests" || cs.Server is not ("localhost" or "127.0.0.1"))
            throw new InvalidOperationException("Las pruebas solo pueden recrear trazo_tests en localhost.");
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TrazoDbContext>();
        await db.Database.EnsureDeletedAsync();
        await DbInitializer.InitializeAsync(scope.ServiceProvider, demo: true);
    }
    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;
    public HttpClient Browser() => CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
    public async Task<T> Db<T>(Func<TrazoDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<TrazoDbContext>());
    }
    public static async Task<string> Token(HttpClient client, string path)
    {
        var page = await client.GetStringAsync(path);
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success, "El formulario debe incluir antiforgery.");
        return WebUtility.HtmlDecode(token.Groups[1].Value);
    }
    public static async Task Login(HttpClient client, string email)
    {
        var token = await Token(client, "/cuenta/ingresar");
        var response = await client.PostAsync("/cuenta/ingresar", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["Email"] = email, ["Password"] = DbInitializer.DemoPassword, ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
}

[Collection("mysql")]
public class MySqlIntegrationTests(MySqlApp app) : IClassFixture<MySqlApp>
{
    [MySqlFact]
    public async Task Mysql_rechaza_autoseguimiento_y_recursos_con_dos_fuentes()
    {
        await Assert.ThrowsAsync<DbUpdateException>(async () => await app.Db(async db =>
        {
            var id = await db.Users.Where(u => u.UserName == "ana").Select(u => u.Id).SingleAsync();
            db.Seguimientos.Add(new Seguimiento { SeguidorId = id, SeguidoId = id });
            return await db.SaveChangesAsync();
        }));
        await Assert.ThrowsAsync<DbUpdateException>(async () => await app.Db(async db =>
        {
            var id = await db.Publicaciones.Select(p => p.Id).FirstAsync();
            db.Recursos.Add(new RecursoMultimedia { PublicacionId = id, ClaveAlmacenamiento = "imagen.png", YouTubeId = "abcdefghijk" });
            return await db.SaveChangesAsync();
        }));
    }

    [MySqlFact]
    public async Task Login_no_redirige_a_sitios_externos_y_logout_no_acepta_get()
    {
        using var browser = app.Browser();
        var token = await MySqlApp.Token(browser, "/cuenta/ingresar");
        var response = await browser.PostAsync("/cuenta/ingresar", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["Email"] = "admin@trazo.test", ["Password"] = DbInitializer.DemoPassword, ["ReturnUrl"] = "https://example.invalid/robar", ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/panel", response.Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await browser.GetAsync("/cuenta/salir")).StatusCode);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), value => value.Contains("__Host-Trazo.Session") && value.Contains("secure", StringComparison.OrdinalIgnoreCase) && value.Contains("httponly", StringComparison.OrdinalIgnoreCase));
    }

    [MySqlFact]
    public async Task Explorar_pagina_en_mysql_y_no_filtra_privados()
    {
        using var browser = app.Browser();
        var first = await browser.GetStringAsync("/");
        var second = await browser.GetStringAsync("/?pagina=2");
        Assert.Equal(12, Regex.Matches(first, "class=\"work-card\"").Count);
        Assert.Equal(6, Regex.Matches(second, "class=\"work-card\"").Count);
        Assert.DoesNotContain("Exploraci&#xF3;n privada", first);
        var privateId = await app.Db(db => db.Publicaciones.Where(p => p.Visibilidad == Visibilidad.Privada).Select(p => p.Id).SingleAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await browser.GetAsync($"/publicaciones/{privateId}")).StatusCode);
        using var admin = app.Browser();
        await MySqlApp.Login(admin, "admin@trazo.test");
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/publicaciones/{privateId}")).StatusCode);
        using var author = app.Browser();
        await MySqlApp.Login(author, "ana@trazo.test");
        Assert.Equal(HttpStatusCode.OK, (await author.GetAsync($"/publicaciones/{privateId}")).StatusCode);
        var related = await browser.GetStringAsync("/?etiqueta=Animaci%C3%B3n");
        Assert.InRange(Regex.Matches(related, "class=\"work-card\"").Count, 1, 12);
        Assert.Equal(HttpStatusCode.BadRequest, (await browser.GetAsync("/?tipo=999")).StatusCode);
    }

    [MySqlFact]
    public async Task Registro_es_atomico_asigna_solo_creador_y_exige_csrf()
    {
        using var browser = app.Browser();
        var before = await app.Db(db => db.Users.CountAsync());
        Assert.Equal(HttpStatusCode.BadRequest, (await browser.PostAsync("/cuenta/registro", new FormUrlEncodedContent([]))).StatusCode);
        var token = await MySqlApp.Token(browser, "/cuenta/registro");
        var data = new Dictionary<string, string> { ["NombreUsuario"] = "nuevo_test", ["NombreVisible"] = "Nueva cuenta", ["Email"] = "nuevo@trazo.test", ["Password"] = DbInitializer.DemoPassword, ["Confirmacion"] = DbInitializer.DemoPassword, ["Rol"] = Roles.Administrador, ["__RequestVerificationToken"] = token };
        var response = await browser.PostAsync("/cuenta/registro", new FormUrlEncodedContent(data));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(before + 1, await app.Db(db => db.Users.CountAsync()));
        using var scope = app.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var user = await users.FindByEmailAsync("nuevo@trazo.test");
        Assert.NotNull(user);
        Assert.True(await users.IsInRoleAsync(user, Roles.Creador));
        Assert.False(await users.IsInRoleAsync(user, Roles.Administrador));
        Assert.True(await app.Db(db => db.Perfiles.AnyAsync(p => p.UsuarioId == user.Id)));
        var deny = await browser.GetAsync("/administracion/etiquetas");
        Assert.Equal(HttpStatusCode.Redirect, deny.StatusCode);
        Assert.StartsWith("https://localhost/cuenta/sin-acceso", deny.Headers.Location!.AbsoluteUri);
        var logoutToken = await MySqlApp.Token(browser, "/panel");
        Assert.Equal(HttpStatusCode.BadRequest, (await browser.PostAsync("/cuenta/salir", new FormUrlEncodedContent([]))).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await browser.PostAsync("/cuenta/salir", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = logoutToken }))).StatusCode);
        Assert.Contains("/cuenta/ingresar", (await browser.GetAsync("/panel")).Headers.Location!.ToString());
    }

    [MySqlFact]
    public async Task Perfil_y_avatar_respetan_privacidad_y_validan_archivos()
    {
        using var owner = app.Browser(); using var visitor = app.Browser();
        await MySqlApp.Login(owner, "bruno@trazo.test");
        var token = await MySqlApp.Token(owner, "/mi-perfil");
        var fake = new MultipartFormDataContent { { new StringContent(token), "__RequestVerificationToken" }, { new ByteArrayContent("<script>alert(1)</script>"u8.ToArray()), "avatar", "falso.png" } };
        Assert.Equal(HttpStatusCode.Redirect, (await owner.PostAsync("/mi-perfil/avatar", fake)).StatusCode);
        Assert.False(await app.Db(db => db.Perfiles.AnyAsync(p => p.Usuario.UserName == "bruno" && p.AvatarClave != null)));
        using var bitmap = new SKBitmap(20, 20); bitmap.Erase(SKColors.Coral);
        using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        using var upload = new MultipartFormDataContent { { new StringContent(token), "__RequestVerificationToken" }, { new ByteArrayContent(png.ToArray()), "avatar", "avatar.png" } };
        Assert.Equal(HttpStatusCode.Redirect, (await owner.PostAsync("/mi-perfil/avatar", upload)).StatusCode);
        var image = await visitor.GetAsync("/p/bruno/avatar");
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal("image/png", image.Content.Headers.ContentType!.MediaType);
        Assert.Contains("no-store", image.Headers.CacheControl!.ToString());
        var form = new Dictionary<string, string> { ["NombreVisible"] = "Bruno Costa", ["Visibilidad"] = "Privada", ["VisibilidadContacto"] = "Oculta", ["ColorAcento"] = "#c4f566", ["UsuarioId"] = "ajeno", ["__RequestVerificationToken"] = token };
        Assert.Equal(HttpStatusCode.Redirect, (await owner.PostAsync("/mi-perfil", new FormUrlEncodedContent(form))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync("/p/bruno")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync("/p/bruno/avatar")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync("/p/bruno/avatar")).StatusCode);
        form["Enlaces[0].Etiqueta"] = "Malicioso"; form["Enlaces[0].Url"] = "javascript:alert(1)";
        Assert.Equal(HttpStatusCode.OK, (await owner.PostAsync("/mi-perfil", new FormUrlEncodedContent(form))).StatusCode);
        Assert.False(await app.Db(db => db.EnlacesPerfil.AnyAsync(e => e.Url.StartsWith("javascript:"))));
        form.Remove("Enlaces[0].Etiqueta"); form.Remove("Enlaces[0].Url"); form["Visibilidad"] = "Publica";
        Assert.Equal(HttpStatusCode.Redirect, (await owner.PostAsync("/mi-perfil", new FormUrlEncodedContent(form))).StatusCode);
    }

    [MySqlFact]
    public async Task Administrador_completa_abm_y_no_borra_etiqueta_en_uso()
    {
        using var admin = app.Browser(); await MySqlApp.Login(admin, "admin@trazo.test");
        var token = await MySqlApp.Token(admin, "/administracion/etiquetas/nueva");
        var form = new Dictionary<string, string> { ["Nombre"] = "Prueba ABM", ["Activa"] = "true", ["__RequestVerificationToken"] = token };
        Assert.Equal(HttpStatusCode.Redirect, (await admin.PostAsync("/administracion/etiquetas/nueva", new FormUrlEncodedContent(form))).StatusCode);
        var id = await app.Db(db => db.Etiquetas.Where(e => e.Nombre == "Prueba ABM").Select(e => e.Id).SingleAsync());
        form["Nombre"] = "Prueba editada";
        Assert.Equal(HttpStatusCode.Redirect, (await admin.PostAsync($"/administracion/etiquetas/{id}/editar", new FormUrlEncodedContent(form))).StatusCode);
        Assert.True(await app.Db(db => db.Etiquetas.AnyAsync(e => e.Id == id && e.Nombre == "Prueba editada")));
        Assert.Equal(HttpStatusCode.Redirect, (await admin.PostAsync($"/administracion/etiquetas/{id}/eliminar", new FormUrlEncodedContent(form))).StatusCode);
        Assert.False(await app.Db(db => db.Etiquetas.AnyAsync(e => e.Id == id)));
        var used = await app.Db(db => db.PublicacionEtiquetas.Select(e => e.EtiquetaId).FirstAsync());
        await admin.PostAsync($"/administracion/etiquetas/{used}/eliminar", new FormUrlEncodedContent(form));
        Assert.True(await app.Db(db => db.Etiquetas.AnyAsync(e => e.Id == used)));
    }

    [MySqlFact]
    public async Task Una_sesion_existente_deja_de_funcionar_al_suspender_la_cuenta()
    {
        using var owner = app.Browser(); await MySqlApp.Login(owner, "bruno@trazo.test");
        await app.Db(async db => { var user = await db.Users.SingleAsync(u => u.UserName == "bruno"); user.Estado = EstadoCuenta.Suspendida; return await db.SaveChangesAsync(); });
        Assert.Contains("/cuenta/ingresar", (await owner.GetAsync("/panel")).Headers.Location!.ToString());
        await app.Db(async db => { var user = await db.Users.SingleAsync(u => u.UserName == "bruno"); user.Estado = EstadoCuenta.Activa; return await db.SaveChangesAsync(); });
    }
}
