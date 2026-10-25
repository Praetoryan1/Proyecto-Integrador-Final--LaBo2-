using Microsoft.EntityFrameworkCore;
using Trazo.Web.Data;
using Trazo.Web.Models;

namespace Trazo.Web.Services;

public class EtiquetasConsulta(TrazoDbContext db)
{
    public Task<Pagina<EtiquetaEditor>> BuscarAsync(string? q, int pagina, CancellationToken ct)
    {
        var query = db.Etiquetas.AsNoTracking().Where(e => e.Activa);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(e => e.Nombre.Contains(q.Trim()));
        return query.OrderBy(e => e.Nombre).ThenBy(e => e.Id).Select(e => new EtiquetaEditor(e.Id, e.Nombre, e.Activa))
            .PaginarAsync(pagina, tamano: 10, ct: ct);
    }
}
