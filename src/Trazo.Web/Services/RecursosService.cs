using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Trazo.Web.Data;
using Trazo.Web.Domain;
using Trazo.Web.Models;

namespace Trazo.Web.Services;

public class RecursosService(TrazoDbContext db, AlmacenMedios almacen, PublicacionesService publicaciones)
{
    public async Task<PublicacionEditor> SubirAsync(string autorId, long id, ArchivoEntrada model, CancellationToken ct)
    {
        var post = await PropiaAsync(autorId, id, model.Version, ct);
        if (post.Recursos.Count >= AlmacenMedios.MaxRecursos) throw Limite();
        var file = await almacen.PrepararAsync(model.Archivo, ct);
        var committed = false;
        try
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            await BloquearAutorAsync(autorId, ct);
            post = await PropiaAsync(autorId, id, model.Version, ct);
            if (post.Recursos.Count >= AlmacenMedios.MaxRecursos) throw Limite();
            var used = await db.Recursos.Where(r => r.Publicacion.AutorId == autorId).SumAsync(r => r.Bytes ?? 0, ct);
            if (used + file.Bytes > AlmacenMedios.CuotaCuenta) throw new PublicacionException(400, "Tu cuenta supera la cuota de 500 MB. Eliminá recursos que ya no necesites.");
            post.Recursos.Add(new RecursoMultimedia
            {
                Tipo = file.Tipo, ClaveAlmacenamiento = file.Clave, Mime = file.Mime, Bytes = file.Bytes,
                EstadoProcesamiento = EstadoProcesamiento.Listo, Orden = SiguienteOrden(post),
                TextoAlternativo = model.TextoAlternativo.Trim(), Creditos = model.Creditos?.Trim() ?? ""
            });
            await GuardarAsync(post, ct);
            await transaction.CommitAsync(ct);
            committed = true;
        }
        finally { if (!committed) almacen.Eliminar(file.Clave); }
        return await publicaciones.ObtenerAsync(autorId, id, ct);
    }

    public async Task<PublicacionEditor> YouTubeAsync(string autorId, long id, YouTubeEntrada model, CancellationToken ct)
    {
        var youtubeId = ExtraerYouTubeId(model.Url);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await BloquearAutorAsync(autorId, ct);
        var post = await PropiaAsync(autorId, id, model.Version, ct);
        if (post.Recursos.Count >= AlmacenMedios.MaxRecursos) throw Limite();
        post.Recursos.Add(new RecursoMultimedia
        {
            Tipo = TipoRecurso.YouTube, YouTubeId = youtubeId, EstadoProcesamiento = EstadoProcesamiento.Listo,
            Orden = SiguienteOrden(post), TextoAlternativo = model.TextoAlternativo.Trim(), Creditos = model.Creditos?.Trim() ?? ""
        });
        await GuardarAsync(post, ct);
        await transaction.CommitAsync(ct);
        return await publicaciones.ObtenerAsync(autorId, id, ct);
    }

    public async Task<PublicacionEditor> EditarAsync(string autorId, long id, long recursoId, RecursoEntrada model, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await BloquearAutorAsync(autorId, ct);
        var post = await PropiaAsync(autorId, id, model.Version, ct);
        var recurso = post.Recursos.SingleOrDefault(r => r.Id == recursoId) ?? throw NoEncontrado();
        recurso.TextoAlternativo = model.TextoAlternativo.Trim();
        recurso.Creditos = model.Creditos?.Trim() ?? "";
        await GuardarAsync(post, ct);
        await transaction.CommitAsync(ct);
        return await publicaciones.ObtenerAsync(autorId, id, ct);
    }

    public async Task<PublicacionEditor> EliminarAsync(string autorId, long id, long recursoId, string version, CancellationToken ct)
    {
        string? key;
        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            await BloquearAutorAsync(autorId, ct);
            var post = await PropiaAsync(autorId, id, version, ct);
            var recurso = post.Recursos.SingleOrDefault(r => r.Id == recursoId) ?? throw NoEncontrado();
            if (post.Tipo == TipoPublicacion.Obra && post.EstadoEditorial == EstadoEditorial.Publicada &&
                !post.Recursos.Any(r => r.Id != recursoId && r.EstadoProcesamiento == EstadoProcesamiento.Listo))
                throw new PublicacionException(400, "Una obra publicada necesita al menos un recurso. Guardala como borrador antes de quitar el último.");
            key = recurso.ClaveAlmacenamiento;
            db.Recursos.Remove(recurso);
            await GuardarAsync(post, ct);
            await transaction.CommitAsync(ct);
        }
        // Primero se quita la referencia autorizable; luego se eliminan los bytes.
        almacen.Eliminar(key);
        return await publicaciones.ObtenerAsync(autorId, id, ct);
    }

    public async Task<PublicacionEditor> OrdenarAsync(string autorId, long id, OrdenRecursosEntrada model, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await BloquearAutorAsync(autorId, ct);
        var post = await PropiaAsync(autorId, id, model.Version, ct);
        if (model.RecursoIds.Length != post.Recursos.Count || model.RecursoIds.Distinct().Count() != model.RecursoIds.Length ||
            !model.RecursoIds.ToHashSet().SetEquals(post.Recursos.Select(r => r.Id)))
            throw new PublicacionException(400, "El orden debe incluir exactamente los recursos actuales de esta publicación.");
        var resources = post.Recursos.ToDictionary(r => r.Id);
        for (var i = 0; i < model.RecursoIds.Length; i++) resources[model.RecursoIds[i]].Orden = -i - 1;
        await GuardarAsync(post, ct); // Valores transitorios evitan colisiones del índice único al intercambiar posiciones.
        for (var i = 0; i < model.RecursoIds.Length; i++) resources[model.RecursoIds[i]].Orden = i;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return await publicaciones.ObtenerAsync(autorId, id, ct);
    }

    private async Task BloquearAutorAsync(string autorId, CancellationToken ct)
    {
        // Serializa cuotas y cambios multimedia del mismo autor incluso entre instancias del servidor.
        await db.Users.FromSqlInterpolated($"SELECT * FROM Usuarios WHERE Id = {autorId} FOR UPDATE").AsNoTracking().ToListAsync(ct);
    }

    private async Task<Publicacion> PropiaAsync(string autorId, long id, string version, CancellationToken ct)
    {
        var post = await db.Publicaciones.Include(p => p.Recursos).SingleOrDefaultAsync(p => p.Id == id && p.AutorId == autorId, ct)
            ?? throw NoEncontrado();
        if (post.Version != version) throw Conflicto();
        return post;
    }

    private async Task GuardarAsync(Publicacion post, CancellationToken ct)
    {
        post.Version = Guid.NewGuid().ToString("N");
        post.ActualizadaUtc = DateTime.UtcNow;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw Conflicto(); }
    }

    public static string ExtraerYouTubeId(string input)
    {
        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri) || uri.Scheme != "https" || !uri.IsDefaultPort || !string.IsNullOrEmpty(uri.UserInfo))
            throw new PublicacionException(400, "Ingresá un enlace HTTPS de YouTube válido.");
        var parts = uri.AbsolutePath.Trim('/').Split('/');
        string? id = null;
        if (uri.Host == "youtu.be" && parts.Length == 1) id = parts[0];
        if (uri.Host is "youtube.com" or "www.youtube.com" or "m.youtube.com")
        {
            if (uri.AbsolutePath == "/watch")
            {
                var query = QueryHelpers.ParseQuery(uri.Query);
                if (query.TryGetValue("v", out var value) && value.Count == 1) id = value[0];
            }
            else if (parts.Length == 2 && parts[0] is "embed" or "shorts") id = parts[1];
        }
        if (id is null || !Regex.IsMatch(id, "^[A-Za-z0-9_-]{11}$"))
            throw new PublicacionException(400, "Usá un enlace de video de youtube.com o youtu.be. No se admite HTML ni otras plataformas.");
        return id; // No se hace ninguna solicitud del servidor a la URL recibida.
    }

    private static int SiguienteOrden(Publicacion p) => p.Recursos.Count == 0 ? 0 : p.Recursos.Max(r => r.Orden) + 1;
    private static PublicacionException NoEncontrado() => new(404, "La publicación o el recurso no están disponibles.");
    private static PublicacionException Limite() => new(400, "Cada publicación admite hasta 10 recursos.");
    private static PublicacionException Conflicto() => new(409, "La publicación cambió. Conservá tus cambios y volvé a cargarla antes de modificar sus recursos.");
}
