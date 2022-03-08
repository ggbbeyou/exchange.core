using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Com.Api.Models;
using Com.Api.Model;

namespace Com.Api.Controllers;

public class OrderController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public OrderController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public async Task<IActionResult> PlaceOrder(string market, List<PlaceOrder> order)
    {
        WebCallResult<string> result = new WebCallResult<string>();
        
        return Json(result);
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
