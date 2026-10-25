using System.ComponentModel.DataAnnotations;

namespace Trazo.Web.Models;

public class TokenEntrada
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = "";
    [Required, StringLength(256)]
    public string Password { get; set; } = "";
}

public record TokenRespuesta(string AccessToken, string TokenType, int ExpiresIn);
