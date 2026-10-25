using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trazo.Web.Domain;
using Trazo.Web.Services;

namespace Trazo.Tests;

[Collection("mysql")]
public class ComunidadIntegrationTests(MySqlApp app) : IClassFixture<MySqlApp>
{
    private async Task<HttpClient> Creador(string email = "ana@trazo.test")
    {
        var client = app.Browser();
        await MySqlApp.Login(client, email);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", await MySqlApp.Token(client, "/panel"));
        return client;
    }
    private Task<string> Id(string name) => app.Db(db => db.Users.Where(u => u.UserName == name).Select(u => u.Id).SingleAsync());
    private Task<long> Publica() => app.Db(db => db.Publicaciones.Where(p => p.Autor.UserName == "bruno" && p.Visibilidad == Visibilidad.Publica).Select(p => p.Id).FirstAsync());
    private static Task<HttpResponseMessage> Post(HttpClient client, string url, params (string Key, string Value)[] values) =>
        client.PostAsync(url, new FormUrlEncodedContent(values.Select(v => new KeyValuePair<string, string>(v.Key, v.Value))));
    private Task Limpiar(string id) => app.Db(async db =>
    {
        await db.Favoritos.Where(f => f.UsuarioId == id).ExecuteDeleteAsync();
        return await db.Seguimientos.Where(s => s.SeguidorId == id).ExecuteDeleteAsync();
    });

    [MySqlFact]
    public async Task Favoritos_y_seguidos_son_privados_idempotentes_y_no_aceptan_otro_actor()
    {
        using var ana = await Creador(); using var bruno = await Creador("bruno@trazo.test");
        var anaId = await Id("ana"); var brunoId = await Id("bruno"); var post = await Publica();
        try
        {
            var repeated = await Task.WhenAll(Post(ana, $"/favoritos/{post}/guardar", ("UsuarioId", brunoId)), Post(ana, $"/favoritos/{post}/guardar"));
            Assert.All(repeated, r => Assert.Equal(HttpStatusCode.Redirect, r.StatusCode));
            repeated = await Task.WhenAll(Post(ana, "/seguimientos/bruno/seguir"), Post(ana, "/seguimientos/bruno/seguir"));
            Assert.All(repeated, r => Assert.Equal(HttpStatusCode.Redirect, r.StatusCode));
            Assert.Equal(1, await app.Db(db => db.Favoritos.CountAsync(f => f.UsuarioId == anaId && f.PublicacionId == post)));
            Assert.Equal(1, await app.Db(db => db.Seguimientos.CountAsync(s => s.SeguidorId == anaId && s.SeguidoId == brunoId)));
            Assert.Equal(0, await app.Db(db => db.Favoritos.CountAsync(f => f.UsuarioId == brunoId)));
            Assert.Contains("class=\"work-card\"", await ana.GetStringAsync("/mis-favoritos"));
            Assert.DoesNotContain("class=\"work-card\"", await bruno.GetStringAsync("/mis-favoritos?usuarioId=" + anaId));
            Assert.DoesNotContain("class=\"follow-card\"", await bruno.GetStringAsync("/mis-seguidos?seguidorId=" + anaId));
            Assert.Contains("Quitar de favoritos", await ana.GetStringAsync($"/publicaciones/{post}"));
            Assert.Contains("Dejar de seguir", await ana.GetStringAsync("/p/bruno"));
            Assert.Equal(HttpStatusCode.Redirect, (await Post(bruno, $"/favoritos/{post}/quitar", ("UsuarioId", anaId))).StatusCode);
            Assert.True(await app.Db(db => db.Favoritos.AnyAsync(f => f.UsuarioId == anaId && f.PublicacionId == post)));
            for (var i = 0; i < 2; i++)
            {
                Assert.Equal(HttpStatusCode.Redirect, (await Post(ana, $"/favoritos/{post}/quitar")).StatusCode);
                Assert.Equal(HttpStatusCode.Redirect, (await Post(ana, "/seguimientos/bruno/quitar")).StatusCode);
            }
            Assert.DoesNotContain("class=\"work-card\"", await ana.GetStringAsync("/mis-favoritos"));
        }
        finally { await Limpiar(anaId); await Limpiar(brunoId); }
    }

    [MySqlFact]
    public async Task Mutaciones_exigen_sesion_csrf_post_y_retorno_local()
    {
        using var anonymous = app.Browser(); using var owner = await Creador();
        using var noCsrf = app.Browser(); await MySqlApp.Login(noCsrf, "ana@trazo.test");
        var id = await Id("ana"); var post = await Publica();
        try
        {
            foreach (var path in new[] { "/mis-favoritos", "/mis-seguidos" })
                Assert.Equal(HttpStatusCode.Redirect, (await anonymous.GetAsync(path)).StatusCode);
            foreach (var path in new[] { $"/favoritos/{post}/guardar", $"/favoritos/{post}/quitar", "/seguimientos/bruno/seguir", "/seguimientos/bruno/quitar", "/mis-favoritos/limpiar", "/mis-seguidos/limpiar" })
            {
                Assert.Equal(HttpStatusCode.Redirect, (await Post(anonymous, path)).StatusCode);
                Assert.Equal(HttpStatusCode.BadRequest, (await Post(noCsrf, path)).StatusCode);
                Assert.Equal(HttpStatusCode.MethodNotAllowed, (await owner.GetAsync(path)).StatusCode);
            }
            var redirect = await Post(owner, $"/favoritos/{post}/guardar", ("returnUrl", "https://example.invalid/robar"));
            Assert.Equal("/mis-favoritos", redirect.Headers.Location!.ToString());
            Assert.Equal(HttpStatusCode.BadRequest, (await owner.GetAsync("/mis-favoritos?pagina=texto")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await owner.GetAsync("/mis-seguidos?q=" + new string('a', 101))).StatusCode);
        }
        finally { await Limpiar(id); }
    }

