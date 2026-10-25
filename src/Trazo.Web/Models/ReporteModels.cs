using System.ComponentModel.DataAnnotations;
using Trazo.Web.Domain;

namespace Trazo.Web.Models;

public class ReporteEntrada
{
    [Required, StringLength(80), Display(Name = "Motivo")]
    public string Motivo { get; set; } = "";
    [Required, StringLength(2000, MinimumLength = 10), Display(Name = "Detalle")]
    public string Detalle { get; set; } = "";
}

public enum DecisionReporte { Desestimar, Ocultar, Restaurar }
public class ResolucionEntrada
{
    [EnumDataType(typeof(DecisionReporte))]
    public DecisionReporte Decision { get; set; }
    [Required, StringLength(2000, MinimumLength = 10), Display(Name = "Resolución")]
    public string Resolucion { get; set; } = "";
    [Required, RegularExpression("^[a-f0-9]{32}$")]
    public string Version { get; set; } = "";
}

public record ReporteFila(long Id, long PublicacionId, string? Titulo, string Motivo, EstadoReporte Estado, DateTime CreadoUtc);
public record ReportesLista(Pagina<ReporteFila> Resultados, EstadoReporte? Estado, string? Q, bool Administracion);
public record ReporteDetalle(long Id, long PublicacionId, string Motivo, string Detalle, EstadoReporte Estado,
    DateTime CreadoUtc, string? Resolucion, DateTime? ResueltoUtc, string Version, string? Titulo,
    string? Texto, EstadoModeracion? Moderacion, IReadOnlyList<RecursoVista> Recursos);
