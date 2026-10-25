using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Trazo.Web.Domain;
using Trazo.Web.Models;
using Trazo.Web.Services;

namespace Trazo.Tests;

public class ApiEntornoTests
{
    [Theory]
    [InlineData("Production", "/api/v1/auth/token", 404)]
    [InlineData("Staging", "/api/v1/publicaciones", 404)]
    [InlineData("Testing", "/api/v1/publicaciones", 503)]
    public async Task Api_no_se_abre_fuera_del_entorno_permitido_o_sin_clave(string environment, string url, int expected)
    {
        await using var app = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment(environment)
            .ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?>
            { ["Jwt:SigningKey"] = environment == "Testing" ? "" : Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)) })));
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        Assert.Equal(expected, (int)(await client.PostAsJsonAsync(url, new { })).StatusCode);
    }
}

[Collection("mysql")]
public class ApiIntegrationTests(MySqlApp app) : IClassFixture<MySqlApp>
{
    private const string Url = "/api/v1/publicaciones";
    private const string TokenUrl = "/api/v1/auth/token";
    private async Task<HttpClient> Api(string email = "ana@trazo.test")
    {
        var client = app.Browser();
        var response = await client.PostAsJsonAsync(TokenUrl, new { Email = email, Password = "Trazo.Demo2026!" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
        Assert.True(response.Headers.CacheControl!.NoStore);
        var token = (await response.Content.ReadFromJsonAsync<TokenRespuesta>())!;
        Assert.Equal("Bearer", token.TokenType); Assert.Equal(600, token.ExpiresIn);
        Assert.Equal(3, token.AccessToken.Split('.').Length);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return client;
    }
    private static GuardarPublicacionModel Nuevo() => new()
    { Titulo = "API: estudio creativo", Texto = "Texto privado de prueba.", Tipo = TipoPublicacion.Actualizacion, EstadoEditorial = EstadoEditorial.Borrador, Visibilidad = Visibilidad.Privada };
    private static EditarPublicacionModel Editar(PublicacionEditor p) => new()
    { Titulo = p.Titulo, Texto = "Texto editado por API.", Tipo = p.Tipo, EstadoEditorial = p.EstadoEditorial, Visibilidad = p.Visibilidad, Version = p.Version, EtiquetaIds = p.Etiquetas.Select(t => t.Id).ToArray() };
    private static Task<HttpResponseMessage> Borrar(HttpClient c, PublicacionEditor p) => c.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"{Url}/{p.Id}") { Content = JsonContent.Create(new { p.Version }) });

