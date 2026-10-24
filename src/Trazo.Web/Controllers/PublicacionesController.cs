using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trazo.Web.Domain;

namespace Trazo.Web.Controllers;

[Authorize(Roles = Roles.Creador)]
public class PublicacionesController : Controller
{
    [HttpGet("/panel/publicaciones")]
    public IActionResult Index() => View();
}
