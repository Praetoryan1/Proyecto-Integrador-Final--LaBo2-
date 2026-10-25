namespace Trazo.Web.Models;

public record FavoritosModel(Pagina<TarjetaPublicacion> Resultados, string? Q);
public record CreadorSeguido(string Usuario, string Nombre, string Especialidad, bool TieneAvatar, bool Disponible);
public record SeguidosModel(Pagina<CreadorSeguido> Resultados, string? Q);