    [MySqlFact]
    public async Task Crud_rest_con_jwt_pagina_valida_propiedad_y_comparte_reglas_con_mvc()
    {
        using var owner = await Api(); using var other = await Api("bruno@trazo.test"); using var admin = await Api("admin@trazo.test");
        var tags = (await owner.GetFromJsonAsync<Pagina<EtiquetaEditor>>("/api/v1/etiquetas?q=Anima"))!;
        Assert.NotEmpty(tags.Items); Assert.Equal(10, tags.Tamano);
        var model = Nuevo(); model.EtiquetaIds = [tags.Items[0].Id];
        var response = await owner.PostAsJsonAsync(Url, model);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var post = (await response.Content.ReadFromJsonAsync<PublicacionEditor>())!;
        Assert.Equal($"https://localhost{Url}/{post.Id}", response.Headers.Location!.ToString());
        Assert.Single(post.Etiquetas);
        foreach (var stranger in new[] { other, admin })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"{Url}/{post.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await stranger.PutAsJsonAsync($"{Url}/{post.Id}", Editar(post))).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await Borrar(stranger, post)).StatusCode);
        }
        var page = (await owner.GetFromJsonAsync<Pagina<PublicacionResumen>>($"{Url}?pagina=2"))!;
        Assert.Equal(2, page.Numero); Assert.Equal(12, page.Tamano); Assert.Single(page.Items);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.GetAsync($"{Url}?estado=999")).StatusCode);
        var invalid = Nuevo(); invalid.Tipo = TipoPublicacion.Obra; invalid.EstadoEditorial = EstadoEditorial.Publicada;
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync(Url, invalid)).StatusCode);
        var savedResponse = await owner.PutAsJsonAsync($"{Url}/{post.Id}", Editar(post));
        Assert.Equal(HttpStatusCode.OK, savedResponse.StatusCode);
        var saved = (await savedResponse.Content.ReadFromJsonAsync<PublicacionEditor>())!;
        Assert.NotEqual(post.Version, saved.Version);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PutAsJsonAsync($"{Url}/{post.Id}", Editar(post))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Borrar(owner, post)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Borrar(owner, saved)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"{Url}/{post.Id}")).StatusCode);
    }

    [MySqlFact]
    public async Task Cookies_y_bearer_no_se_sustituyen_y_token_por_query_no_se_acepta()
    {
        using var cookie = app.Browser(); await MySqlApp.Login(cookie, "ana@trazo.test");
        var denied = await cookie.GetAsync(Url);
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        Assert.Contains(denied.Headers.WwwAuthenticate, h => h.Scheme == "Bearer");
        Assert.Equal(HttpStatusCode.Unauthorized, (await cookie.PostAsJsonAsync(Url, Nuevo())).StatusCode);
        using var jwt = await Api();
        var panel = await jwt.GetAsync("/panel");
        Assert.Equal(HttpStatusCode.Redirect, panel.StatusCode);
        Assert.Contains("/cuenta/ingresar", panel.Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await jwt.PostAsJsonAsync("/panel-json/publicaciones", Nuevo())).StatusCode);
        var token = jwt.DefaultRequestHeaders.Authorization!.Parameter;
        using var visitor = app.Browser();
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.GetAsync(Url + "?access_token=" + token)).StatusCode);
    }

    [MySqlFact]
    public async Task Firma_emisor_audiencia_tiempos_tipo_algoritmo_y_claims_son_obligatorios()
    {
        using var client = await Api();
        var original = new JwtSecurityTokenHandler().ReadJwtToken(client.DefaultRequestHeaders.Authorization!.Parameter);
        var key = new SymmetricSecurityKey(Convert.FromBase64String(app.JwtKey));
        string Mint(string issuer = JwtLocalSettings.Issuer, string audience = JwtLocalSettings.Audience, int expiry = 600,
            int nbf = -1, string algorithm = SecurityAlgorithms.HmacSha256, string type = "at+jwt", bool signed = true,
            bool expiration = true, bool subject = true, bool stamp = true, bool wrongKey = false)
        {
            var claims = original.Claims.Where(c => c.Type is "sub" or "stamp" or "role")
                .Where(c => subject || c.Type != "sub").Where(c => stamp || c.Type != "stamp");
            var signingKey = wrongKey ? new SymmetricSecurityKey(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64)) : key;
            // HS384 necesita más bytes: usar una clave distinta también debe rechazarse.
            if (algorithm == SecurityAlgorithms.HmacSha384) signingKey = new SymmetricSecurityKey(new byte[64]);
            var token = new JwtSecurityToken(issuer, audience, claims, DateTime.UtcNow.AddSeconds(nbf),
                expiration ? DateTime.UtcNow.AddSeconds(expiry) : null, signed ? new SigningCredentials(signingKey, algorithm) : null);
            token.Header["typ"] = type;
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        var tokens = new[] { Mint(issuer: "otro"), Mint(audience: "otra"), Mint(expiry: -60, nbf: -120), Mint(nbf: 120),
            Mint(wrongKey: true), Mint(algorithm: SecurityAlgorithms.HmacSha384), Mint(type: "JWT"), Mint(signed: false),
            Mint(expiration: false), Mint(subject: false), Mint(stamp: false), "texto.invalido.token" };
        foreach (var token in tokens)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Url)).StatusCode);
        }
    }

    [MySqlFact]
    public async Task Revocacion_invalida_tokens_y_cookies_existentes()
    {
        using var api = await Api(); using var cookie = app.Browser(); await MySqlApp.Login(cookie, "ana@trazo.test");
        Assert.Equal(HttpStatusCode.NoContent, (await api.PostAsync("/api/v1/auth/revocar-sesiones", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.GetAsync(Url)).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await cookie.GetAsync("/panel")).StatusCode);
        using var fresh = await Api();
        Assert.Equal(HttpStatusCode.OK, (await fresh.GetAsync(Url)).StatusCode);
    }

    [MySqlFact]
    public async Task Permisos_y_estado_de_cuenta_se_revisan_en_cada_peticion()
    {
        using var client = await Api();
        using var scope = app.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var user = (await users.FindByEmailAsync("ana@trazo.test"))!;
        try
        {
            Assert.True((await users.RemoveFromRoleAsync(user, Roles.Creador)).Succeeded);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Url)).StatusCode);
            Assert.True((await users.AddToRoleAsync(user, Roles.Creador)).Succeeded);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(Url)).StatusCode);
            await app.Db(db => db.Users.Where(u => u.Id == user.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.Estado, EstadoCuenta.Suspendida)));
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Url)).StatusCode);
        }
        finally { await app.Db(db => db.Users.Where(u => u.Id == user.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.Estado, EstadoCuenta.Activa))); }
    }

    [MySqlFact]
    public async Task Emisor_limita_solicitudes_por_ip_antes_de_validar_credenciales()
    {
        // Instancia aislada del limitador para no depender del orden de otras pruebas.
        await using var isolated = app.WithWebHostBuilder(_ => { });
        using var client = isolated.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        for (var i = 0; i < 20; i++)
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(TokenUrl, new { })).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync(TokenUrl, new { })).StatusCode);
    }

    [MySqlFact]
    public async Task Emision_aplica_bloqueo_identity_y_no_emite_con_password_incorrecto()
    {
        using var client = app.Browser();
        using var scope = app.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var user = new Usuario { UserName = "jwtlock", Email = "jwtlock@trazo.test" };
        Assert.True((await users.CreateAsync(user, "Trazo.Demo2026!")).Succeeded);
        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(TokenUrl, new { user.Email, Password = "Incorrecta123!" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(TokenUrl, new { user.Email, Password = "Trazo.Demo2026!" })).StatusCode);
        var locked = (await users.FindByIdAsync(user.Id))!;
        await scope.ServiceProvider.GetRequiredService<Trazo.Web.Data.TrazoDbContext>().Entry(locked).ReloadAsync();
        Assert.True(await users.IsLockedOutAsync(locked));
    }
}
