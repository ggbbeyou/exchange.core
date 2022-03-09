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
        GrpcChannel channel = GrpcChannel.ForAddress("http://192.168.2.5:8080");
        var client = new ExchangeService.ExchangeServiceClient(channel);


        // var client1 = new Health.HealthClient(channel);

        // var response = client1.CheckAsync(new HealthCheckRequest());
        // var status = response.Status;

        // inti
        Req<string> req = new Req<string>();
        req.op = E_Op.service_init;
        req.market = "btc/usdt";
        BaseMarketInfo info = new BaseMarketInfo();
        info.market = "btc/usdt";
        info.last_price = 38000.123456789m;
        req.data = JsonConvert.SerializeObject(info);
        string json = JsonConvert.SerializeObject(req);
        var reply = await client.UnaryCallAsync(new Request { Json = json });

        //start

        req.op = E_Op.service_start;
        req.market = "btc/usdt";      
        req.data = JsonConvert.SerializeObject(info);
        json = JsonConvert.SerializeObject(req);
        var reply2 = await client.UnaryCallAsync(new Request { Json = json });



        channel.ShutdownAsync().Wait();
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