    [MySqlFact]
    public async Task Guardar_y_seguir_no_otorgan_acceso_ni_permiten_autoseguimiento()
    {
        using var viewer = await Creador("bruno@trazo.test");
        var id = await Id("bruno");
        try
        {
            var privatePosts = await app.Db(db => db.Publicaciones.Where(p => p.Autor.UserName == "ana" &&
                (p.Visibilidad == Visibilidad.Privada || p.EstadoEditorial == EstadoEditorial.Borrador)).Select(p => p.Id).ToArrayAsync());
            Assert.Equal(HttpStatusCode.Redirect, (await Post(viewer, "/seguimientos/ana/seguir")).StatusCode);
            foreach (var p in privatePosts)
            {
                Assert.Equal(HttpStatusCode.NotFound, (await Post(viewer, $"/favoritos/{p}/guardar")).StatusCode);
                Assert.Equal(HttpStatusCode.NotFound, (await viewer.GetAsync($"/publicaciones/{p}")).StatusCode);
            }
            Assert.Equal(HttpStatusCode.BadRequest, (await Post(viewer, "/seguimientos/bruno/seguir")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await Post(viewer, "/seguimientos/no_existe/seguir")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await Post(viewer, "/favoritos/9223372036854775807/guardar")).StatusCode);
            var registered = await app.Db(db => db.Publicaciones.Where(p => p.Autor.UserName == "ana" && p.Visibilidad == Visibilidad.Registrados).Select(p => p.Id).SingleAsync());
            Assert.Equal(HttpStatusCode.Redirect, (await Post(viewer, $"/favoritos/{registered}/guardar")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync($"/publicaciones/{registered}")).StatusCode);
        }
        finally { await Limpiar(id); }
    }

    [MySqlFact]
    public async Task Privacidad_filtra_listas_contadores_y_limpieza_sin_revelar_destinos()
    {
        using var viewer = await Creador();
        var id = await Id("ana"); var other = await Id("bruno"); var post = await Publica();
        try
        {
            await Post(viewer, $"/favoritos/{post}/guardar"); await Post(viewer, "/seguimientos/bruno/seguir");
            await app.Db(db => db.Perfiles.Where(p => p.UsuarioId == other).ExecuteUpdateAsync(s => s.SetProperty(p => p.Visibilidad, Visibilidad.Privada)));
            Assert.DoesNotContain("class=\"work-card\"", await viewer.GetStringAsync("/mis-favoritos"));
            Assert.DoesNotContain("class=\"follow-card\"", await viewer.GetStringAsync("/mis-seguidos"));
            Assert.Equal(HttpStatusCode.NotFound, (await Post(viewer, "/seguimientos/bruno/seguir")).StatusCode);
            var panel = await viewer.GetStringAsync("/panel");
            Assert.Contains("FAVORITOS GUARDADOS</span><strong>0</strong>", panel);
            Assert.Contains("CREADORES QUE SEGUÍS</span><strong>0</strong>", panel);
            Assert.Equal(HttpStatusCode.Redirect, (await Post(viewer, "/mis-favoritos/limpiar")).StatusCode);
            Assert.Equal(HttpStatusCode.Redirect, (await Post(viewer, "/mis-seguidos/limpiar")).StatusCode);
            Assert.False(await app.Db(db => db.Favoritos.AnyAsync(f => f.UsuarioId == id)));
            Assert.False(await app.Db(db => db.Seguimientos.AnyAsync(s => s.SeguidorId == id)));
        }
        finally
        {
            await app.Db(db => db.Perfiles.Where(p => p.UsuarioId == other).ExecuteUpdateAsync(s => s.SetProperty(p => p.Visibilidad, Visibilidad.Publica)));
            await Limpiar(id);
        }
    }

