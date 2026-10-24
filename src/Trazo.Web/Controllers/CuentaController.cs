using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Trazo.Web.Data;
using Trazo.Web.Domain;
using Trazo.Web.Models;
namespace Trazo.Web.Controllers;

[Route("cuenta")]
public class CuentaController(UserManager<Usuario> users, SignInManager<Usuario> signIn, TrazoDbContext db, IWebHostEnvironment environment) : Controller
{
    [HttpGet("registro"), AllowAnonymous]
    public IActionResult Registro() => View(new RegistroModel());
    [HttpPost("registro"), AllowAnonymous, EnableRateLimiting("cuentas")]
    public async Task<IActionResult> Registro(RegistroModel model)
    {
        // Falta correo transaccional: no abrir registros públicos en este incremento.
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing")) return StatusCode(503);
        if (string.IsNullOrWhiteSpace(model.NombreVisible)) ModelState.AddModelError(nameof(model.NombreVisible), "Ingresá un nombre visible.");
        if (!ModelState.IsValid) return View(model);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var user = new Usuario { UserName = model.NombreUsuario, Email = model.Email.Trim() };
        var result = await users.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "No se pudo crear la cuenta. Revisá los datos, elegí otro usuario o correo y usá una contraseña con mayúscula, minúscula, número y símbolo.");
            return View(model);
        }
        result = await users.AddToRoleAsync(user, Roles.Creador);
        if (!result.Succeeded) throw new InvalidOperationException("No se pudo asignar el rol inicial. Ejecutar --initialize.");
        db.Perfiles.Add(new Perfil { UsuarioId = user.Id, NombreVisible = model.NombreVisible.Trim() });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        await signIn.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Editar", "Perfil");
    }
    [HttpGet("ingresar"), AllowAnonymous]
    public IActionResult Ingresar(string? returnUrl) => View(new IngresoModel { ReturnUrl = returnUrl });
    [HttpPost("ingresar"), AllowAnonymous, EnableRateLimiting("cuentas")]
    public async Task<IActionResult> Ingresar(IngresoModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await users.FindByEmailAsync(model.Email.Trim());
        var result = user is null ? Microsoft.AspNetCore.Identity.SignInResult.Failed :
            await signIn.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
        if (!result.Succeeded || user?.Estado != EstadoCuenta.Activa)
        {
            ModelState.AddModelError("", "No pudimos iniciar sesión. Revisá tus datos o intentá nuevamente más tarde.");
            return View(model);
        }
        await signIn.SignInAsync(user, isPersistent: false);
        return Url.IsLocalUrl(model.ReturnUrl) ? LocalRedirect(model.ReturnUrl!) : RedirectToAction("Index", "Panel");
    }
    [HttpPost("salir"), Authorize]
    public async Task<IActionResult> Salir()
    {
        await signIn.SignOutAsync();
        return RedirectToAction("Index", "Explorar");
    }
    [HttpGet("sin-acceso")]
    public IActionResult SinAcceso() { Response.StatusCode = 403; return View(); }
}
