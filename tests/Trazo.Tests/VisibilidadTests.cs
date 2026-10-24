using Trazo.Web.Domain;
using Trazo.Web.Services;

namespace Trazo.Tests;

public class VisibilidadTests
{
    [Theory]
    [InlineData(Visibilidad.Publica, Visibilidad.Publica, null, true)]
    [InlineData(Visibilidad.Registrados, Visibilidad.Publica, null, false)]
    [InlineData(Visibilidad.Publica, Visibilidad.Registrados, null, false)]
    [InlineData(Visibilidad.Publica, Visibilidad.Privada, "otro", false)]
    [InlineData(Visibilidad.Privada, Visibilidad.Publica, "otro", false)]
    [InlineData(Visibilidad.Registrados, Visibilidad.Registrados, "otro", true)]
    [InlineData(Visibilidad.Privada, Visibilidad.Privada, "autor", true)]
    public void Se_aplica_la_visibilidad_mas_restrictiva(Visibilidad publicacion, Visibilidad perfil, string? visitante, bool esperado)
    {
        var post = Crear(publicacion, perfil);
        Assert.Equal(esperado, new[] { post }.AsQueryable().LegiblesPor(visitante).Any());
    }

    [Theory]
    [InlineData(EstadoEditorial.Borrador, EstadoModeracion.Visible)]
    [InlineData(EstadoEditorial.Archivada, EstadoModeracion.Visible)]
    [InlineData(EstadoEditorial.Publicada, EstadoModeracion.Oculta)]
    public void Estados_no_publicos_solo_son_legibles_por_autor(EstadoEditorial editorial, EstadoModeracion moderacion)
    {
        var post = Crear(Visibilidad.Publica, Visibilidad.Publica);
        post.EstadoEditorial = editorial; post.EstadoModeracion = moderacion;
        Assert.Empty(new[] { post }.AsQueryable().LegiblesPor("otro"));
        Assert.Single(new[] { post }.AsQueryable().LegiblesPor("autor"));
    }

    [Fact]
    public void Suspension_oculta_el_contenido_incluso_al_propietario()
    {
        var post = Crear(Visibilidad.Publica, Visibilidad.Publica);
        post.Autor.Estado = EstadoCuenta.Suspendida;
        Assert.Empty(new[] { post }.AsQueryable().LegiblesPor("autor"));
    }

    [Fact]
    public void Contacto_oculto_no_es_visible_para_otra_cuenta()
    {
        var profile = new Perfil { UsuarioId = "autor", VisibilidadContacto = VisibilidadContacto.Oculta };
        Assert.False(VisibilidadService.PuedeVerContacto(profile, "otro"));
        Assert.True(VisibilidadService.PuedeVerContacto(profile, "autor"));
    }

    private static Publicacion Crear(Visibilidad post, Visibilidad perfil) => new()
    {
        AutorId = "autor",
        Visibilidad = post,
        EstadoEditorial = EstadoEditorial.Publicada,
        Autor = new Usuario { Id = "autor", Perfil = new Perfil { UsuarioId = "autor", Visibilidad = perfil } }
    };
}
