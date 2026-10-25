using Microsoft.EntityFrameworkCore;
using Trazo.Web.Data;
using Trazo.Web.Domain;
using Trazo.Web.Models;

namespace Trazo.Web.Services;

public class ReportesService(TrazoDbContext db)
{
    public const string Revision = "Solicitud de revisión";
    public static readonly string[] Motivos = ["Derechos de autor", "Contenido ofensivo", "Spam o engaño", "Datos personales", "Otro"];

    // Solo para las rutas administrativas protegidas por rol. No modifica la lectura pública.
    public IQueryable<Publicacion> Moderables() => db.Publicaciones.Where(p =>
        p.Autor.Estado == EstadoCuenta.Activa && p.EstadoEditorial == EstadoEditorial.Publicada &&
        p.Visibilidad != Visibilidad.Privada && p.Autor.Perfil.Visibilidad != Visibilidad.Privada);

    public IQueryable<Publicacion> Reportables(string actor) => Moderables().Where(p =>
        (p.AutorId != actor && p.EstadoModeracion == EstadoModeracion.Visible) ||
        (p.AutorId == actor && p.EstadoModeracion == EstadoModeracion.Oculta));

    public async Task<long> CrearAsync(string actor, long id, ReporteEntrada entrada, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var post = await Reportables(actor).SingleOrDefaultAsync(p => p.Id == id, ct) ?? throw NoDisponible();
        var revision = post.AutorId == actor;
        if ((!revision && !Motivos.Contains(entrada.Motivo)) || (revision && entrada.Motivo != Revision))
            throw new PublicacionException(400, "Seleccioná un motivo válido para esta solicitud.");
        if (entrada.Detalle.Trim().Length < 10) throw new PublicacionException(400, "Explicá el motivo en al menos 10 caracteres.");
        var previous = await db.Reportes.Where(r => r.PublicacionId == id && r.DenuncianteId == actor && r.Estado == EstadoReporte.Pendiente)
            .Select(r => (long?)r.Id).FirstOrDefaultAsync(ct);
        if (previous.HasValue) return previous.Value;
        var report = new Reporte { PublicacionId = id, DenuncianteId = actor, Motivo = entrada.Motivo, Detalle = entrada.Detalle.Trim() };
        // Tomar la fila antes de insertar la FK evita invertir bloqueos entre altas simultáneas.
        var claimed = await db.Publicaciones.Where(p => p.Id == id && p.Version == post.Version)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Version, Guid.NewGuid().ToString("N")), ct);
        if (claimed != 1) throw Conflicto();
        db.Reportes.Add(report);
        await GuardarAsync(ct);
        await transaction.CommitAsync(ct);
        return report.Id;
    }

    public Task<Pagina<ReporteFila>> ListaAsync(string actor, bool admin, EstadoReporte? estado, string? q, int pagina, CancellationToken ct)
    {
        var readable = admin ? Moderables() : db.Publicaciones.LegiblesPor(actor);
        var ids = readable.Select(p => p.Id);
        var query = db.Reportes.AsNoTracking().Where(r => admin || r.DenuncianteId == actor);
        if (estado.HasValue) query = query.Where(r => r.Estado == estado.Value);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(r => r.Motivo.Contains(q.Trim()) ||
            (ids.Contains(r.PublicacionId) && r.Publicacion.Titulo.Contains(q.Trim())));
        return query.OrderByDescending(r => r.CreadoUtc).ThenByDescending(r => r.Id)
            .Select(r => new ReporteFila(r.Id, r.PublicacionId, ids.Contains(r.PublicacionId) ? r.Publicacion.Titulo : null,
                r.Motivo, r.Estado, r.CreadoUtc)).PaginarAsync(pagina, ct: ct);
    }

    public async Task<ReporteDetalle> DetalleAsync(string actor, bool admin, long id, CancellationToken ct)
    {
        var report = await db.Reportes.AsNoTracking().Where(r => r.Id == id && (admin || r.DenuncianteId == actor))
            .Select(r => new { r.Id, r.PublicacionId, r.Motivo, r.Detalle, r.Estado, r.CreadoUtc, r.Resolucion, r.ResueltoUtc, r.Publicacion.Version })
            .SingleOrDefaultAsync(ct) ?? throw NoDisponible();
        var readable = admin ? Moderables() : db.Publicaciones.LegiblesPor(actor);
        var post = await readable.AsNoTracking().Where(p => p.Id == report.PublicacionId)
            .Select(p => new { p.Titulo, p.Texto, p.EstadoModeracion }).SingleOrDefaultAsync(ct);
        var resources = post is null ? [] : await db.Recursos.AsNoTracking().Where(r => r.PublicacionId == report.PublicacionId && r.EstadoProcesamiento == EstadoProcesamiento.Listo)
            .OrderBy(r => r.Orden).Select(r => new RecursoVista(r.Id, r.Tipo, r.Orden, r.TextoAlternativo, r.Creditos, r.YouTubeId)).ToListAsync(ct);
        return new(report.Id, report.PublicacionId, report.Motivo, report.Detalle, report.Estado, report.CreadoUtc,
            report.Resolucion, report.ResueltoUtc, report.Version, post?.Titulo, post?.Texto, post?.EstadoModeracion, resources);
    }

    public async Task ResolverAsync(string admin, long id, ResolucionEntrada entrada, CancellationToken ct)
    {
        var report = await db.Reportes.Include(r => r.Publicacion).SingleOrDefaultAsync(r => r.Id == id, ct) ?? throw NoDisponible();
        var post = report.Publicacion;
        if (report.Estado != EstadoReporte.Pendiente || post.Version != entrada.Version) throw Conflicto();
        if (entrada.Resolucion.Trim().Length < 10) throw new PublicacionException(400, "Explicá la resolución en al menos 10 caracteres.");
        if (entrada.Decision != DecisionReporte.Desestimar)
        {
            if (!await Moderables().AnyAsync(p => p.Id == post.Id, ct)) throw NoDisponible();
            if ((entrada.Decision == DecisionReporte.Ocultar && (report.Motivo == Revision || post.EstadoModeracion != EstadoModeracion.Visible)) ||
                (entrada.Decision == DecisionReporte.Restaurar && (report.Motivo != Revision || post.EstadoModeracion != EstadoModeracion.Oculta)))
                throw new PublicacionException(409, "La decisión no corresponde al estado actual. Volvé a revisar el reporte.");
            post.EstadoModeracion = entrada.Decision == DecisionReporte.Ocultar ? EstadoModeracion.Oculta : EstadoModeracion.Visible;
        }
        report.Estado = entrada.Decision == DecisionReporte.Desestimar ? EstadoReporte.Desestimado : EstadoReporte.Resuelto;
        report.Resolucion = entrada.Resolucion.Trim();
        report.ResueltoPorId = admin;
        report.ResueltoUtc = DateTime.UtcNow;
        post.Version = Guid.NewGuid().ToString("N");
        post.ActualizadaUtc = DateTime.UtcNow;
        await GuardarAsync(ct);
    }

    private async Task GuardarAsync(CancellationToken ct)
    {
        // SaveChanges usa una transacción: la resolución y la moderación se confirman juntas.
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw Conflicto(); }
    }
    private static PublicacionException Conflicto() => new(409, "El contenido o el reporte cambió. Volvé a cargarlo antes de continuar.");
    private static PublicacionException NoDisponible() => new(404, "El contenido no está disponible.");
}
