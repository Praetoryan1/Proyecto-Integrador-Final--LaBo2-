using System.ComponentModel.DataAnnotations;
using Trazo.Web.Domain;

namespace Trazo.Web.Models;

public class GuardarPublicacionModel
{
    [Required(ErrorMessage = "Escribí un título."), StringLength(160, ErrorMessage = "El título admite hasta 160 caracteres.")]
    public string Titulo { get; set; } = "";
    [Required(ErrorMessage = "Escribí el texto de tu publicación."), StringLength(12000, ErrorMessage = "El texto admite hasta 12000 caracteres.")]
    public string Texto { get; set; } = "";
    [EnumDataType(typeof(TipoPublicacion), ErrorMessage = "Elegí un tipo válido.")]
    public TipoPublicacion Tipo { get; set; } = TipoPublicacion.Actualizacion;
    [EnumDataType(typeof(EstadoEditorial), ErrorMessage = "Elegí un estado válido.")]
    public EstadoEditorial EstadoEditorial { get; set; } = EstadoEditorial.Borrador;
    [EnumDataType(typeof(Visibilidad), ErrorMessage = "Elegí una visibilidad válida.")]
    public Visibilidad Visibilidad { get; set; } = Visibilidad.Publica;
    [Required, MaxLength(8, ErrorMessage = "Podés seleccionar hasta 8 etiquetas.")]
    public long[] EtiquetaIds { get; set; } = [];
}

public class EditarPublicacionModel : GuardarPublicacionModel
{
    [Required, RegularExpression("^[a-f0-9]{32}$", ErrorMessage = "La versión no es válida. Volvé a abrir la publicación.")]
    public string Version { get; set; } = "";
}

public class EliminarPublicacionModel
{
    [Required, RegularExpression("^[a-f0-9]{32}$")]
    public string Version { get; set; } = "";
}

public record EtiquetaEditor(long Id, string Nombre, bool Activa);
public record PublicacionResumen(long Id, string Titulo, TipoPublicacion Tipo, EstadoEditorial EstadoEditorial,
    Visibilidad Visibilidad, EstadoModeracion EstadoModeracion, DateTime ActualizadaUtc, string Version);
public record PublicacionEditor(long Id, string Titulo, string Texto, TipoPublicacion Tipo, EstadoEditorial EstadoEditorial,
    Visibilidad Visibilidad, EstadoModeracion EstadoModeracion, string Version, IReadOnlyList<EtiquetaEditor> Etiquetas);
