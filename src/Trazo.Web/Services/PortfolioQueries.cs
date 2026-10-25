using Trazo.Web.Domain;
using Trazo.Web.Models;
namespace Trazo.Web.Services;

public static class PortfolioQueries
{
    public static IQueryable<TarjetaPublicacion> Tarjetas(this IQueryable<Publicacion> query) => query.Select(p =>
        new TarjetaPublicacion(p.Id, p.Titulo, p.Texto, p.Tipo, p.Autor.Perfil.NombreVisible,
            p.Autor.UserName!, p.PublicadaUtc, p.Etiquetas.Where(e => e.Etiqueta.Activa).Select(e => e.Etiqueta.Nombre).ToArray(),
            p.Recursos.Where(r => r.EstadoProcesamiento == EstadoProcesamiento.Listo && r.Tipo != TipoRecurso.YouTube)
                .OrderBy(r => r.Orden).Select(r => (long?)r.Id).FirstOrDefault()));
}
