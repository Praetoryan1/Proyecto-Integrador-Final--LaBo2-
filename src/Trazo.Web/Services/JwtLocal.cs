using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Trazo.Web.Domain;
using Trazo.Web.Models;

namespace Trazo.Web.Services;

// Emisor académico local. No es un servidor OAuth/OIDC para producción.
public sealed class JwtLocalSettings
{
    public const string Issuer = "trazo-local";
    public const string Audience = "trazo-api-evaluacion";
    public const int DuracionSegundos = 600;
    public bool EntornoPermitido { get; }
    public SymmetricSecurityKey? Key { get; }
    public bool Disponible => EntornoPermitido && Key is not null;

    public JwtLocalSettings(IWebHostEnvironment environment, IConfiguration configuration)
    {
        EntornoPermitido = environment.IsDevelopment() || environment.IsEnvironment("Testing");
        if (!EntornoPermitido) return;
        try
        {
            var bytes = Convert.FromBase64String(configuration["Jwt:SigningKey"] ?? "");
            if (bytes.Length >= 32) Key = new SymmetricSecurityKey(bytes);
        }
        catch (FormatException) { /* La API devuelve 503; MVC puede seguir funcionando. */ }
    }

    public TokenValidationParameters Validacion() => new()
    {
        ValidateIssuer = true, ValidIssuer = Issuer,
        ValidateAudience = true, ValidAudience = Audience,
        ValidateIssuerSigningKey = true, IssuerSigningKey = Key,
        ValidateLifetime = true, RequireExpirationTime = true, RequireSignedTokens = true,
        ClockSkew = TimeSpan.FromSeconds(30), ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ValidTypes = ["at+jwt"], NameClaimType = JwtRegisteredClaimNames.Sub, RoleClaimType = "role"
    };
}

public sealed class JwtLocalService(JwtLocalSettings settings, UserManager<Usuario> users)
{
    public static string Sello(string? stamp) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(stamp ?? "")));

    public async Task<TokenRespuesta> EmitirAsync(Usuario user)
    {
        var now = DateTime.UtcNow;
        var roles = await users.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new("stamp", Sello(user.SecurityStamp))
        };
        claims.AddRange(roles.Select(role => new Claim("role", role)));
        var token = new JwtSecurityToken(JwtLocalSettings.Issuer, JwtLocalSettings.Audience, claims, now,
            now.AddSeconds(JwtLocalSettings.DuracionSegundos), new SigningCredentials(settings.Key!, SecurityAlgorithms.HmacSha256));
        token.Header["typ"] = "at+jwt";
        return new(new JwtSecurityTokenHandler().WriteToken(token), "Bearer", JwtLocalSettings.DuracionSegundos);
    }

    public static async Task ValidarCuentaAsync(TokenValidatedContext context)
    {
        var users = context.HttpContext.RequestServices.GetRequiredService<UserManager<Usuario>>();
        var principal = context.Principal!;
        var id = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var user = id is null ? null : await users.FindByIdAsync(id);
        if (user is null || user.Estado != EstadoCuenta.Activa ||
            string.IsNullOrEmpty(user.SecurityStamp) || principal.FindFirstValue("stamp") != Sello(user.SecurityStamp) ||
            await users.IsLockedOutAsync(user))
        {
            context.Fail("Credencial no vigente.");
            return;
        }
        // Los roles vigentes de la base prevalecen sobre los emitidos en el token.
        var identity = (ClaimsIdentity)principal.Identity!;
        foreach (var claim in identity.FindAll("role").ToList()) identity.RemoveClaim(claim);
        foreach (var role in await users.GetRolesAsync(user)) identity.AddClaim(new("role", role));
    }
}
