using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Trazo.Web.Data;
using Trazo.Web.Domain;
using Trazo.Web.Models;

namespace Trazo.Web.Services;

public class ComunidadService(TrazoDbContext db)
{
    public IQueryable<Publicacion> PublicacionesDisponibles(string usuarioId) => db.Publicaciones.AsNoTracking()
        .LegiblesPor(usuarioId).Where(p => p.EstadoEditorial == EstadoEditorial.Publicada && p.EstadoModeracion == EstadoModeracion.Visible);

    public IQueryable<Favorito> FavoritosVisibles(string usuarioId)
    {
        var visibles = PublicacionesDisponibles(usuarioId).Select(p => p.Id);
        return db.Favoritos.AsNoTracking().Where(f => f.UsuarioId == usuarioId && visibles.Contains(f.PublicacionId));
    }

    public IQueryable<Seguimiento> SeguidosVisibles(string usuarioId) => db.Seguimientos.AsNoTracking()
        .Where(s => s.SeguidorId == usuarioId && db.Perfiles.LegiblesPor(usuarioId).Select(p => p.UsuarioId).Contains(s.SeguidoId));

    public Task<Pagina<TarjetaPublicacion>> FavoritosAsync(string usuarioId, string? q, int pagina, CancellationToken ct)
    {
        var query = FavoritosVisibles(usuarioId);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(f => f.Publicacion.Titulo.Contains(q.Trim()));
        return query.OrderByDescending(f => f.CreadoUtc).ThenByDescending(f => f.PublicacionId)
            .Select(f => f.Publicacion).Tarjetas().PaginarAsync(pagina, ct: ct);
    }

    public Task<Pagina<CreadorSeguido>> SeguidosAsync(string usuarioId, string? q, int pagina, CancellationToken ct)
    {
        var query = SeguidosVisibles(usuarioId);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(s => s.Seguido.Perfil.NombreVisible.Contains(q.Trim()) ||
            s.Seguido.UserName!.Contains(q.Trim()) || s.Seguido.Perfil.Especialidad.Contains(q.Trim()));
        return query.OrderByDescending(s => s.CreadoUtc).ThenBy(s => s.SeguidoId)
            .Select(s => new CreadorSeguido(s.Seguido.UserName!, s.Seguido.Perfil.NombreVisible,
                s.Seguido.Perfil.Especialidad, s.Seguido.Perfil.AvatarClave != null, s.Seguido.Perfil.DisponibleParaTrabajar))
            .PaginarAsync(pagina, ct: ct);
    }

    public async Task GuardarAsync(string usuarioId, long publicacionId, CancellationToken ct)
    {
        if (!await PublicacionesDisponibles(usuarioId).AnyAsync(p => p.Id == publicacionId, ct)) throw NoDisponible();
        if (await db.Favoritos.AnyAsync(f => f.UsuarioId == usuarioId && f.PublicacionId == publicacionId, ct)) return;
        var favorito = new Favorito { UsuarioId = usuarioId, PublicacionId = publicacionId };
        db.Favoritos.Add(favorito);
        await GuardarRelacionAsync(favorito, ct);
    }

    public Task QuitarAsync(string usuarioId, long publicacionId, CancellationToken ct) => db.Favoritos
        .Where(f => f.UsuarioId == usuarioId && f.PublicacionId == publicacionId).ExecuteDeleteAsync(ct);

    public async Task SeguirAsync(string usuarioId, string nombreUsuario, CancellationToken ct)
    {
        var id = await db.Perfiles.LegiblesPor(usuarioId).Where(p => p.Usuario.NormalizedUserName == nombreUsuario.ToUpper())
            .Select(p => p.UsuarioId).SingleOrDefaultAsync(ct) ?? throw NoDisponible();
        if (id == usuarioId) throw new PublicacionException(400, "No podés seguir tu propio perfil.");
        if (await db.Seguimientos.AnyAsync(s => s.SeguidorId == usuarioId && s.SeguidoId == id, ct)) return;
        var seguimiento = new Seguimiento { SeguidorId = usuarioId, SeguidoId = id };
        db.Seguimientos.Add(seguimiento);
        await GuardarRelacionAsync(seguimiento, ct);
    }

    // Quitar una relación propia está permitido aunque el destino ya no sea visible.
    public Task DejarDeSeguirAsync(string usuarioId, string nombreUsuario, CancellationToken ct)
    {
        // Subconsulta de IDs: el DELETE no necesita un JOIN sobre la tabla destino.
        var ids = db.Users.Where(u => u.NormalizedUserName == nombreUsuario.ToUpper()).Select(u => u.Id);
        return db.Seguimientos.Where(s => s.SeguidorId == usuarioId && ids.Contains(s.SeguidoId)).ExecuteDeleteAsync(ct);
    }

    public Task LimpiarFavoritosAsync(string usuarioId, CancellationToken ct)
    {
        var visibles = PublicacionesDisponibles(usuarioId).Select(p => p.Id);
        return db.Favoritos.Where(f => f.UsuarioId == usuarioId && !visibles.Contains(f.PublicacionId)).ExecuteDeleteAsync(ct);
    }

    public Task LimpiarSeguidosAsync(string usuarioId, CancellationToken ct) => db.Seguimientos
        .Where(s => s.SeguidorId == usuarioId && !db.Perfiles.LegiblesPor(usuarioId).Select(p => p.UsuarioId).Contains(s.SeguidoId)).ExecuteDeleteAsync(ct);

    private async Task GuardarRelacionAsync(object relacion, CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException e) when (e.InnerException is MySqlException { Number: 1062 })
        { db.Entry(relacion).State = EntityState.Detached; } // Dos solicitudes de alta dejan una sola relación.
        catch (DbUpdateException e) when (e.InnerException is MySqlException { Number: 1452 })
        { throw NoDisponible(); } // El destino se eliminó durante la operación.
    }

    private static PublicacionException NoDisponible() => new(404, "El contenido no está disponible.");
}