    [MySqlFact]
    public async Task Archivo_moderacion_suspension_y_baja_se_reflejan_en_favoritos()
    {
        using var viewer = await Creador();
        var id = await Id("ana"); var other = await Id("bruno");
        var post = await app.Db(async db => { var p = new Publicacion { AutorId = other, Titulo = "Temporal favorito", Texto = "Prueba", Tipo = TipoPublicacion.Actualizacion, EstadoEditorial = EstadoEditorial.Publicada }; db.Publicaciones.Add(p); await db.SaveChangesAsync(); return p.Id; });
        try
        {
            await Post(viewer, $"/favoritos/{post}/guardar"); await Post(viewer, "/seguimientos/bruno/seguir");
            await app.Db(db => db.Publicaciones.Where(p => p.Id == post).ExecuteUpdateAsync(s => s.SetProperty(p => p.EstadoEditorial, EstadoEditorial.Archivada)));
            Assert.DoesNotContain("class=\"work-card\"", await viewer.GetStringAsync("/mis-favoritos"));
            Assert.Equal(HttpStatusCode.NotFound, (await Post(viewer, $"/favoritos/{post}/guardar")).StatusCode);
            await app.Db(db => db.Publicaciones.Where(p => p.Id == post).ExecuteUpdateAsync(s => s.SetProperty(p => p.EstadoEditorial, EstadoEditorial.Publicada).SetProperty(p => p.EstadoModeracion, EstadoModeracion.Oculta)));
            Assert.DoesNotContain("class=\"work-card\"", await viewer.GetStringAsync("/mis-favoritos"));
            await app.Db(db => db.Publicaciones.Where(p => p.Id == post).ExecuteUpdateAsync(s => s.SetProperty(p => p.EstadoModeracion, EstadoModeracion.Visible)));
            Assert.Contains("class=\"work-card\"", await viewer.GetStringAsync("/mis-favoritos"));
            await app.Db(db => db.Users.Where(u => u.Id == other).ExecuteUpdateAsync(s => s.SetProperty(u => u.Estado, EstadoCuenta.Suspendida)));
            Assert.DoesNotContain("class=\"work-card\"", await viewer.GetStringAsync("/mis-favoritos"));
            Assert.DoesNotContain("class=\"follow-card\"", await viewer.GetStringAsync("/mis-seguidos"));
            await app.Db(db => db.Publicaciones.Where(p => p.Id == post).ExecuteDeleteAsync());
            Assert.False(await app.Db(db => db.Favoritos.AnyAsync(f => f.PublicacionId == post)));
        }
        finally
        {
            await Limpiar(id);
            await app.Db(db => db.Users.Where(u => u.Id == other).ExecuteUpdateAsync(s => s.SetProperty(u => u.Estado, EstadoCuenta.Activa)));
            await app.Db(db => db.Publicaciones.Where(p => p.Id == post).ExecuteDeleteAsync());
        }
    }

    [MySqlFact]
    public async Task Ambas_listas_paginan_en_mysql_y_filtran_antes_de_contar()
    {
        using var viewer = await Creador(); var id = await Id("ana");
        var synthetic = new List<string>(); var posts = new List<long>();
        try
        {
            await app.Db(async db =>
            {
                for (var i = 0; i < 14; i++)
                {
                    var user = new Usuario { UserName = $"social{i}", NormalizedUserName = $"SOCIAL{i}", Email = $"social{i}@trazo.test", NormalizedEmail = $"SOCIAL{i}@TRAZO.TEST" };
                    user.Perfil = new Perfil { NombreVisible = $"Creador prueba {i}", Especialidad = "Grabado", Visibilidad = i == 13 ? Visibilidad.Privada : Visibilidad.Publica };
                    db.Users.Add(user); synthetic.Add(user.Id);
                    var post = new Publicacion { Autor = user, Titulo = $"Obra grabada {i}", Texto = "Prueba", Tipo = TipoPublicacion.Actualizacion, EstadoEditorial = EstadoEditorial.Publicada };
                    db.Publicaciones.Add(post);
                    db.Seguimientos.Add(new Seguimiento { SeguidorId = id, Seguido = user });
                    db.Favoritos.Add(new Favorito { UsuarioId = id, Publicacion = post });
                }
                await db.SaveChangesAsync(); posts.AddRange(await db.Publicaciones.Where(p => synthetic.Contains(p.AutorId)).Select(p => p.Id).ToArrayAsync()); return 0;
            });
            foreach (var (path, css) in new[] { ("/mis-favoritos", "work-card"), ("/mis-seguidos", "follow-card") })
            {
                var first = await viewer.GetStringAsync(path); var second = await viewer.GetStringAsync(path + "?pagina=2");
                Assert.Equal(12, Regex.Matches(first, $"class=\"{css}\"").Count);
                Assert.Single(Regex.Matches(second, $"class=\"{css}\""));
                Assert.Contains("13 resultados", first);
                Assert.DoesNotContain("prueba 13", first + second);
                Assert.DoesNotContain("grabada 13", first + second);
            }
            Assert.Single(Regex.Matches(await viewer.GetStringAsync("/mis-favoritos?q=grabada%2012"), "class=\"work-card\""));
            Assert.Single(Regex.Matches(await viewer.GetStringAsync("/mis-seguidos?q=prueba%2012"), "class=\"follow-card\""));
        }
        finally
        {
            await Limpiar(id);
            await app.Db(async db => { await db.Publicaciones.Where(p => posts.Contains(p.Id)).ExecuteDeleteAsync(); await db.Perfiles.Where(p => synthetic.Contains(p.UsuarioId)).ExecuteDeleteAsync(); return await db.Users.Where(u => synthetic.Contains(u.Id)).ExecuteDeleteAsync(); });
        }
    }
}
