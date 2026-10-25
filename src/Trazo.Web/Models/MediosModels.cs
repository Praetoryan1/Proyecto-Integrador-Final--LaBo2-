using System.ComponentModel.DataAnnotations;
using Trazo.Web.Domain;

namespace Trazo.Web.Models;

public class RecursoEntrada
{
    [Required, RegularExpression("^[a-f0-9]{32}$")]
    public string Version { get; set; } = "";
    [Required(ErrorMessage = "Describí el contenido del recurso."), StringLength(500)]
    public string TextoAlternativo { get; set; } = "";
    [StringLength(300)]
    public string Creditos { get; set; } = "";
}
public class ArchivoEntrada : RecursoEntrada
{
    [Required(ErrorMessage = "Elegí un archivo.")]
    public IFormFile Archivo { get; set; } = null!;
}
public class YouTubeEntrada : RecursoEntrada
{
    [Required, StringLength(500)]
    public string Url { get; set; } = "";
}
public class OrdenRecursosEntrada : EliminarPublicacionModel
{
    [Required, MaxLength(10)]
    public long[] RecursoIds { get; set; } = [];
}
public record RecursoVista(long Id, TipoRecurso Tipo, int Orden, string TextoAlternativo, string Creditos, string? YouTubeId)
{
    public string? ArchivoUrl => Tipo == TipoRecurso.YouTube ? null : $"/medios/{Id}/archivo";
    public string? VistaPreviaUrl => Tipo == TipoRecurso.YouTube ? null : $"/medios/{Id}/vista-previa";
}
