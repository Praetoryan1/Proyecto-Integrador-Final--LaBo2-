using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Trazo.Web.Models;

namespace Trazo.Web.Controllers;

public class HomeController : Controller
{
    [HttpGet("/privacidad")]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [Route("/error")]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
