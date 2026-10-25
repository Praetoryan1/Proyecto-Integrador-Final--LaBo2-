using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trazo.Web.Domain;
using Trazo.Web.Models;
using Trazo.Web.Services;

namespace Trazo.Web.Controllers;

[Authorize(Roles = Roles.Creador), RequestSizeLimit(10_000), TypeFilter(typeof(PublicacionExceptionFilter))]
public class ComunidadController(ComunidadService service) : Controller
{
    private string UsuarioId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("/mis-favoritos")]
    public async Task<IActionResult> Favoritos(string? q, int pagina = 1, CancellationToken ct = default)
    {
        if (!ModelState.IsValid || q?.Length > 100) return BadRequest();
        return View(new FavoritosModel(await service.FavoritosAsync(UsuarioId, q, pagina, ct), q));
    }

    [HttpGet("/mis-seguidos")]
    public async Task<IActionResult> Seguidos(string? q, int pagina = 1, CancellationToken ct = default)
    {
        if (!ModelState.IsValid || q?.Length > 100) return BadRequest();
        return View(new SeguidosModel(await service.SeguidosAsync(UsuarioId, q, pagina, ct), q));
    }

    [HttpPost("/favoritos/{id:long}/guardar")]
    public async Task<IActionResult> Guardar(long id, string? returnUrl, CancellationToken ct)
    {
        await service.GuardarAsync(UsuarioId, id, ct);
        return Volver(returnUrl, "/mis-favoritos", "Publicación guardada en tus favoritos privados.");
    }

    [HttpPost("/favoritos/{id:long}/quitar")]
    public async Task<IActionResult> Quitar(long id, string? returnUrl, CancellationToken ct)
    {
        await service.QuitarAsync(UsuarioId, id, ct);
        return Volver(returnUrl, "/mis-favoritos", "Publicación quitada de tus favoritos.");
    }

    [HttpPost("/seguimientos/{usuario}/seguir")]
    public async Task<IActionResult> Seguir(string usuario, string? returnUrl, CancellationToken ct)
    {
        if (usuario.Length > 30) return BadRequest();
        await service.SeguirAsync(UsuarioId, usuario, ct);
        return Volver(returnUrl, "/mis-seguidos", "Creador agregado a tu lista de seguidos.");
    }

    [HttpPost("/seguimientos/{usuario}/quitar")]
    public async Task<IActionResult> DejarDeSeguir(string usuario, string? returnUrl, CancellationToken ct)
    {
        if (usuario.Length > 30) return BadRequest();
        await service.DejarDeSeguirAsync(UsuarioId, usuario, ct);
        return Volver(returnUrl, "/mis-seguidos", "Creador quitado de tu lista de seguidos.");
    }

    [HttpPost("/mis-favoritos/limpiar")]
    public async Task<IActionResult> LimpiarFavoritos(CancellationToken ct)
    {
        await service.LimpiarFavoritosAsync(UsuarioId, ct);
        return Volver(null, "/mis-favoritos", "Se quitaron los favoritos que ya no estaban disponibles.");
    }

    [HttpPost("/mis-seguidos/limpiar")]
    public async Task<IActionResult> LimpiarSeguidos(CancellationToken ct)
    {
        await service.LimpiarSeguidosAsync(UsuarioId, ct);
        return Volver(null, "/mis-seguidos", "Se quitaron los seguimientos que ya no estaban disponibles.");
    }

    private IActionResult Volver(string? returnUrl, string fallback, string message)
    {
        TempData["Exito"] = message;
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : fallback);
    }
}
