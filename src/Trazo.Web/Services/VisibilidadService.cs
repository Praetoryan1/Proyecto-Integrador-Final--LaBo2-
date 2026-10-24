using Trazo.Web.Domain;

namespace Trazo.Web.Services;

// Estas expresiones permanecen en IQueryable: MySQL filtra antes de contar/paginar.
public static class VisibilidadService
{
    public static IQueryable<Publicacion> LegiblesPor(this IQueryable<Publicacion> query, string? usuarioId)
        => query.Where(p => p.Autor.Estado == EstadoCuenta.Activa &&
            (p.AutorId == usuarioId ||
             (p.EstadoEditorial == EstadoEditorial.Publicada && p.EstadoModeracion == EstadoModeracion.Visible &&
              (p.Visibilidad == Visibilidad.Publica || (usuarioId != null && p.Visibilidad == Visibilidad.Registrados)) &&
              (p.Autor.Perfil.Visibilidad == Visibilidad.Publica || (usuarioId != null && p.Autor.Perfil.Visibilidad == Visibilidad.Registrados)))));

    public static IQueryable<Perfil> LegiblesPor(this IQueryable<Perfil> query, string? usuarioId)
        => query.Where(p => p.Usuario.Estado == EstadoCuenta.Activa &&
            (p.UsuarioId == usuarioId || p.Visibilidad == Visibilidad.Publica ||
                (usuarioId != null && p.Visibilidad == Visibilidad.Registrados)));

    public static bool PuedeVerContacto(Perfil perfil, string? usuarioId) =>
        perfil.UsuarioId == usuarioId || perfil.VisibilidadContacto == VisibilidadContacto.Publica ||
        (usuarioId != null && perfil.VisibilidadContacto == VisibilidadContacto.Registrados);
}
