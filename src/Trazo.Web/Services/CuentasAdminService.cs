using Microsoft.EntityFrameworkCore;
using Trazo.Web.Data;
using Trazo.Web.Domain;
using Trazo.Web.Models;

namespace Trazo.Web.Services;

public class CuentasAdminService(TrazoDbContext db)
{
    private IQueryable<string> Administradores() => db.UserRoles.Where(ur => db.Roles
        .Where(r => r.NormalizedName == "ADMINISTRADOR").Select(r => r.Id).Contains(ur.RoleId)).Select(ur => ur.UserId);

    private IQueryable<CuentaAdministrada> Proyectar(IQueryable<Usuario> query)
    {
        var admins = Administradores();
        // Metadatos mínimos para administrar estados; no expone emails de acceso ni perfiles privados.
        return query.AsNoTracking().Select(u => new CuentaAdministrada(u.Id, u.UserName!, u.Estado, u.FechaAlta, admins.Contains(u.Id)));
    }

    public Task<Pagina<CuentaAdministrada>> ListarAsync(string? q, EstadoCuenta? estado, int pagina, CancellationToken ct)
    {
        var query = db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(u => u.UserName!.Contains(q.Trim()));
        if (estado.HasValue) query = query.Where(u => u.Estado == estado.Value);
        return Proyectar(query.OrderBy(u => u.UserName).ThenBy(u => u.Id)).PaginarAsync(pagina, ct: ct);
    }

    public async Task<CuentaAdminDetalle> DetalleAsync(string actor, string id, int pagina, CancellationToken ct)
    {
        var cuenta = await Proyectar(db.Users.Where(u => u.Id == id)).SingleOrDefaultAsync(ct) ?? throw new PublicacionException(404, "La cuenta no está disponible.");
        var version = await db.Users.Where(u => u.Id == id).Select(u => u.ConcurrencyStamp).SingleAsync(ct);
        var historial = await db.CambiosEstadoCuenta.AsNoTracking().Where(c => c.UsuarioId == id)
            .OrderByDescending(c => c.CreadoUtc).ThenByDescending(c => c.Id)
            .Select(c => new CambioCuentaVista(c.Id, c.Administrador.UserName!, c.EstadoAnterior, c.EstadoNuevo, c.Motivo, c.CreadoUtc))
            .PaginarAsync(pagina, ct: ct);
        return new(cuenta, version!, actor == id || cuenta.Administradora, historial);
    }

    public async Task CambiarAsync(string actor, string id, CambiarEstadoCuentaEntrada entrada, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id, ct) ?? throw new PublicacionException(404, "La cuenta no está disponible.");
        if (actor == id || await Administradores().AnyAsync(a => a == id, ct))
            throw new PublicacionException(400, "La cuenta propia y las cuentas administradoras están protegidas en este panel.");
        if (!Enum.IsDefined(entrada.EstadoNuevo) || entrada.Motivo.Trim().Length < 10)
            throw new PublicacionException(400, "Elegí un estado válido y explicá el motivo en al menos 10 caracteres.");
        if (user.ConcurrencyStamp != entrada.Version || user.Estado == entrada.EstadoNuevo)
            throw new PublicacionException(409, "La cuenta cambió o ya tiene ese estado. Volvé a revisarla antes de confirmar.");
        db.CambiosEstadoCuenta.Add(new CambioEstadoCuenta {
            UsuarioId = id, AdministradorId = actor, EstadoAnterior = user.Estado,
            EstadoNuevo = entrada.EstadoNuevo, Motivo = entrada.Motivo.Trim()
        });
        user.Estado = entrada.EstadoNuevo;
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        // Identity ya configura ConcurrencyStamp como token. Estado, sellos e historial son atómicos.
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new PublicacionException(409, "Otra operación cambió la cuenta. Volvé a cargarla antes de continuar."); }
    }
}
