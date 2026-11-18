using Fixawyy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fixawyy.Controllers;

[Authorize(Roles = AppRoles.DispatcherAuthorizedRoles)]
public class DispatcherController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Facility Operations Dispatcher & Analytics";
        return View();
    }
}
