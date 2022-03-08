using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Com.Api.Models;
using Com.Api.Model;
using Com.Bll;
using Com.Model;

namespace Com.Api.Controllers;

public class OrderController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public OrderController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="market"></param>
    /// <param name="orders"></param>
    /// <returns></returns>
    public async Task<IActionResult> PlaceOrder(string market, List<PlaceOrder> orders)
    {
        WebCallResult<List<BaseOrder>> result = new WebCallResult<List<BaseOrder>>();
        List<BaseOrder> order = new List<BaseOrder>();
        result.data = await OrderService.instance.PlaceOrder(market, 1, order);
        return Json(result);
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
