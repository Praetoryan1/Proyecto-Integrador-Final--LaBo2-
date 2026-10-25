using Microsoft.EntityFrameworkCore;
using Trazo.Web.Data;
using Trazo.Web.Domain;
using Trazo.Web.Models;

namespace Trazo.Web.Services;

public class PublicacionException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}

// Las mismas reglas de escritura podrán ser usadas por la futura API JWT.
public class PublicacionesService(TrazoDbContext db, AlmacenMedios almacen)
{
    public Task<Pagina<PublicacionResumen>> ListarAsync(string autorId, string? q, EstadoEditorial? estado, int pagina, CancellationToken ct)
    {
        var query = db.Publicaciones.AsNoTracking().Where(p => p.AutorId == autorId);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(p => p.Titulo.Contains(q.Trim()));
        if (estado.HasValue) query = query.Where(p => p.EstadoEditorial == estado);
        return query.OrderByDescending(p => p.ActualizadaUtc).ThenByDescending(p => p.Id)
            .Select(p => new PublicacionResumen(p.Id, p.Titulo, p.Tipo, p.EstadoEditorial, p.Visibilidad,
                p.EstadoModeracion, p.ActualizadaUtc, p.Version)).PaginarAsync(pagina, ct: ct);
    }

    public async Task<PublicacionEditor> ObtenerAsync(string autorId, long id, CancellationToken ct)
    {
        var post = await db.Publicaciones.AsNoTracking().Where(p => p.AutorId == autorId && p.Id == id)
            .Include(p => p.Etiquetas).ThenInclude(e => e.Etiqueta).Include(p => p.Recursos).AsSplitQuery().SingleOrDefaultAsync(ct)
            ?? throw NoEncontrada();
        return Proyectar(post);
    }

    public async Task<PublicacionEditor> CrearAsync(string autorId, GuardarPublicacionModel model, CancellationToken ct)
    {
        var post = new Publicacion { AutorId = autorId };
        await ValidarAsync(post, model, ct);
        Aplicar(post, model);
        post.Etiquetas = model.EtiquetaIds.Select(id => new PublicacionEtiqueta { EtiquetaId = id }).ToList();
        db.Publicaciones.Add(post);
        await db.SaveChangesAsync(ct);
        return await ObtenerAsync(autorId, post.Id, ct);
    }

    public async Task<PublicacionEditor> EditarAsync(string autorId, long id, EditarPublicacionModel model, CancellationToken ct)
    {
        var post = await PropiaAsync(autorId, id, ct);
        ComprobarVersion(post, model.Version);
        await ValidarAsync(post, model, ct);
        Aplicar(post, model);
        foreach (var old in post.Etiquetas.Where(e => !model.EtiquetaIds.Contains(e.EtiquetaId)).ToList())
            db.PublicacionEtiquetas.Remove(old);
        var previousIds = post.Etiquetas.Select(e => e.EtiquetaId).ToHashSet();
        foreach (var tag in model.EtiquetaIds.Where(id => !previousIds.Contains(id)))
            post.Etiquetas.Add(new PublicacionEtiqueta { EtiquetaId = tag });
        await GuardarConVersionAsync(ct);
        return await ObtenerAsync(autorId, id, ct);
    }

    public async Task EliminarAsync(string autorId, long id, string version, CancellationToken ct)
    {
        var post = await PropiaAsync(autorId, id, ct);
        ComprobarVersion(post, version);
        if (await db.Reportes.AnyAsync(r => r.PublicacionId == id, ct))
            throw new PublicacionException(409, "Esta publicación tiene reportes vinculados. Podés archivarla para retirar su difusión.");
        var keys = await db.Recursos.Where(r => r.PublicacionId == id).Select(r => r.ClaveAlmacenamiento).ToListAsync(ct);
        db.Publicaciones.Remove(post);
        try { await GuardarConVersionAsync(ct); }
        catch (DbUpdateException) { throw new PublicacionException(409, "No se pudo eliminar porque cambió una relación. Volvé a cargar la publicación o archivala."); }
        foreach (var key in keys) almacen.Eliminar(key);
    }

    private async Task<Publicacion> PropiaAsync(string autorId, long id, CancellationToken ct) =>
        await db.Publicaciones.Include(p => p.Etiquetas).SingleOrDefaultAsync(p => p.Id == id && p.AutorId == autorId, ct)
        ?? throw NoEncontrada();

    private async Task ValidarAsync(Publicacion post, GuardarPublicacionModel model, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model.Titulo) || string.IsNullOrWhiteSpace(model.Texto))
            throw new PublicacionException(400, "Completá el título y el texto.");
        var ids = model.EtiquetaIds;
        if (ids.Length > 8 || ids.Any(id => id <= 0) || ids.Distinct().Count() != ids.Length)
            throw new PublicacionException(400, "Elegí hasta 8 etiquetas diferentes y válidas.");
        var attached = post.Etiquetas.Select(e => e.EtiquetaId).ToArray();
        var available = await db.Etiquetas.CountAsync(e => ids.Contains(e.Id) && (e.Activa || attached.Contains(e.Id)), ct);
        if (available != ids.Length)
            throw new PublicacionException(400, "Una etiqueta no existe o fue desactivada. Quitala y buscá otra.");
        if (model.Tipo == TipoPublicacion.Obra && model.EstadoEditorial == EstadoEditorial.Publicada &&
            !await db.Recursos.AnyAsync(r => r.PublicacionId == post.Id && r.EstadoProcesamiento == EstadoProcesamiento.Listo, ct))
            throw new PublicacionException(400, "Para publicar una obra necesitás al menos un recurso listo. Guardala como borrador, agregá un archivo o enlace de YouTube y luego publicala.");
    }

    private static void Aplicar(Publicacion post, GuardarPublicacionModel model)
    {
        post.Titulo = model.Titulo.Trim();
        post.Texto = model.Texto.Trim();
        post.Tipo = model.Tipo;
        post.Visibilidad = model.Visibilidad;
        post.EstadoEditorial = model.EstadoEditorial;
        post.ActualizadaUtc = DateTime.UtcNow;
        if (model.EstadoEditorial == EstadoEditorial.Publicada && post.PublicadaUtc is null)
            post.PublicadaUtc = post.ActualizadaUtc;
        post.Version = Guid.NewGuid().ToString("N");
        // AutorId, CreadaUtc y EstadoModeracion nunca provienen del formulario.
    }

    private static void ComprobarVersion(Publicacion post, string version)
    {
        if (post.Version != version) throw Conflicto();
    }

    private async Task GuardarConVersionAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw Conflicto(); }
    }

    private static PublicacionEditor Proyectar(Publicacion p) => new(p.Id, p.Titulo, p.Texto, p.Tipo, p.EstadoEditorial,
        p.Visibilidad, p.EstadoModeracion, p.Version,
        p.Etiquetas.OrderBy(e => e.Etiqueta.Nombre).Select(e => new EtiquetaEditor(e.EtiquetaId, e.Etiqueta.Nombre, e.Etiqueta.Activa)).ToList(),
        p.Recursos.Where(r => r.EstadoProcesamiento == EstadoProcesamiento.Listo).OrderBy(r => r.Orden)
            .Select(r => new RecursoVista(r.Id, r.Tipo, r.Orden, r.TextoAlternativo, r.Creditos, r.YouTubeId)).ToList());
    private static PublicacionException NoEncontrada() => new(404, "La publicación no está disponible.");
    private static PublicacionException Conflicto() => new(409, "La publicación cambió en otra pestaña o sesión. Conservá tu texto y volvé a cargar su versión actual antes de guardar.");
}
