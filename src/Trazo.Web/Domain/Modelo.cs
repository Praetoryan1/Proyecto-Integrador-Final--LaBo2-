using Microsoft.AspNetCore.Identity;

namespace Trazo.Web.Domain;

public enum Visibilidad { Publica, Registrados, Privada }
public enum VisibilidadContacto { Publica, Registrados, Oculta }
public enum EstadoCuenta { Activa, Suspendida }
public enum TipoPublicacion { Obra, Actualizacion }
public enum EstadoEditorial { Borrador, Publicada, Archivada }
public enum EstadoModeracion { Visible, Oculta }
public enum TipoRecurso { Imagen, Gif, Video, YouTube }
public enum EstadoProcesamiento { Pendiente, Listo, Rechazado }
public enum EstadoReporte { Pendiente, Resuelto, Desestimado }
public enum TemaPerfil { Oscuro, Claro }
public enum DisposicionPerfil { Cuadricula, Lista }

public static class Roles
{
    public const string Creador = "Creador";
    public const string Administrador = "Administrador";
}

public class Usuario : IdentityUser
{
    public EstadoCuenta Estado { get; set; } = EstadoCuenta.Activa;
    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;
    public Perfil Perfil { get; set; } = null!;
}

public class Perfil
{
    public string UsuarioId { get; set; } = "";
    public Usuario Usuario { get; set; } = null!;
    public string NombreVisible { get; set; } = "";
    public string Biografia { get; set; } = "";
    public string Especialidad { get; set; } = "";
    public bool DisponibleParaTrabajar { get; set; }
    public string? AvatarClave { get; set; }
    public TemaPerfil Tema { get; set; }
    public string ColorAcento { get; set; } = "#c4f566";
    public DisposicionPerfil Disposicion { get; set; }
    public Visibilidad Visibilidad { get; set; } = Visibilidad.Publica;
    public string? EmailContacto { get; set; }
    public VisibilidadContacto VisibilidadContacto { get; set; } = VisibilidadContacto.Oculta;
    public List<EnlacePerfil> Enlaces { get; set; } = [];
}

public class EnlacePerfil
{
    public long Id { get; set; }
    public string PerfilUsuarioId { get; set; } = "";
    public Perfil Perfil { get; set; } = null!;
    public string Etiqueta { get; set; } = "";
    public string Url { get; set; } = "";
    public int Orden { get; set; }
}

public class Publicacion
{
    public long Id { get; set; }
    public string AutorId { get; set; } = "";
    public Usuario Autor { get; set; } = null!;
    public TipoPublicacion Tipo { get; set; }
    public string Titulo { get; set; } = "";
    public string Texto { get; set; } = "";
    public EstadoEditorial EstadoEditorial { get; set; }
    public EstadoModeracion EstadoModeracion { get; set; }
    public Visibilidad Visibilidad { get; set; }
    public int? OrdenDestacado { get; set; }
    public DateTime CreadaUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PublicadaUtc { get; set; }
    public DateTime ActualizadaUtc { get; set; } = DateTime.UtcNow;
    public string Version { get; set; } = Guid.NewGuid().ToString("N");
    public List<RecursoMultimedia> Recursos { get; set; } = [];
    public List<PublicacionEtiqueta> Etiquetas { get; set; } = [];
}

public class RecursoMultimedia
{
    public long Id { get; set; }
    public long PublicacionId { get; set; }
    public Publicacion Publicacion { get; set; } = null!;
    public TipoRecurso Tipo { get; set; }
    public string? ClaveAlmacenamiento { get; set; }
    public string? YouTubeId { get; set; }
    public string? Mime { get; set; }
    public long? Bytes { get; set; }
    public EstadoProcesamiento EstadoProcesamiento { get; set; }
    public int Orden { get; set; }
    public string TextoAlternativo { get; set; } = "";
    public string Creditos { get; set; } = "";
}

public class Etiqueta
{
    public long Id { get; set; }
    public string Nombre { get; set; } = "";
    public string NombreNormalizado { get; set; } = "";
    public bool Activa { get; set; } = true;
    public List<PublicacionEtiqueta> Publicaciones { get; set; } = [];
}

public class PublicacionEtiqueta
{
    public long PublicacionId { get; set; }
    public Publicacion Publicacion { get; set; } = null!;
    public long EtiquetaId { get; set; }
    public Etiqueta Etiqueta { get; set; } = null!;
}

public class Favorito
{
    public string UsuarioId { get; set; } = "";
    public Usuario Usuario { get; set; } = null!;
    public long PublicacionId { get; set; }
    public Publicacion Publicacion { get; set; } = null!;
    public DateTime CreadoUtc { get; set; } = DateTime.UtcNow;
}

public class Seguimiento
{
    public string SeguidorId { get; set; } = "";
    public Usuario Seguidor { get; set; } = null!;
    public string SeguidoId { get; set; } = "";
    public Usuario Seguido { get; set; } = null!;
    public DateTime CreadoUtc { get; set; } = DateTime.UtcNow;
}

public class Reporte
{
    public long Id { get; set; }
    public long PublicacionId { get; set; }
    public Publicacion Publicacion { get; set; } = null!;
    public string DenuncianteId { get; set; } = "";
    public Usuario Denunciante { get; set; } = null!;
    public string? ResueltoPorId { get; set; }
    public Usuario? ResueltoPor { get; set; }
    public string Motivo { get; set; } = "";
    public string Detalle { get; set; } = "";
    public EstadoReporte Estado { get; set; }
    public DateTime CreadoUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ResueltoUtc { get; set; }
    public string? Resolucion { get; set; }
}
