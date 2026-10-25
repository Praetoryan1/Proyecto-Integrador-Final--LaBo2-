using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trazo.Web.Domain;
using Trazo.Web.Models;
using Trazo.Web.Services;

namespace Trazo.Web.Controllers.Api;

[ApiController, Route("api/v1/publicaciones"), IgnoreAntiforgeryToken]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Creador)]
[TypeFilter(typeof(PublicacionExceptionFilter)), RequestSizeLimit(100_000)]
public class PublicacionesApiController(PublicacionesService service) : ControllerBase
{
    private string AutorId => User.FindFirstValue("sub")!;

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

[ApiController, Route("api/v1/etiquetas"), IgnoreAntiforgeryToken]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Creador)]
public class EtiquetasApiController(EtiquetasConsulta service) : ControllerBase
{
    [HttpGet]
    public Task<Pagina<EtiquetaEditor>> Buscar([FromQuery, StringLength(40)] string? q, int pagina = 1, CancellationToken ct = default)
        => service.BuscarAsync(q, pagina, ct);
}
