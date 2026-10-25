using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trazo.Web.Data;
using Trazo.Web.Domain;
using Trazo.Web.Models;
using Trazo.Web.Services;

namespace Trazo.Web.Controllers;

[ApiController, Authorize(Roles = Roles.Creador), Route("/panel-json/publicaciones/{id:long}/recursos")]
[TypeFilter(typeof(PublicacionExceptionFilter)), RequestSizeLimit(100_000)]
public class RecursosController(RecursosService service) : ControllerBase
{
    private string AutorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost("archivo"), RequestSizeLimit(52_500_000), RequestFormLimits(MultipartBodyLengthLimit = 52_500_000)]
    public Task<PublicacionEditor> Subir(long id, [FromForm] ArchivoEntrada model, CancellationToken ct)
        => service.SubirAsync(AutorId, id, model, ct);

    [HttpPost("youtube")]
    public Task<PublicacionEditor> YouTube(long id, YouTubeEntrada model, CancellationToken ct)
        => service.YouTubeAsync(AutorId, id, model, ct);

    [HttpPut("{recursoId:long}")]
    public Task<PublicacionEditor> Editar(long id, long recursoId, RecursoEntrada model, CancellationToken ct)
        => service.EditarAsync(AutorId, id, recursoId, model, ct);

    [HttpDelete("{recursoId:long}")]
    public Task<PublicacionEditor> Eliminar(long id, long recursoId, [FromBody] EliminarPublicacionModel model, CancellationToken ct)
        => service.EliminarAsync(AutorId, id, recursoId, model.Version, ct);

    [HttpPut("orden")]
    public Task<PublicacionEditor> Ordenar(long id, OrdenRecursosEntrada model, CancellationToken ct)
        => service.OrdenarAsync(AutorId, id, model, ct);
}

public class MediosController(TrazoDbContext db, AlmacenMedios almacen) : Controller
{
    [HttpGet("/medios/{id:long}/archivo"), HttpHead("/medios/{id:long}/archivo")]
    public Task<IActionResult> Archivo(long id, CancellationToken ct) => Servir(id, false, ct);

    [HttpGet("/medios/{id:long}/vista-previa"), HttpHead("/medios/{id:long}/vista-previa")]
    public Task<IActionResult> VistaPrevia(long id, CancellationToken ct) => Servir(id, true, ct);

    private async Task<IActionResult> Servir(long id, bool preview, CancellationToken ct)
    {
        var viewer = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var readable = db.Publicaciones.LegiblesPor(viewer).Select(p => p.Id);
        var resource = await db.Recursos.AsNoTracking().Where(r => r.Id == id && readable.Contains(r.PublicacionId) && r.EstadoProcesamiento == EstadoProcesamiento.Listo)
            .Select(r => new { r.ClaveAlmacenamiento, r.Mime }).SingleOrDefaultAsync(ct);
        var path = almacen.Ruta(resource?.ClaveAlmacenamiento, preview);
        if (path is null || !System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path, preview ? "image/png" : resource!.Mime!, enableRangeProcessing: true);
    }
}
