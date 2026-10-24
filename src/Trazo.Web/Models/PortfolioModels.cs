using Trazo.Web.Domain;
namespace Trazo.Web.Models;

public record TarjetaPublicacion(long Id, string Titulo, string Texto, TipoPublicacion Tipo, string Autor, string NombreUsuario, DateTime? Fecha, string[] Etiquetas);
public record ExplorarModel(Pagina<TarjetaPublicacion> Resultados, string? Q, string? Etiqueta, TipoPublicacion? Tipo);
public record PerfilPublicoModel(string Usuario, string Nombre, string Biografia, string Especialidad, bool Disponible,
    TemaPerfil Tema, DisposicionPerfil Disposicion, string Acento, bool TieneAvatar, string? EmailContacto,
    List<EnlaceModel> Enlaces, Pagina<TarjetaPublicacion> Publicaciones, bool Propio);
public record DetallePublicacionModel(TarjetaPublicacion Publicacion, Visibilidad Visibilidad, EstadoEditorial Estado, bool Propia);
public record PanelModel(string Nombre, string Usuario, int Publicaciones, int Favoritos, int Seguidos);
