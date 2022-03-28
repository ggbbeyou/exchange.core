using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Com.Api.Models;

namespace Com.Api.Controllers;

/// <summary>
/// 
/// </summary>
// [Route("[controller]/[action]")]
public class HomeController : Controller
{
    /// <summary>
    /// 
    /// </summary>
    private readonly ILogger<HomeController> _logger;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    // [Route("[action]")]
    [HttpGet]
    [Route("[controller]/[action]")]
    public IActionResult Index()
    {
        return Json("");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public IActionResult Privacy()
    {
        return View();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
