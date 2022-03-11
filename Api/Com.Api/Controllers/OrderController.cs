using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Com.Api.Models;
using Com.Bll;
using Com.Db;
using Com.Bll.ApiModel;
using Com.Db.Enum;
using Com.Db.Model;

namespace Com.Api.Controllers;

public class OrderController : Controller
{
    /// <summary>
    /// 常用接口
    /// </summary>
    private FactoryConstant constant = null!;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="environment"></param>
    /// <param name="provider"></param>
    /// <param name="logger"></param>
    public OrderController(IServiceProvider provider, IConfiguration configuration, IHostEnvironment environment, ILogger<OrderController> logger)
    {
        this.constant = new FactoryConstant(provider, configuration, environment, logger);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="market"></param>
    /// <param name="orders"></param>
    /// <returns></returns>
    public IActionResult PlaceOrder(long market, List<PlaceOrder> orders)
    {
        List<Orders> matchOrders = new List<Orders>();
        foreach (var item in orders)
        {
            Orders orderResult = new Orders();
            orderResult.order_id = this.constant.worker.NextId();
            orderResult.client_id = item.client_id;
            orderResult.market = market;
            orderResult.uid = 1;
            orderResult.price = item.price ?? 0;
            orderResult.amount = item.amount;
            orderResult.total = item.price ?? 0 * item.amount;
            orderResult.create_time = DateTimeOffset.UtcNow;
            orderResult.amount_unsold = 0;
            orderResult.amount_done = item.amount;
            orderResult.deal_last_time = null;
            orderResult.side = item.side;
            orderResult.state = E_OrderState.unsold;
            orderResult.type = item.type;
            orderResult.data = null;
            orderResult.remarks = null;
            matchOrders.Add(orderResult);
        }
        Res<List<Orders>> res = FactoryService.instance.order_service.PlaceOrder(market, matchOrders);
        WebCallResult<List<Orders>> result = new WebCallResult<List<Orders>>();
        result.success = true;
        result.code = 0;
        result.message = res.message;
        result.data = new List<Orders>();
        foreach (var item in res.data)
        {
            result.data.Add(item);
        }
        return Json(result);
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
