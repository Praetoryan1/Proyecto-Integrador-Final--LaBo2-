using Microsoft.EntityFrameworkCore;

namespace Trazo.Web.Models;

public record Pagina<T>(IReadOnlyList<T> Items, int Numero, int Tamano, int Total)
{
    public int Paginas => Math.Max(1, (int)Math.Ceiling((double)Total / Tamano));
    public bool Anterior => Numero > 1;
    public bool Siguiente => Numero < Paginas;
}

public static class Paginacion
{
    public static async Task<Pagina<T>> PaginarAsync<T>(this IQueryable<T> query, int pagina, int tamano = 12, CancellationToken ct = default)
    {
        tamano = Math.Clamp(tamano, 1, 50);
        var total = await query.CountAsync(ct);
        pagina = Math.Clamp(pagina, 1, Math.Max(1, (int)Math.Ceiling((double)total / tamano)));
        var items = await query.Skip((pagina - 1) * tamano).Take(tamano).ToListAsync(ct);
        return new(items, pagina, tamano, total);
    }
}
