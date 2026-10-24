using System.ComponentModel.DataAnnotations;
using Trazo.Web.Domain;
namespace Trazo.Web.Models;

public class RegistroModel
{
    [Required, StringLength(30, MinimumLength = 3), RegularExpression("^[a-z0-9_]+$", ErrorMessage = "Usá letras minúsculas, números o guion bajo."), Display(Name = "Nombre de usuario")]
    public string NombreUsuario { get; set; } = "";
    [Required, StringLength(80), Display(Name = "Nombre visible")]
    public string NombreVisible { get; set; } = "";
    [Required, EmailAddress, StringLength(254), Display(Name = "Correo de acceso")]
    public string Email { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 12), DataType(DataType.Password), Display(Name = "Contraseña")]
    public string Password { get; set; } = "";
    [Required, Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden."), DataType(DataType.Password), Display(Name = "Repetí la contraseña")]
    public string Confirmacion { get; set; } = "";
}
public class IngresoModel
{
    [Required, EmailAddress, StringLength(254), Display(Name = "Correo de acceso")]
    public string Email { get; set; } = "";
    [Required, StringLength(128), DataType(DataType.Password), Display(Name = "Contraseña")]
    public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
}
public class EditarPerfilModel
{
    [Required, StringLength(80), Display(Name = "Nombre visible")]
    public string NombreVisible { get; set; } = "";
    [StringLength(1200), Display(Name = "Sobre vos")]
    public string? Biografia { get; set; }
    [StringLength(100)] public string? Especialidad { get; set; }
    [Display(Name = "Disponible para trabajar")] public bool DisponibleParaTrabajar { get; set; }
    [EnumDataType(typeof(Visibilidad)), Display(Name = "Visibilidad del perfil")]
    public Visibilidad Visibilidad { get; set; }
    [EnumDataType(typeof(VisibilidadContacto)), Display(Name = "Visibilidad del contacto")]
    public VisibilidadContacto VisibilidadContacto { get; set; }
    [EmailAddress, StringLength(254), Display(Name = "Correo profesional (opcional)")]
    public string? EmailContacto { get; set; }
    [EnumDataType(typeof(TemaPerfil))] public TemaPerfil Tema { get; set; }
    [EnumDataType(typeof(DisposicionPerfil)), Display(Name = "Disposición")]
    public DisposicionPerfil Disposicion { get; set; }
    [Required, RegularExpression("^#[0-9a-fA-F]{6}$"), Display(Name = "Color de acento")]
    public string ColorAcento { get; set; } = "#c4f566";
    [MaxLength(5)] public List<EnlaceModel> Enlaces { get; set; } = [];
}
public class EnlaceModel
{
    [StringLength(40)] public string? Etiqueta { get; set; }
    [StringLength(500)] public string? Url { get; set; }
}
public class EtiquetaModel
{
    [Required, StringLength(40), RegularExpression(@"^[\p{L}\p{N}][\p{L}\p{N} _-]*$", ErrorMessage = "Usá letras, números, espacios o guiones.")]
    public string Nombre { get; set; } = "";
    public bool Activa { get; set; } = true;
}
