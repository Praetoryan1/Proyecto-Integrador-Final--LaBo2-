using System.ComponentModel.DataAnnotations;
using Trazo.Web.Domain;

namespace Trazo.Web.Models;

public record ObraDestacable(long Id, string Titulo, string Version, int? Posicion, bool Disponible);
public record DestacadosModel(IReadOnlyList<ObraDestacable> Seleccion, string VersionSeleccion, Pagina<ObraDestacable> Obras, string? Q);
public class DestacadoEntrada
{
    [Required, RegularExpression("^[a-f0-9]{32}$")]
    public string Version { get; set; } = "";
    [Required, RegularExpression("^[A-F0-9]{64}$")]
    public string VersionSeleccion { get; set; } = "";
    // 0 retira de destacados; 1..6 asigna una posición editorial.
    [Range(0, 6)] public int Posicion { get; set; }
}
