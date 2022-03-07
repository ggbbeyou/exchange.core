using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Com.Admin.Models;
using Grpc.Net.Client;
using GrpcExchange;
using Com.Model;
using Newtonsoft.Json;
using Com.Model.Enum;

namespace Com.Admin.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        using var channel = GrpcChannel.ForAddress("https://localhost:5001");
        var client = new ExchangeService.ExchangeServiceClient(channel);
        Req<BaseMarketInfo> req = new Req<BaseMarketInfo>();
        req.op = E_Op.service_init;
        req.market = "btc/usdt";
        req.data = new BaseMarketInfo();
        req.data.market = "btc/usdt";
        req.data.last_price = 38000;
        string json = JsonConvert.SerializeObject(req);
        var reply = await client.UnaryCallAsync(new Request { Json = json });
        _logger.LogInformation(reply.Message);
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
