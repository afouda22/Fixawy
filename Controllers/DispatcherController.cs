using Microsoft.AspNetCore.Mvc;

namespace Fixawyy.Controllers;

public class DispatcherController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Operations Dispatcher & Analytics";
        return View();
    }
}
