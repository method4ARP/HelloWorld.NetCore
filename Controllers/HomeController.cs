using Microsoft.AspNetCore.Mvc;
using HelloWorld.NetCore.Models;

namespace HelloWorld.NetCore.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Welcome()
    {
        var model = new WelcomeViewModel
        {
            CurrentDate = DateTime.Now
        };
        return View(model);
    }

    public IActionResult Error()
    {
        return View();
    }
}
