using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trazo.Web.Data;
using Trazo.Web.Domain;
using Trazo.Web.Models;
using Trazo.Web.Services;

namespace Trazo.Web.Controllers;

[Authorize(Roles = Roles.Administrador), RequestSizeLimit(20_000), TypeFilter(typeof(ReporteExceptionFilter))]
public class ModeracionController(ReportesService service, TrazoDbContext db, AlmacenMedios almacen) : Controller
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("/administracion/reportes")]
    public async Task<IActionResult> Index(EstadoReporte? estado = EstadoReporte.Pendiente, string? q = null, int pagina = 1, CancellationToken ct = default)
    {
        if (!ModelState.IsValid || q?.Length > 100 || (estado.HasValue && !Enum.IsDefined(estado.Value))) return BadRequest();
        return View("~/Views/Reportes/Index.cshtml", new ReportesLista(await service.ListaAsync(Actor, true, estado, q, pagina, ct), estado, q, true));
    }

    [HttpGet("/administracion/reportes/{id:long}")]
    public async Task<IActionResult> Detalle(long id, CancellationToken ct) => View(await service.DetalleAsync(Actor, true, id, ct));

    [HttpPost("/administracion/reportes/{id:long}/resolver")]
    public async Task<IActionResult> Resolver(long id, ResolucionEntrada entrada, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            Response.StatusCode = 400;
            ViewData["ResolucionIngresada"] = entrada.Resolucion;
            return View("Detalle", await service.DetalleAsync(Actor, true, id, ct));
        }
        await service.ResolverAsync(Actor, id, entrada, ct);
        TempData["Exito"] = "Resolución registrada.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpGet("/administracion/reportes/{id:long}/medios/{recursoId:long}/{vista}"), HttpHead("/administracion/reportes/{id:long}/medios/{recursoId:long}/{vista}")]
    public async Task<IActionResult> Medio(long id, long recursoId, string vista, CancellationToken ct)
    {
        if (vista is not ("archivo" or "vista-previa")) return NotFound();
        var ids = service.Moderables().Select(p => p.Id);
        var reports = db.Reportes.Where(r => r.Id == id && ids.Contains(r.PublicacionId)).Select(r => r.PublicacionId);
        var resource = await db.Recursos.AsNoTracking().Where(r => r.Id == recursoId && reports.Contains(r.PublicacionId) && r.EstadoProcesamiento == EstadoProcesamiento.Listo)
            .Select(r => new { r.ClaveAlmacenamiento, r.Mime }).SingleOrDefaultAsync(ct);
        var preview = vista == "vista-previa";
        var path = almacen.Ruta(resource?.ClaveAlmacenamiento, preview);
        if (path is null || !System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path, preview ? "image/png" : resource!.Mime!, enableRangeProcessing: true);
    }
}
