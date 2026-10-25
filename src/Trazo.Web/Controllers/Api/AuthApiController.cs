using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Trazo.Web.Domain;
using Trazo.Web.Models;
using Trazo.Web.Services;

namespace Trazo.Web.Controllers.Api;

[ApiController, Route("api/v1/auth"), IgnoreAntiforgeryToken, RequestSizeLimit(10_000)]
public class AuthApiController(UserManager<Usuario> users, SignInManager<Usuario> signIn, JwtLocalService jwt) : ControllerBase
{
    [HttpPost("token"), AllowAnonymous, EnableRateLimiting("cuentas")]
    public async Task<ActionResult<TokenRespuesta>> Token(TokenEntrada model)
    {
        var user = await users.FindByEmailAsync(model.Email.Trim());
        var result = user is null ? Microsoft.AspNetCore.Identity.SignInResult.Failed :
            await signIn.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
        if (!result.Succeeded || user?.Estado != EstadoCuenta.Activa || user.TwoFactorEnabled)
            return Problem(statusCode: 401, title: "No se pudo emitir el token", detail: "Credenciales inválidas o cuenta no disponible.");
        // No crea una cookie. El segundo factor necesitaría un flujo distinto del emisor demo.
        return await jwt.EmitirAsync(user);
    }

    [HttpPost("revocar-sesiones"), Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> RevocarSesiones()
    {
        var user = (await users.FindByIdAsync(User.FindFirstValue("sub")!))!;
        var result = await users.UpdateSecurityStampAsync(user);
        if (!result.Succeeded) return Problem(statusCode: 409, title: "La cuenta cambió; volvé a intentar.");
        // Revoca todos los JWT y cookies de la cuenta, comprobados en cada petición.
        return NoContent();
    }
}
