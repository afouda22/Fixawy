using Fixawyy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fixawyy.Controllers;

[Authorize(Roles = AppRoles.OperationsAuthorizedRoles)]
public class DispatcherController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Operations Dispatcher & Analytics";
        return View();
    }
}
