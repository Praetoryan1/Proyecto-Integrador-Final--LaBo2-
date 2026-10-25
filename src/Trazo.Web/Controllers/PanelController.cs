using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trazo.Web.Data;
using Trazo.Web.Models;
using Trazo.Web.Services;
namespace Trazo.Web.Controllers;

[Authorize]
public class PanelController(TrazoDbContext db, ComunidadService comunidad) : Controller
{
    [HttpGet("/panel")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var p = await db.Perfiles.Where(p => p.UsuarioId == id).Select(p => new { p.NombreVisible, p.Usuario.UserName }).SingleAsync(ct);
        return View(new PanelModel(p.NombreVisible, p.UserName!, await db.Publicaciones.CountAsync(p => p.AutorId == id, ct),
            await comunidad.FavoritosVisibles(id).CountAsync(ct), await comunidad.SeguidosVisibles(id).CountAsync(ct)));
    }
}
