using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trazo.Web.Data;
using Trazo.Web.Domain;
using Trazo.Web.Models;
using Trazo.Web.Services;
namespace Trazo.Web.Controllers;

public class ExplorarController(TrazoDbContext db) : Controller
{
    [HttpGet("/"), HttpGet("/explorar")]
    public async Task<IActionResult> Index(string? q, string? etiqueta, TipoPublicacion? tipo, int pagina = 1, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return BadRequest();
        if ((q?.Length ?? 0) > 100 || (etiqueta?.Length ?? 0) > 40 || (tipo.HasValue && !Enum.IsDefined(tipo.Value))) return BadRequest();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var query = db.Publicaciones.AsNoTracking().LegiblesPor(userId)
            .Where(p => p.EstadoEditorial == EstadoEditorial.Publicada && p.EstadoModeracion == EstadoModeracion.Visible);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(p => p.Titulo.Contains(q.Trim()) || p.Texto.Contains(q.Trim()));
        if (!string.IsNullOrWhiteSpace(etiqueta)) query = query.Where(p => p.Etiquetas.Any(e => e.Etiqueta.Activa && e.Etiqueta.NombreNormalizado == etiqueta.Trim().ToUpper()));
        if (tipo.HasValue) query = query.Where(p => p.Tipo == tipo.Value);
        var results = await query.OrderByDescending(p => p.PublicadaUtc).ThenByDescending(p => p.Id).Tarjetas().PaginarAsync(pagina, ct: ct);
        return View(new ExplorarModel(results, q, etiqueta, tipo));
    }

    [HttpGet("/publicaciones/{id:long}")]
    public async Task<IActionResult> Detalle(long id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var query = db.Publicaciones.AsNoTracking().LegiblesPor(userId).Where(p => p.Id == id);
        var state = await query.Select(p => new { p.Visibilidad, p.EstadoEditorial, p.AutorId }).SingleOrDefaultAsync(ct);
        if (state is null) return NotFound();
        var card = await query.Tarjetas().SingleAsync(ct);
        return View(new DetallePublicacionModel(card, state.Visibilidad, state.EstadoEditorial, state.AutorId == userId));
    }
}
