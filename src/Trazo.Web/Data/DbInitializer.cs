using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Trazo.Web.Domain;
namespace Trazo.Web.Data;

public static class DbInitializer
{
    public const string DemoPassword = "Trazo.Demo2026!";
    public static async Task InitializeAsync(IServiceProvider services, bool demo, bool entrega = false)
    {
        var db = services.GetRequiredService<TrazoDbContext>();
        await db.Database.MigrateAsync();
        if (entrega && (!demo || !services.GetRequiredService<IWebHostEnvironment>().IsDevelopment() || await db.Users.AnyAsync()))
            throw new InvalidOperationException("La demo de entrega requiere Development y una base sin usuarios. No se sobrescribe una instalación existente.");
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { Roles.Creador, Roles.Administrador })
            if (!await roles.RoleExistsAsync(role)) Check(await roles.CreateAsync(new IdentityRole(role)));
        if (!demo) return;
        var users = services.GetRequiredService<UserManager<Usuario>>();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var ana = await CrearUsuario("ana", "Ana Rivera", "Ilustración y dirección de arte", users, db);
        var bruno = await CrearUsuario("bruno", "Bruno Costa", "Animación y diseño de personajes", users, db);
        var admin = await CrearUsuario("admin", "Equipo Trazo", "Administración", users, db);
        if (!await users.IsInRoleAsync(admin, Roles.Administrador)) Check(await users.AddToRoleAsync(admin, Roles.Administrador));
        foreach (var name in new[] { "Ilustración", "Animación", "Diseño", "Proceso", "Identidad", "3D" })
            if (!await db.Etiquetas.AnyAsync(e => e.Nombre == name)) db.Etiquetas.Add(new Etiqueta { Nombre = name, NombreNormalizado = name.ToUpperInvariant() });
        await db.SaveChangesAsync();
        if (!await db.Publicaciones.AnyAsync(p => p.AutorId == ana.Id))
        {
            string[] titles = ["La forma de una idea", "Cuaderno de exploración", "Un mundo en dos colores", "Letras en movimiento", "Detrás de cada personaje", "El ritmo de lo cotidiano", "Volver a mirar", "Pequeños universos", "Una pausa entre fotogramas", "Geometría sensible", "Historias sin palabras", "Encontrar la paleta", "La ciudad imaginada", "De boceto a sistema", "Ensayos de luz", "Lo que cuenta una línea", "Un gesto, mil historias", "Notas de un nuevo comienzo"];
            var tags = await db.Etiquetas.OrderBy(e => e.Id).ToListAsync();
            for (var i = 0; i < titles.Length; i++)
            {
                var date = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc).AddHours(i * 7);
                db.Publicaciones.Add(new Publicacion
                {
                    AutorId = i % 2 == 0 ? ana.Id : bruno.Id,
                    Tipo = TipoPublicacion.Actualizacion,
                    Titulo = titles[i],
                    Texto = "Un apunte de mi proceso creativo: probar, observar y volver a empezar. Esta exploración reúne ideas sobre composición, ritmo y color.\n\nContenido original de demostración para recorrer el portafolio y probar sus filtros.",
                    EstadoEditorial = EstadoEditorial.Publicada,
                    Visibilidad = Visibilidad.Publica,
                    CreadaUtc = date,
                    ActualizadaUtc = date,
                    PublicadaUtc = date,
                    Etiquetas = [new PublicacionEtiqueta { EtiquetaId = tags[i % tags.Count].Id }]
                });
            }
            db.Publicaciones.AddRange(
                new Publicacion { AutorId = ana.Id, Tipo = TipoPublicacion.Actualizacion, Titulo = "Apunte para la comunidad", Texto = "Esta actualización requiere una cuenta activa.", EstadoEditorial = EstadoEditorial.Publicada, Visibilidad = Visibilidad.Registrados, PublicadaUtc = DateTime.UtcNow },
                new Publicacion { AutorId = ana.Id, Tipo = TipoPublicacion.Actualizacion, Titulo = "Exploración privada de Ana", Texto = "Solo Ana puede consultar este contenido.", EstadoEditorial = EstadoEditorial.Publicada, Visibilidad = Visibilidad.Privada, PublicadaUtc = DateTime.UtcNow },
                new Publicacion { AutorId = ana.Id, Tipo = TipoPublicacion.Actualizacion, Titulo = "Borrador de Ana", Texto = "Todavía no está publicado.", EstadoEditorial = EstadoEditorial.Borrador });
        }
        await db.SaveChangesAsync();
        if (entrega) await EntregaDemo.CompletarAsync(services, db);
        await transaction.CommitAsync();
    }
    private static async Task<Usuario> CrearUsuario(string slug, string name, string specialty, UserManager<Usuario> users, TrazoDbContext db)
    {
        var user = await users.FindByNameAsync(slug);
        if (user is not null) return user; // Nunca sobrescribir contraseñas o perfiles existentes.
        user = new Usuario { UserName = slug, Email = $"{slug}@trazo.test", EmailConfirmed = true };
        Check(await users.CreateAsync(user, DemoPassword));
        Check(await users.AddToRoleAsync(user, Roles.Creador));
        db.Perfiles.Add(new Perfil
        {
            UsuarioId = user.Id,
            NombreVisible = name,
            Especialidad = specialty,
            Biografia = "Me interesa encontrar historias en las formas, los colores y el movimiento. Este es mi espacio para compartir el proceso.",
            DisponibleParaTrabajar = slug != "admin"
        });
        await db.SaveChangesAsync();
        return user;
    }
    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
