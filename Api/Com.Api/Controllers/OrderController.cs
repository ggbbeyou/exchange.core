using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Com.Api.Models;

namespace Com.Api.Controllers;

public class OrderController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public OrderController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
