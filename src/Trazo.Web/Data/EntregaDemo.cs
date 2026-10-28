using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Trazo.Web.Domain;

namespace Trazo.Web.Data;

public static class EntregaDemo
{
    private record Archivo(string ArchivoRuta, long Bytes, string Hash);
    public static async Task CompletarAsync(IServiceProvider services, TrazoDbContext db)
    {
        var env = services.GetRequiredService<IWebHostEnvironment>();
        var config = services.GetRequiredService<IConfiguration>();
        var source = Path.Combine(env.ContentRootPath, "DemoAssets");
        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(source, "manifest.json")));
        var files = json.RootElement.EnumerateArray().Select(e => new Archivo(e.GetProperty("archivo").GetString()!, e.GetProperty("bytes").GetInt64(), e.GetProperty("sha256").GetString()!)).ToList();
        foreach (var f in files)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(f.ArchivoRuta, "^(avatars|medios)/[a-f0-9]{32}(\\.preview)?\\.(png|gif|mp4)$"))
                throw new InvalidDataException("Ruta inválida en el paquete demo.");
            var bytes = await File.ReadAllBytesAsync(Path.Combine(source, f.ArchivoRuta));
            if (bytes.LongLength != f.Bytes || !Convert.ToHexString(SHA256.HashData(bytes)).Equals(f.Hash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("El paquete demo no coincide con su manifiesto.");
        }
        foreach (var f in files)
        {
            var avatar = f.ArchivoRuta.StartsWith("avatars/", StringComparison.Ordinal);
            var destination = config[avatar ? "Storage:Root" : "Media:Root"] ?? Path.Combine(env.ContentRootPath, "App_Data", avatar ? "avatars" : "medios");
            Directory.CreateDirectory(destination);
            var path = Path.Combine(destination, Path.GetFileName(f.ArchivoRuta));
            if (File.Exists(path))
            {
                if (!Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(path))).Equals(f.Hash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Ya existe un archivo distinto con una clave demo. Usá un almacenamiento vacío.");
            }
            else File.Copy(Path.Combine(source, f.ArchivoRuta), path);
        }
        var ana = await db.Users.Include(u => u.Perfil).SingleAsync(u => u.UserName == "ana");
        var bruno = await db.Users.Include(u => u.Perfil).SingleAsync(u => u.UserName == "bruno");
        var admin = await db.Users.Include(u => u.Perfil).SingleAsync(u => u.UserName == "admin");
        var date = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
        var users = new[] { ana, bruno, admin };
        for (var i = 0; i < users.Length; i++) { users[i].Perfil.AvatarClave = new string('a', 31) + (i + 1) + ".png"; users[i].FechaAlta = date.AddDays(-10); }
        ana.Perfil.EmailContacto = "contacto@ana.trazo.test"; ana.Perfil.VisibilidadContacto = VisibilidadContacto.Publica;
        bruno.Perfil.Tema = TemaPerfil.Claro; bruno.Perfil.ColorAcento = "#7dd3fc";
        db.EnlacesPerfil.Add(new EnlacePerfil { PerfilUsuarioId = ana.Id, Etiqueta = "Sitio de ejemplo", Url = "https://example.com", Orden = 0 });
        var tags = await db.Etiquetas.OrderBy(e => e.Id).ToListAsync();
        Publicacion Obra(Usuario user, string title, int file, Visibilidad visibility, int? highlight, EstadoModeracion moderation = EstadoModeracion.Visible)
        {
            var extension = file == 3 ? "gif" : file == 4 ? "mp4" : "png";
            var key = new string('b', 31) + file + "." + extension;
            var preview = new string('b', 31) + file + ".preview.png";
            return new Publicacion { AutorId = user.Id, Titulo = title, Texto = "Estudio geométrico original generado para la demostración académica de Trazo. Composición, color y movimiento con formas simples.\n\nMaterial ficticio: no representa encargos ni obras de terceros.", Tipo = TipoPublicacion.Obra,
                EstadoEditorial = EstadoEditorial.Publicada, EstadoModeracion = moderation, Visibilidad = visibility, OrdenDestacado = highlight, CreadaUtc = date, ActualizadaUtc = date, PublicadaUtc = date,
                Etiquetas = [new PublicacionEtiqueta { EtiquetaId = tags[file % tags.Count].Id }],
                Recursos = [new RecursoMultimedia { Tipo = file == 3 ? TipoRecurso.Gif : file == 4 ? TipoRecurso.Video : TipoRecurso.Imagen, ClaveAlmacenamiento = key,
                    Mime = file == 3 ? "image/gif" : file == 4 ? "video/mp4" : "image/png", Bytes = files.Single(f => f.ArchivoRuta == "medios/" + key).Bytes + files.Single(f => f.ArchivoRuta == "medios/" + preview).Bytes,
                    EstadoProcesamiento = EstadoProcesamiento.Listo, TextoAlternativo = "Anillos y bandas geométricas en una composición de colores", Creditos = "Material demo generado por tools/Generar-Assets-Demo.mjs" }] };
        }
        var works = new[] {
            Obra(ana, "Atlas de formas", 1, Visibilidad.Publica, 1), Obra(ana, "Ritmo circular", 3, Visibilidad.Publica, 2),
            Obra(ana, "Proceso para la comunidad", 5, Visibilidad.Registrados, 3), Obra(bruno, "Movimiento en un segundo", 4, Visibilidad.Publica, 1),
            Obra(bruno, "Identidad en color", 2, Visibilidad.Publica, 2), Obra(bruno, "Propuesta reservada", 6, Visibilidad.Privada, null),
            Obra(bruno, "Obra en revisión", 7, Visibilidad.Publica, null, EstadoModeracion.Oculta)
        };
        db.Publicaciones.AddRange(works); await db.SaveChangesAsync();
        db.Favoritos.AddRange(new Favorito { UsuarioId = ana.Id, PublicacionId = works[3].Id, CreadoUtc = date }, new Favorito { UsuarioId = bruno.Id, PublicacionId = works[0].Id, CreadoUtc = date });
        db.Seguimientos.AddRange(new Seguimiento { SeguidorId = ana.Id, SeguidoId = bruno.Id, CreadoUtc = date }, new Seguimiento { SeguidorId = bruno.Id, SeguidoId = ana.Id, CreadoUtc = date });
        db.Reportes.AddRange(new Reporte { DenuncianteId = ana.Id, PublicacionId = works[4].Id, Motivo = "Otro", Detalle = "Caso ficticio pendiente para practicar la revisión administrativa.", CreadoUtc = date },
            new Reporte { DenuncianteId = ana.Id, PublicacionId = works[6].Id, Motivo = "Derechos de autor", Detalle = "Caso ficticio: se solicita aclarar la autoría del material.", CreadoUtc = date, Estado = EstadoReporte.Resuelto, ResueltoPorId = admin.Id, ResueltoUtc = date.AddHours(1), Resolucion = "Decisión ficticia: obra oculta mientras se revisan sus créditos. El autor puede solicitar revisión." });
        db.CambiosEstadoCuenta.AddRange(new CambioEstadoCuenta { UsuarioId = bruno.Id, AdministradorId = admin.Id, EstadoAnterior = EstadoCuenta.Activa, EstadoNuevo = EstadoCuenta.Suspendida, Motivo = "Historial ficticio de suspensión para la defensa académica.", CreadoUtc = date.AddDays(-2) },
            new CambioEstadoCuenta { UsuarioId = bruno.Id, AdministradorId = admin.Id, EstadoAnterior = EstadoCuenta.Suspendida, EstadoNuevo = EstadoCuenta.Activa, Motivo = "Historial ficticio de reactivación: la cuenta demo queda activa.", CreadoUtc = date.AddDays(-1) });
        await db.SaveChangesAsync();
    }
}
