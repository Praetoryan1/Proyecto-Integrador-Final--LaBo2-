using System.ComponentModel.DataAnnotations;
using Trazo.Web.Domain;

namespace Trazo.Web.Models;

public record CuentaAdministrada(string Id, string Usuario, EstadoCuenta Estado, DateTime FechaAlta, bool Administradora);
public record CuentasAdminLista(Pagina<CuentaAdministrada> Resultados, string? Q, EstadoCuenta? Estado);
public record CambioCuentaVista(long Id, string Administrador, EstadoCuenta Anterior, EstadoCuenta Nuevo, string Motivo, DateTime Fecha);
public record CuentaAdminDetalle(CuentaAdministrada Cuenta, string Version, bool Protegida, Pagina<CambioCuentaVista> Historial);

public class CambiarEstadoCuentaEntrada
{
    [Required, StringLength(100)]
    public string Version { get; set; } = "";
    [EnumDataType(typeof(EstadoCuenta))]
    public EstadoCuenta EstadoNuevo { get; set; }
    [Required, StringLength(1000, MinimumLength = 10)]
    public string Motivo { get; set; } = "";
}
