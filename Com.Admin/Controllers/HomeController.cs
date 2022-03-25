using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Com.Admin.Models;
using Grpc.Net.Client;
using GrpcExchange;
using Newtonsoft.Json;
using Snowflake.Core;
using Com.Bll;
using Com.Db;
using Com.Api.Sdk.Enum;

using System.Data;
using Com.Bll.Util;

namespace Com.Admin.Controllers;

public class HomeController : Controller
{
    /// <s ummary>
    /// 雪花算法
    /// </summary>
    /// <returns></returns>
    public readonly IdWorker worker = new IdWorker(1, 1);
    public readonly Random random = new Random();


    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="environment"></param>
    /// <param name="provider"></param>
    /// <param name="logger"></param>
    public HomeController(IServiceProvider provider, IConfiguration configuration, IHostEnvironment environment, ILogger<HomeController> logger)
    {

    }

    public IActionResult Index()
    {
        // GrpcChannel channel = GrpcChannel.ForAddress("http://192.168.2.5:8080");
        // var client = new ExchangeService.ExchangeServiceClient(channel);




        // // inti
        // CallRequest<string> req = new CallRequest<string>();
        // req.op = E_Op.service_warm_cache;
        // req.market = 1;
        // MarketInfo info = new MarketInfo();
        // info.market = 1;
        // info.last_price = 38000.123456789m;
        // req.data = JsonConvert.SerializeObject(info);
        // string json = JsonConvert.SerializeObject(req);
        // var reply = await client.UnaryCallAsync(new Request { Json = json });

        // //start

        // req.op = E_Op.service_start;
        // req.market = 1;
        // req.data = JsonConvert.SerializeObject(info);
        // json = JsonConvert.SerializeObject(req);
        // var reply2 = await client.UnaryCallAsync(new Request { Json = json });



        // channel.ShutdownAsync().Wait();

        return View();
    }




    public IActionResult Privacy()
    {


        // long market = 1;
        // List<Orders> orders = new List<Orders>();
        // for (int i = 0; i < 10; i++)
        // {
        //     Orders order = new Orders();
        //     Orders orderResult = new Orders();
        //     orderResult.order_id = worker.NextId();
        //     orderResult.client_id = null;
        //     orderResult.market = market;
        //     orderResult.uid = 1;
        //     orderResult.price = (decimal)random.NextDouble();
        //     orderResult.amount = (decimal)random.NextDouble();
        //     orderResult.total = orderResult.price * orderResult.amount;
        //     orderResult.create_time = DateTimeOffset.UtcNow;
        //     orderResult.amount_unsold = 0;
        //     orderResult.amount_done = orderResult.amount;
        //     orderResult.deal_last_time = null;
        //     orderResult.side = i % 2 == 0 ? E_OrderSide.buy : E_OrderSide.sell;
        //     orderResult.state = E_OrderState.unsold;
        //     orderResult.type = i % 2 == 0 ? E_OrderType.price_fixed : E_OrderType.price_market;
        //     orderResult.data = null;
        //     orderResult.remarks = null;
        //     orders.Add(order);
        // }
        // Res<List<Orders>> res = FactoryService.instance.order_service.PlaceOrder(market, orders);

        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public IActionResult Upload()
    {
        var files = Request.Form.Files.First();
        MemoryStream ms = new MemoryStream();
        files.CopyTo(ms);
        byte[] a = ms.ToArray();

        DataTable? dt = ExcelHelper.OpenExcel(files.OpenReadStream(), Path.GetExtension(files.FileName));
        Dictionary<DateTimeOffset, decimal> dic = new Dictionary<DateTimeOffset, decimal>();
        if (dt != null)
        {
            foreach (DataRow row in dt.Rows)
            {
                // dic.Add(DateTimeOffset.Parse(row[0].ToString()), decimal.Parse(row[1].ToString()));


            }
        }
        return Json(new { code = 0, msg = "ok" });
    }


    public void test()
    {
        // var date = Request;
        // var files = Request.Form.Files;
        // long size = files.Sum(f => f.Length);
        // string webRootPath = _hostingEnvironment.WebRootPath;
        // string contentRootPath = _hostingEnvironment.ContentRootPath;
        // foreach (var formFile in files)
        // {
        //     if (formFile.Length > 0)
        //     {

        //         string fileExt = GetFileExt(formFile.FileName); //文件扩展名，不含“.”
        //         long fileSize = formFile.Length; //获得文件大小，以字节为单位
        //         string newFileName = System.Guid.NewGuid().ToString() + "." + fileExt; //随机生成新的文件名
        //         var filePath = webRootPath + "/upload/" + newFileName;
        //         using (var stream = new FileStream(filePath, FileMode.Create))
        //         {

        //             await formFile.CopyToAsync(stream);
        //         }
        //     }
        // }






    }

    // private DataTable ReadExcelToTable(Stream stream)
    // {
    //     DataTable result = new DataTable();
    //     Workbook workbook = new Workbook();
    //     workbook.Open(stream);
    //     Cells cells = workbook.Worksheets[0].Cells;
    //     result = cells.ExportDataTableAsString(0, 0, cells.MaxDataRow + 1, cells.MaxColumn + 1, false);
    //     return result;
    // }

}
