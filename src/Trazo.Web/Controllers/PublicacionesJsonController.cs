using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Trazo.Web.Domain;
using Trazo.Web.Models;
using Trazo.Web.Services;

namespace Trazo.Web.Controllers;

public class PublicacionExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not PublicacionException error) return;
        context.Result = new ObjectResult(new ProblemDetails { Status = error.Status, Title = "No se pudo completar la operación", Detail = error.Message }) { StatusCode = error.Status };
        context.ExceptionHandled = true;
    }
}

// API del panel del mismo origen: cookies + antiforgery. JWT usa /api/v1/.
[ApiController, Authorize(Roles = Roles.Creador), Route("/panel-json/publicaciones")]
[TypeFilter(typeof(PublicacionExceptionFilter)), RequestSizeLimit(100_000)]
public class PublicacionesJsonController(PublicacionesService service) : ControllerBase
{
    private string AutorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public Task<Pagina<PublicacionResumen>> Listar([FromQuery, StringLength(100)] string? q,
        [FromQuery, EnumDataType(typeof(EstadoEditorial))] EstadoEditorial? estado, int pagina = 1, CancellationToken ct = default)
        => service.ListarAsync(AutorId, q, estado, pagina, ct);

    [HttpGet("{id:long}")]
    public Task<PublicacionEditor> Obtener(long id, CancellationToken ct) => service.ObtenerAsync(AutorId, id, ct);

    [HttpPost]
    public async Task<ActionResult<PublicacionEditor>> Crear(GuardarPublicacionModel model, CancellationToken ct)
    {
        var result = await service.CrearAsync(AutorId, model, ct);
        return CreatedAtAction(nameof(Obtener), new { id = result.Id }, result);
    }

    [HttpPut("{id:long}")]
    public Task<PublicacionEditor> Editar(long id, EditarPublicacionModel model, CancellationToken ct)
        => service.EditarAsync(AutorId, id, model, ct);

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Eliminar(long id, [FromBody] EliminarPublicacionModel model, CancellationToken ct)
    {
        await service.EliminarAsync(AutorId, id, model.Version, ct);
        return NoContent();
    }
}

[ApiController, Authorize(Roles = Roles.Creador), Route("/panel-json/etiquetas")]
public class EtiquetasJsonController(EtiquetasConsulta service) : ControllerBase
{
    [HttpGet]
    public Task<Pagina<EtiquetaEditor>> Buscar([FromQuery, StringLength(40)] string? q, int pagina = 1, CancellationToken ct = default)
        => service.BuscarAsync(q, pagina, ct);
}
