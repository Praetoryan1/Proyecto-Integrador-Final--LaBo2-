using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Trazo.Web.Domain;

namespace Trazo.Web.Data;

public class TrazoDbContext(DbContextOptions<TrazoDbContext> options) : IdentityDbContext<Usuario>(options)
{
    public DbSet<Perfil> Perfiles => Set<Perfil>();
    public DbSet<EnlacePerfil> EnlacesPerfil => Set<EnlacePerfil>();
    public DbSet<Publicacion> Publicaciones => Set<Publicacion>();
    public DbSet<RecursoMultimedia> Recursos => Set<RecursoMultimedia>();
    public DbSet<Etiqueta> Etiquetas => Set<Etiqueta>();
    public DbSet<PublicacionEtiqueta> PublicacionEtiquetas => Set<PublicacionEtiqueta>();
    public DbSet<Favorito> Favoritos => Set<Favorito>();
    public DbSet<Seguimiento> Seguimientos => Set<Seguimiento>();
    public DbSet<Reporte> Reportes => Set<Reporte>();
    public DbSet<CambioEstadoCuenta> CambiosEstadoCuenta => Set<CambioEstadoCuenta>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Usuario>().ToTable("Usuarios");
        b.Entity<IdentityRole>().ToTable("Roles");
        b.Entity<IdentityUserRole<string>>().ToTable("UsuarioRoles");
        b.Entity<IdentityUserClaim<string>>().ToTable("UsuarioClaims");
        b.Entity<IdentityRoleClaim<string>>().ToTable("RolClaims");
        b.Entity<IdentityUserLogin<string>>().ToTable("UsuarioLogins");
        b.Entity<IdentityUserToken<string>>().ToTable("UsuarioTokens");
        b.Entity<Usuario>().Property(x => x.Id).HasMaxLength(191);
        b.Entity<IdentityRole>().Property(x => x.Id).HasMaxLength(191);
        b.Entity<Usuario>().HasIndex(x => x.NormalizedEmail).IsUnique();
        b.Entity<CambioEstadoCuenta>(e =>
        {
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Administrador).WithMany().HasForeignKey(x => x.AdministradorId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Motivo).HasMaxLength(1000);
            e.HasIndex(x => new { x.UsuarioId, x.CreadoUtc, x.Id });
            e.ToTable(t => t.HasCheckConstraint("CK_CambioEstadoCuenta_Transicion", "`EstadoAnterior` IN (0, 1) AND `EstadoNuevo` IN (0, 1) AND `EstadoAnterior` <> `EstadoNuevo` AND `UsuarioId` <> `AdministradorId`"));
        });

        b.Entity<Perfil>(e =>
        {
            e.HasKey(x => x.UsuarioId);
            e.HasOne(x => x.Usuario).WithOne(x => x.Perfil).HasForeignKey<Perfil>(x => x.UsuarioId);
            e.Property(x => x.NombreVisible).HasMaxLength(80);
            e.Property(x => x.Biografia).HasMaxLength(1200);
            e.Property(x => x.Especialidad).HasMaxLength(100);
            e.Property(x => x.AvatarClave).HasMaxLength(40);
            e.Property(x => x.ColorAcento).HasMaxLength(7);
            e.Property(x => x.EmailContacto).HasMaxLength(254);
        });
        b.Entity<EnlacePerfil>(e =>
        {
            e.HasOne(x => x.Perfil).WithMany(x => x.Enlaces).HasForeignKey(x => x.PerfilUsuarioId);
            e.Property(x => x.Etiqueta).HasMaxLength(40);
            e.Property(x => x.Url).HasMaxLength(500);
        });
        b.Entity<Publicacion>(e =>
        {
            e.HasOne(x => x.Autor).WithMany().HasForeignKey(x => x.AutorId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Titulo).HasMaxLength(160);
            e.Property(x => x.Texto).HasMaxLength(12000);
            e.Property(x => x.Version).HasMaxLength(32).IsConcurrencyToken();
            e.HasIndex(x => new { x.AutorId, x.PublicadaUtc, x.Id });
            e.HasIndex(x => new { x.EstadoEditorial, x.EstadoModeracion, x.Visibilidad, x.PublicadaUtc, x.Id });
        });
        b.Entity<RecursoMultimedia>(e =>
        {
            e.HasOne(x => x.Publicacion).WithMany(x => x.Recursos).HasForeignKey(x => x.PublicacionId);
            e.HasIndex(x => new { x.PublicacionId, x.Orden }).IsUnique();
            e.Property(x => x.ClaveAlmacenamiento).HasMaxLength(120);
            e.Property(x => x.YouTubeId).HasMaxLength(11);
            e.Property(x => x.Mime).HasMaxLength(80);
            e.Property(x => x.TextoAlternativo).HasMaxLength(500);
            e.Property(x => x.Creditos).HasMaxLength(300);
            e.ToTable(t => t.HasCheckConstraint("CK_Recurso_Fuente", "(`ClaveAlmacenamiento` IS NOT NULL AND `YouTubeId` IS NULL AND `Tipo` <> 3) OR (`ClaveAlmacenamiento` IS NULL AND `YouTubeId` IS NOT NULL AND `Tipo` = 3)"));
        });
        b.Entity<Etiqueta>(e =>
        {
            e.Property(x => x.Nombre).HasMaxLength(40);
            e.Property(x => x.NombreNormalizado).HasMaxLength(40);
            e.HasIndex(x => x.NombreNormalizado).IsUnique();
        });
        b.Entity<PublicacionEtiqueta>(e =>
        {
            e.HasKey(x => new { x.PublicacionId, x.EtiquetaId });
            e.HasOne(x => x.Publicacion).WithMany(x => x.Etiquetas).HasForeignKey(x => x.PublicacionId);
            e.HasOne(x => x.Etiqueta).WithMany(x => x.Publicaciones).HasForeignKey(x => x.EtiquetaId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<Favorito>(e =>
        {
            e.HasKey(x => new { x.UsuarioId, x.PublicacionId });
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId);
            e.HasOne(x => x.Publicacion).WithMany().HasForeignKey(x => x.PublicacionId);
        });
        b.Entity<Seguimiento>(e =>
        {
            e.HasKey(x => new { x.SeguidorId, x.SeguidoId });
            e.HasOne(x => x.Seguidor).WithMany().HasForeignKey(x => x.SeguidorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Seguido).WithMany().HasForeignKey(x => x.SeguidoId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("CK_Seguimiento_Distinto", "`SeguidorId` <> `SeguidoId`"));
        });
        b.Entity<Reporte>(e =>
        {
            e.HasOne(x => x.Publicacion).WithMany().HasForeignKey(x => x.PublicacionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Denunciante).WithMany().HasForeignKey(x => x.DenuncianteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ResueltoPor).WithMany().HasForeignKey(x => x.ResueltoPorId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Motivo).HasMaxLength(80);
            e.Property(x => x.Detalle).HasMaxLength(2000);
            e.Property(x => x.Resolucion).HasMaxLength(2000);
        });
    }
}
