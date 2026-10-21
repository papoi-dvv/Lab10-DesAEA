using System.Diagnostics;
using Hotel.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Hotel.Web.Controllers;

public class HomeController : Controller
{
    // La portada del sitio es el listado de habitaciones.
    public IActionResult Index() => RedirectToAction("Index", "Habitaciones");

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
