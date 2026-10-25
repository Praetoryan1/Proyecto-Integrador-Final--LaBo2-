using Trazo.Web.Domain;
namespace Trazo.Web.Models;

public record TarjetaPublicacion(long Id, string Titulo, string Texto, TipoPublicacion Tipo, string Autor, string NombreUsuario, DateTime? Fecha, string[] Etiquetas, long? PortadaId);
public record ExplorarModel(Pagina<TarjetaPublicacion> Resultados, string? Q, string? Etiqueta, TipoPublicacion? Tipo, TipoRecurso? Medio);
public record PerfilPublicoModel(string Usuario, string Nombre, string Biografia, string Especialidad, bool Disponible,
    TemaPerfil Tema, DisposicionPerfil Disposicion, string Acento, bool TieneAvatar, string? EmailContacto,
    List<EnlaceModel> Enlaces, Pagina<TarjetaPublicacion> Publicaciones, bool Propio, bool Siguiendo);
public record DetallePublicacionModel(TarjetaPublicacion Publicacion, Visibilidad Visibilidad, EstadoEditorial Estado, bool Propia, IReadOnlyList<RecursoVista> Recursos, bool Guardado, bool PuedeGuardar);
public record PanelModel(string Nombre, string Usuario, int Publicaciones, int Favoritos, int Seguidos);
