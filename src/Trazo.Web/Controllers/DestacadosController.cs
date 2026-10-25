using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trazo.Web.Domain;
using Trazo.Web.Models;
using Trazo.Web.Services;

namespace Trazo.Web.Controllers;

[Authorize(Roles = Roles.Creador), RequestSizeLimit(10_000)]
public class DestacadosController(DestacadosService service) : Controller
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    [HttpGet("/mi-portafolio/destacados")]
    public async Task<IActionResult> Index(string? q, int pagina = 1, CancellationToken ct = default)
    {
        if (!ModelState.IsValid || q?.Length > 100) return BadRequest();
        return View(await service.PanelAsync(Actor, q, pagina, ct));
    }

    [HttpPost("/mi-portafolio/destacados/{id:long}")]
    public async Task<IActionResult> Guardar(long id, DestacadoEntrada entrada, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest();
        try { await service.GuardarAsync(Actor, id, entrada, ct); }
        catch (PublicacionException e) {
            Response.StatusCode = e.Status;
            return View("Error", e.Message);
        }
        TempData["Exito"] = "Selección actualizada. Destacar una obra no cambia sus permisos.";
        return RedirectToAction(nameof(Index));
    }
}
