using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trazo.Web.Domain;
using Trazo.Web.Models;
using Trazo.Web.Services;

namespace Trazo.Web.Controllers;

[Authorize(Roles = Roles.Administrador), RequestSizeLimit(12_000)]
public class CuentasAdminController(CuentasAdminService service) : Controller
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("/administracion/cuentas")]
    public async Task<IActionResult> Index(string? q, EstadoCuenta? estado, int pagina = 1, CancellationToken ct = default)
    {
        if (!ModelState.IsValid || q?.Length > 100 || (estado.HasValue && !Enum.IsDefined(estado.Value))) return BadRequest();
        return View(new CuentasAdminLista(await service.ListarAsync(q, estado, pagina, ct), q, estado));
    }

    [HttpGet("/administracion/cuentas/{id}")]
    public async Task<IActionResult> Detalle(string id, int pagina = 1, CancellationToken ct = default)
    {
        if (!ModelState.IsValid || id.Length > 191) return BadRequest();
        try { return View(await service.DetalleAsync(Actor, id, pagina, ct)); }
        catch (PublicacionException e) when (e.Status == 404) { return NotFound(); }
    }

    [HttpPost("/administracion/cuentas/{id}/estado")]
    public async Task<IActionResult> CambiarEstado(string id, CambiarEstadoCuentaEntrada entrada, CancellationToken ct)
    {
        if (id.Length > 191) return BadRequest();
        try
        {
            if (ModelState.IsValid)
            {
                await service.CambiarAsync(Actor, id, entrada, ct);
                TempData["Exito"] = "Estado actualizado y decisión registrada. La cuenta deberá iniciar sesión nuevamente al estar activa.";
                return RedirectToAction(nameof(Detalle), new { id });
            }
            Response.StatusCode = 400;
        }
        catch (PublicacionException e)
        {
            if (e.Status == 404) return NotFound();
            Response.StatusCode = e.Status;
            ModelState.AddModelError("", e.Message);
        }
        ViewData["MotivoIngresado"] = entrada.Motivo;
        // Un conflicto requiere GET explícito; no se reenvía una decisión con una versión nueva en silencio.
        ViewData["Recargar"] = Response.StatusCode == 409;
        try { return View("Detalle", await service.DetalleAsync(Actor, id, 1, ct)); }
        catch (PublicacionException e) when (e.Status == 404) { return NotFound(); }
    }
}
