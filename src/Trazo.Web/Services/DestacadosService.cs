using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Trazo.Web.Data;
using Trazo.Web.Domain;
using Trazo.Web.Models;

namespace Trazo.Web.Services;

public class DestacadosService(TrazoDbContext db)
{
    public const int Limite = 6;
    public static IQueryable<Publicacion> Elegibles(IQueryable<Publicacion> query) => query.Where(p =>
        p.Tipo == TipoPublicacion.Obra && p.EstadoEditorial == EstadoEditorial.Publicada &&
        p.EstadoModeracion == EstadoModeracion.Visible && p.Visibilidad != Visibilidad.Privada &&
        p.Recursos.Any(r => r.EstadoProcesamiento == EstadoProcesamiento.Listo));

    private static string Huella(IEnumerable<Publicacion> posts) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        string.Join("|", posts.OrderBy(p => p.Id).Select(p => $"{p.Id}:{p.OrdenDestacado}:{p.Version}")))));

    public async Task<DestacadosModel> PanelAsync(string actor, string? q, int pagina, CancellationToken ct)
    {
        var propios = db.Publicaciones.AsNoTracking().Where(p => p.AutorId == actor);
        var selection = await propios.Where(p => p.OrdenDestacado != null).OrderBy(p => p.OrdenDestacado).ThenBy(p => p.Id).Take(Limite).ToListAsync(ct);
        var eligibleIds = Elegibles(propios).Select(p => p.Id);
        var available = await propios.Where(p => p.OrdenDestacado != null && eligibleIds.Contains(p.Id)).Select(p => p.Id).Take(Limite).ToListAsync(ct);
        var query = Elegibles(propios);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(p => p.Titulo.Contains(q.Trim()));
        var page = await query.OrderByDescending(p => p.PublicadaUtc).ThenByDescending(p => p.Id)
            .Select(p => new ObraDestacable(p.Id, p.Titulo, p.Version, p.OrdenDestacado, true)).PaginarAsync(pagina, ct: ct);
        return new(selection.Select(p => new ObraDestacable(p.Id, p.Titulo, p.Version, p.OrdenDestacado, available.Contains(p.Id))).ToList(), Huella(selection), page, q);
    }

    public async Task GuardarAsync(string actor, long id, DestacadoEntrada entrada, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Mutex por portafolio en MySQL: las posiciones se modifican serialmente, incluso entre procesos.
        // SQL parametrizado, sin composición LINQ sobre FOR UPDATE. Solo puede devolver una fila por PK.
        var profiles = await db.Perfiles.FromSqlInterpolated($"SELECT * FROM `Perfiles` WHERE `UsuarioId` = {actor} FOR UPDATE")
            .AsNoTracking().ToListAsync(ct);
        if (profiles.Count != 1) throw new PublicacionException(404, "El perfil no está disponible.");
        var propios = db.Publicaciones.Where(p => p.AutorId == actor);
        var post = await propios.SingleOrDefaultAsync(p => p.Id == id, ct) ?? throw new PublicacionException(404, "La obra no está disponible.");
        var selected = await propios.Where(p => p.OrdenDestacado != null).OrderBy(p => p.OrdenDestacado).ThenBy(p => p.Id).Take(Limite + 1).ToListAsync(ct);
        if (selected.Count > Limite || post.Version != entrada.Version || Huella(selected) != entrada.VersionSeleccion)
            throw Conflicto();
        if (entrada.Posicion is < 0 or > Limite) throw new PublicacionException(400, "Elegí una posición entre 1 y 6, o quitá la obra de destacados.");
        if (entrada.Posicion > 0 && !await Elegibles(propios).AnyAsync(p => p.Id == id, ct))
            throw new PublicacionException(400, "Solo podés destacar obras publicadas, con recursos listos, visibles por moderación y públicas o para registrados.");
        var destination = entrada.Posicion == 0 ? (int?)null : entrada.Posicion;
        if (post.OrdenDestacado == destination) return;
        foreach (var previous in selected.Where(p => p.Id != id && p.OrdenDestacado == destination))
        {
            previous.OrdenDestacado = null;
            previous.Version = Guid.NewGuid().ToString("N");
        }
        post.OrdenDestacado = destination;
        post.Version = Guid.NewGuid().ToString("N");
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw Conflicto(); }
        await transaction.CommitAsync(ct);
    }

    private static PublicacionException Conflicto() => new(409, "La obra o la selección cambió en otra pestaña. Volvé a revisar antes de guardar.");
}
