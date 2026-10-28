using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trazo.Web.Data;
using Trazo.Web.Domain;

namespace Trazo.Tests;

public class EntregaApp : MySqlApp
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseEnvironment("Development");
    }
}

[Collection("mysql")]
public class EntregaIntegrationTests(EntregaApp app) : IClassFixture<EntregaApp>
{
    [MySqlFact]
    public async Task Demo_completa_restaurable_con_archivos_independientes_privacidad_y_sin_sobrescritura()
    {
        // El fixture ya verificó que solo se use trazo_tests en localhost.
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TrazoDbContext>();
        await db.Database.EnsureDeletedAsync();
        await DbInitializer.InitializeAsync(scope.ServiceProvider, demo: true, entrega: true);
        Assert.Equal(28, await db.Publicaciones.CountAsync());
        Assert.Equal(3, await db.Perfiles.CountAsync(p => p.AvatarClave != null));
        Assert.Equal(2, await db.Reportes.CountAsync());
        Assert.Equal(2, await db.CambiosEstadoCuenta.CountAsync());
        Assert.Equal(2, await db.Favoritos.CountAsync());
        Assert.Equal(2, await db.Seguimientos.CountAsync());
        var resources = await db.Recursos.Include(r => r.Publicacion).ToListAsync();
        Assert.Equal(7, resources.Count);
        Assert.Equal(7, resources.Select(r => r.ClaveAlmacenamiento).Distinct().Count());
        using var anon = app.Browser(); using var ana = app.Browser(); using var admin = app.Browser();
        await MySqlApp.Login(ana, "ana@trazo.test"); await MySqlApp.Login(admin, "admin@trazo.test");
        foreach (var resource in resources)
        {
            var file = Path.Combine(app.MediaRoot, resource.ClaveAlmacenamiento!);
            var preview = Path.ChangeExtension(file, ".preview.png");
            Assert.Equal(resource.Bytes, new FileInfo(file).Length + new FileInfo(preview).Length);
            var visible = resource.Publicacion.Visibilidad == Visibilidad.Publica && resource.Publicacion.EstadoModeracion == EstadoModeracion.Visible;
            foreach (var suffix in new[] { "archivo", "vista-previa" })
            {
                var url = $"/medios/{resource.Id}/{suffix}";
                Assert.Equal(visible ? HttpStatusCode.OK : HttpStatusCode.NotFound, (await anon.GetAsync(url)).StatusCode);
                if (resource.Publicacion.Visibilidad == Visibilidad.Registrados)
                    Assert.Equal(HttpStatusCode.OK, (await ana.GetAsync(url)).StatusCode);
                if (resource.Publicacion.Visibilidad == Visibilidad.Privada)
                    Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync(url)).StatusCode);
            }
        }
        var video = resources.Single(r => r.Tipo == TipoRecurso.Video);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/medios/{video.Id}/archivo");
        request.Headers.Range = new RangeHeaderValue(0, 99);
        var partial = await anon.SendAsync(request);
        Assert.Equal(HttpStatusCode.PartialContent, partial.StatusCode);
        Assert.Equal(100, (await partial.Content.ReadAsByteArrayAsync()).Length);
        foreach (var user in new[] { "ana", "bruno", "admin" })
            Assert.Equal(HttpStatusCode.OK, (await anon.GetAsync($"/p/{user}/avatar")).StatusCode);
        await Assert.ThrowsAsync<InvalidOperationException>(() => DbInitializer.InitializeAsync(scope.ServiceProvider, demo: true, entrega: true));
        Assert.Equal(28, await db.Publicaciones.CountAsync());
        // Borrar los archivos de una obra no puede quitar los de otra.
        var store = scope.ServiceProvider.GetRequiredService<Trazo.Web.Services.AlmacenMedios>();
        store.Eliminar(resources[0].ClaveAlmacenamiento);
        Assert.All(resources.Skip(1), r => Assert.True(File.Exists(store.Ruta(r.ClaveAlmacenamiento))));
    }
}
