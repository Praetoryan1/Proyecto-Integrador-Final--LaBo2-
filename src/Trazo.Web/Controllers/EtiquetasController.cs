using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trazo.Web.Data;
using Trazo.Web.Domain;
using Trazo.Web.Models;
namespace Trazo.Web.Controllers;

[Authorize(Roles = Roles.Administrador), Route("administracion/etiquetas")]
public class EtiquetasController(TrazoDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? q, int pagina = 1, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return BadRequest();
        if ((q?.Length ?? 0) > 40) return BadRequest();
        var query = db.Etiquetas.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(e => e.Nombre.Contains(q.Trim()));
        ViewData["Q"] = q;
        return View(await query.OrderBy(e => e.Nombre).ThenBy(e => e.Id).PaginarAsync(pagina, ct: ct));
    }
    [HttpGet("nueva")]
    public IActionResult Crear() => View("Formulario", new EtiquetaModel());
    [HttpGet("{id:long}/editar")]
    public async Task<IActionResult> Editar(long id)
    {
        var e = await db.Etiquetas.FindAsync(id);
        if (e is null) return NotFound();
        ViewData["Id"] = id;
        return View("Formulario", new EtiquetaModel { Nombre = e.Nombre, Activa = e.Activa });
    }
    [HttpPost("nueva")]
    public Task<IActionResult> Crear(EtiquetaModel model) => Guardar(null, model);
    [HttpPost("{id:long}/editar")]
    public Task<IActionResult> Editar(long id, EtiquetaModel model) => Guardar(id, model);

    private async Task<IActionResult> Guardar(long? id, EtiquetaModel model)
    {
        ViewData["Id"] = id;
        if (!ModelState.IsValid) return View("Formulario", model);
        var normalized = model.Nombre.Trim().ToUpperInvariant();
        if (await db.Etiquetas.AnyAsync(e => e.NombreNormalizado == normalized && e.Id != id))
        { ModelState.AddModelError(nameof(model.Nombre), "Ya existe una etiqueta con ese nombre."); return View("Formulario", model); }
        var entity = id.HasValue ? await db.Etiquetas.FindAsync(id.Value) : new Etiqueta();
        if (entity is null) return NotFound();
        entity.Nombre = model.Nombre.Trim(); entity.NombreNormalizado = normalized; entity.Activa = model.Activa;
        if (!id.HasValue) db.Etiquetas.Add(entity);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        { ModelState.AddModelError("", "No se pudo guardar; revisá si el nombre ya existe."); return View("Formulario", model); }
        TempData["Exito"] = "Etiqueta guardada.";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost("{id:long}/eliminar")]
    public async Task<IActionResult> Eliminar(long id)
    {
        var e = await db.Etiquetas.FindAsync(id);
        if (e is null) return NotFound();
        if (await db.PublicacionEtiquetas.AnyAsync(p => p.EtiquetaId == id))
        { TempData["Error"] = "La etiqueta está en uso. Podés desactivarla desde Editar."; return RedirectToAction(nameof(Index)); }
        db.Etiquetas.Remove(e);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { TempData["Error"] = "No se pudo eliminar la etiqueta; puede estar en uso."; }
        return RedirectToAction(nameof(Index));
    }
}
