using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Com.Api.Models;
using Com.Api.Model;
using Com.Bll;
using Com.Model;
using Com.Model.Enum;

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
        List<BaseOrder> matchOrders = new List<BaseOrder>();
        foreach (var item in orders)
        {
            BaseOrder orderResult = new BaseOrder();
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
        Res<List<BaseOrder>> res = FactoryService.instance.order_service.PlaceOrder(market, matchOrders);
        WebCallResult<List<BaseOrder>> result = new WebCallResult<List<BaseOrder>>();
        result.success = true;
        result.code = 0;
        result.message = res.message;
        result.data = new List<BaseOrder>();
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
