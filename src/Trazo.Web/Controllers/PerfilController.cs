using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trazo.Web.Data;
using Trazo.Web.Domain;
using Trazo.Web.Models;
using Trazo.Web.Services;
namespace Trazo.Web.Controllers;

public class PerfilController(TrazoDbContext db, AvatarService avatars) : Controller
{
    public static readonly string[] Acentos = ["#c4f566", "#c4b5fd", "#7dd3fc", "#fda4af", "#fdba74"];
    private string UsuarioId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("/p/{usuario}")]
    public async Task<IActionResult> Ver(string usuario, int pagina = 1, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return BadRequest();
        var viewer = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var profile = await db.Perfiles.AsNoTracking().LegiblesPor(viewer).Include(p => p.Enlaces)
            .Include(p => p.Usuario).SingleOrDefaultAsync(p => p.Usuario.NormalizedUserName == usuario.ToUpper(), ct);
        if (profile is null) return NotFound();
        var contact = VisibilidadService.PuedeVerContacto(profile, viewer);
        var posts = await db.Publicaciones.AsNoTracking().LegiblesPor(viewer).Where(p => p.AutorId == profile.UsuarioId)
            .OrderBy(p => p.OrdenDestacado == null).ThenBy(p => p.OrdenDestacado)
            .ThenByDescending(p => p.PublicadaUtc).ThenByDescending(p => p.Id).Tarjetas().PaginarAsync(pagina, ct: ct);
        return View(new PerfilPublicoModel(profile.Usuario.UserName!, profile.NombreVisible, profile.Biografia, profile.Especialidad,
            profile.DisponibleParaTrabajar, profile.Tema, profile.Disposicion, profile.ColorAcento, profile.AvatarClave != null,
            contact ? profile.EmailContacto : null,
            contact ? profile.Enlaces.OrderBy(x => x.Orden).Select(e => new EnlaceModel { Etiqueta = e.Etiqueta, Url = e.Url }).ToList() : [],
            posts, viewer == profile.UsuarioId, viewer != null && await db.Seguimientos.AnyAsync(s => s.SeguidorId == viewer && s.SeguidoId == profile.UsuarioId, ct)));
    }

    [HttpGet("/mi-perfil"), Authorize]
    public async Task<IActionResult> Editar(CancellationToken ct)
    {
        var p = await db.Perfiles.AsNoTracking().Include(p => p.Enlaces).SingleAsync(p => p.UsuarioId == UsuarioId, ct);
        return View(new EditarPerfilModel
        {
            NombreVisible = p.NombreVisible,
            Biografia = p.Biografia,
            Especialidad = p.Especialidad,
            DisponibleParaTrabajar = p.DisponibleParaTrabajar,
            Visibilidad = p.Visibilidad,
            EmailContacto = p.EmailContacto,
            VisibilidadContacto = p.VisibilidadContacto,
            Tema = p.Tema,
            Disposicion = p.Disposicion,
            ColorAcento = p.ColorAcento,
            Enlaces = p.Enlaces.OrderBy(e => e.Orden).Select(e => new EnlaceModel { Etiqueta = e.Etiqueta, Url = e.Url }).ToList()
        });
    }

    [HttpPost("/mi-perfil"), Authorize]
    public async Task<IActionResult> Editar(EditarPerfilModel model, CancellationToken ct)
    {
        if (!Acentos.Contains(model.ColorAcento)) ModelState.AddModelError(nameof(model.ColorAcento), "Elegí uno de los colores disponibles.");
        if (string.IsNullOrWhiteSpace(model.NombreVisible)) ModelState.AddModelError(nameof(model.NombreVisible), "Ingresá un nombre visible.");
        for (var i = 0; i < model.Enlaces.Count; i++)
        {
            var link = model.Enlaces[i];
            if (string.IsNullOrWhiteSpace(link.Url) && string.IsNullOrWhiteSpace(link.Etiqueta)) continue;
            if (string.IsNullOrWhiteSpace(link.Etiqueta)) ModelState.AddModelError($"Enlaces[{i}].Etiqueta", "Escribí una etiqueta.");
            if (!Uri.TryCreate(link.Url, UriKind.Absolute, out var url) || url.Scheme != "https" || string.IsNullOrEmpty(url.Host) || !string.IsNullOrEmpty(url.UserInfo))
                ModelState.AddModelError($"Enlaces[{i}].Url", "Ingresá un enlace HTTPS válido, sin credenciales.");
        }
        if (!ModelState.IsValid) return View(model);
        var p = await db.Perfiles.Include(p => p.Enlaces).SingleAsync(p => p.UsuarioId == UsuarioId, ct);
        p.NombreVisible = model.NombreVisible.Trim(); p.Biografia = model.Biografia?.Trim() ?? "";
        p.Especialidad = model.Especialidad?.Trim() ?? ""; p.DisponibleParaTrabajar = model.DisponibleParaTrabajar;
        p.Visibilidad = model.Visibilidad; p.EmailContacto = model.EmailContacto?.Trim();
        p.VisibilidadContacto = model.VisibilidadContacto; p.Tema = model.Tema;
        p.ColorAcento = model.ColorAcento; p.Disposicion = model.Disposicion;
        db.EnlacesPerfil.RemoveRange(p.Enlaces);
        p.Enlaces = model.Enlaces.Where(e => !string.IsNullOrWhiteSpace(e.Url)).Select((e, i) =>
            new EnlacePerfil { Etiqueta = e.Etiqueta!.Trim(), Url = e.Url!.Trim(), Orden = i }).ToList();
        await db.SaveChangesAsync(ct);
        TempData["Exito"] = "Tu perfil quedó actualizado.";
        return RedirectToAction(nameof(Editar));
    }

    [HttpPost("/mi-perfil/avatar"), Authorize, RequestSizeLimit(2_200_000)]
    public async Task<IActionResult> SubirAvatar(IFormFile? avatar, CancellationToken ct)
    {
        if (avatar is null) { TempData["Error"] = "Elegí una imagen."; return RedirectToAction(nameof(Editar)); }
        string key;
        try { key = await avatars.GuardarAsync(avatar, ct); }
        catch (InvalidDataException e) { TempData["Error"] = e.Message; return RedirectToAction(nameof(Editar)); }
        var p = await db.Perfiles.SingleAsync(p => p.UsuarioId == UsuarioId, ct);
        var previous = p.AvatarClave;
        p.AvatarClave = key;
        try { await db.SaveChangesAsync(ct); }
        catch { avatars.Eliminar(key); throw; }
        avatars.Eliminar(previous);
        TempData["Exito"] = "Tu avatar quedó actualizado.";
        return RedirectToAction(nameof(Editar));
    }

    [HttpPost("/mi-perfil/avatar/eliminar"), Authorize]
    public async Task<IActionResult> EliminarAvatar(CancellationToken ct)
    {
        var p = await db.Perfiles.SingleAsync(p => p.UsuarioId == UsuarioId, ct);
        var previous = p.AvatarClave; p.AvatarClave = null;
        await db.SaveChangesAsync(ct); avatars.Eliminar(previous);
        return RedirectToAction(nameof(Editar));
    }

    [HttpGet("/p/{usuario}/avatar")]
    public async Task<IActionResult> Avatar(string usuario, CancellationToken ct)
    {
        var p = await db.Perfiles.AsNoTracking().LegiblesPor(User.FindFirstValue(ClaimTypes.NameIdentifier))
            .Where(p => p.Usuario.NormalizedUserName == usuario.ToUpper()).Select(p => new { p.AvatarClave }).SingleOrDefaultAsync(ct);
        var path = avatars.ObtenerRuta(p?.AvatarClave);
        return path is null ? NotFound() : PhysicalFile(path, "image/png");
    }
}
