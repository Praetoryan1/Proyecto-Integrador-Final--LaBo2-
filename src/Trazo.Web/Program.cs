using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trazo.Web.Data;
using Trazo.Web.Domain;
using Trazo.Web.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<TrazoDbContext>((services, options) =>
{
    var connection = services.GetRequiredService<IConfiguration>().GetConnectionString("Trazo")
        ?? throw new InvalidOperationException("Configurá ConnectionStrings:Trazo con tools/Preparar-Entorno.ps1 o dotnet user-secrets. Consultá docs/desarrollo.md.");
    options.UseMySQL(connection);
});
builder.Services.AddIdentity<Usuario, IdentityRole>(o =>
{
    o.User.RequireUniqueEmail = true;
    o.Password.RequiredLength = 12;
    o.Lockout.MaxFailedAccessAttempts = 5;
    o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    o.SignIn.RequireConfirmedAccount = !builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing");
}).AddEntityFrameworkStores<TrazoDbContext>().AddDefaultTokenProviders();
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.Zero);
builder.Services.ConfigureApplicationCookie(o =>
{
    o.Cookie.Name = "__Host-Trazo.Session";
    o.Cookie.HttpOnly = true;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.LoginPath = "/cuenta/ingresar";
    o.AccessDeniedPath = "/cuenta/sin-acceso";
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.Events.OnValidatePrincipal = async context =>
    {
        await SecurityStampValidator.ValidatePrincipalAsync(context);
        if (context.Principal?.Identity?.IsAuthenticated != true) return;
        var users = context.HttpContext.RequestServices.GetRequiredService<UserManager<Usuario>>();
        var user = await users.GetUserAsync(context.Principal);
        if (user?.Estado != EstadoCuenta.Activa)
        {
            context.RejectPrincipal();
            await context.HttpContext.RequestServices.GetRequiredService<SignInManager<Usuario>>().SignOutAsync();
        }
    };
});
builder.Services.AddAntiforgery(o =>
{
    o.Cookie.Name = "__Host-Trazo.Antiforgery";
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.HeaderName = "X-CSRF-TOKEN";
});
builder.Services.AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddSingleton<JwtLocalSettings>();
builder.Services.AddScoped<JwtLocalService>();
// AddAuthentication sin default conserva las cookies Identity como esquema MVC.
builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<JwtLocalSettings>((options, settings) =>
    {
        options.MapInboundClaims = false;
        options.IncludeErrorDetails = false;
        options.TokenValidationParameters = settings.Validacion();
        options.Events = new JwtBearerEvents { OnTokenValidated = JwtLocalService.ValidarCuentaAsync };
    });
builder.Services.AddScoped<EtiquetasConsulta>();
builder.Services.AddScoped<ComunidadService>();
builder.Services.AddScoped<ReportesService>();
builder.Services.AddScoped<CuentasAdminService>();
builder.Services.AddScoped<AvatarService>();
builder.Services.AddScoped<PublicacionesService>();
builder.Services.AddScoped<RecursosService>();
builder.Services.AddSingleton<ProcesoMultimedia>();
builder.Services.AddSingleton<AlmacenMedios>();
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("reportes", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.AddPolicy("cuentas", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
var app = builder.Build();
if (args.Contains("--initialize") || args.Contains("--seed-demo"))
{
    if (args.Contains("--seed-demo") && !app.Environment.IsDevelopment())
        throw new InvalidOperationException("Los datos de demostración solo se permiten en Development.");
    using var scope = app.Services.CreateScope();
    await DbInitializer.InitializeAsync(scope.ServiceProvider, args.Contains("--seed-demo"));
    return;
}
app.UseRequestLocalization(new RequestLocalizationOptions().SetDefaultCulture("es-AR").AddSupportedCultures("es-AR").AddSupportedUICultures("es-AR"));
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    context.Response.Headers.ContentSecurityPolicy = "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; connect-src 'self'; frame-src https://www.youtube-nocookie.com; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";
    context.Response.Headers.CacheControl = "no-store";
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        var jwt = context.RequestServices.GetRequiredService<JwtLocalSettings>();
        if (!jwt.EntornoPermitido) { context.Response.StatusCode = 404; return; }
        if (!jwt.Disponible)
        {
            context.Response.StatusCode = 503;
            await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = 503, Title = "API local sin configurar", Detail = "Ejecutá tools/Preparar-Api.ps1 y reiniciá la aplicación." });
            return;
        }
    }
    await next();
});
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapControllerRoute("default", "{controller=Explorar}/{action=Index}/{id?}");
app.Run();
public partial class Program;
