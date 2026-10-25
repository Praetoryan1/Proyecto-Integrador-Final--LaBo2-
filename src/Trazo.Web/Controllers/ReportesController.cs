using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Trazo.Web.Domain;
using Trazo.Web.Models;
using Trazo.Web.Services;

namespace Trazo.Web.Controllers;

[Authorize(Roles = Roles.Creador), RequestSizeLimit(20_000), TypeFilter(typeof(ReporteExceptionFilter))]
public class ReportesController(ReportesService service) : Controller
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("/publicaciones/{id:long}/reportar")]
    public async Task<IActionResult> Crear(long id, CancellationToken ct) => await Formulario(id, new(), ct);

    [HttpPost("/publicaciones/{id:long}/reportar"), EnableRateLimiting("reportes")]
    public async Task<IActionResult> Crear(long id, ReporteEntrada entrada, CancellationToken ct)
    {
        if (!ModelState.IsValid) return await Formulario(id, entrada, ct);
        var reportId = await service.CrearAsync(Actor, id, entrada, ct);
        TempData["Exito"] = "Solicitud registrada. Podés consultar su estado en Mis reportes.";
        return RedirectToAction(nameof(Detalle), new { id = reportId });
    }

    private async Task<IActionResult> Formulario(long id, ReporteEntrada entrada, CancellationToken ct)
    {
        var post = await service.Reportables(Actor).Where(p => p.Id == id).Select(p => new { p.Titulo, Propia = p.AutorId == Actor }).SingleOrDefaultAsync(ct);
        if (post is null) return NotFound();
        ViewData["PublicacionId"] = id; ViewData["TituloObra"] = post.Titulo; ViewData["Revision"] = post.Propia;
        if (post.Propia) entrada.Motivo = ReportesService.Revision;
        return View("Crear", entrada);
    }

    [HttpGet("/mis-reportes")]
    public async Task<IActionResult> Index(EstadoReporte? estado, string? q, int pagina = 1, CancellationToken ct = default)
    {
        if (!ModelState.IsValid || q?.Length > 100 || (estado.HasValue && !Enum.IsDefined(estado.Value))) return BadRequest();
        return View("~/Views/Reportes/Index.cshtml", new ReportesLista(await service.ListaAsync(Actor, false, estado, q, pagina, ct), estado, q, false));
    }

    [HttpGet("/mis-reportes/{id:long}")]
    public async Task<IActionResult> Detalle(long id, CancellationToken ct) => View(await service.DetalleAsync(Actor, false, id, ct));
}
