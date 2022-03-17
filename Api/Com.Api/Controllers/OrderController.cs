using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Com.Api.Models;
using Com.Bll;
using Com.Db;
using Com.Db.Enum;
using Com.Db.Model;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Com.Api.Controllers;

// [Route("api/[controller]/[action]")]
// [Authorize]
[AllowAnonymous]
public class OrderController : Controller
{
    /// <summary>
    /// 登录玩家id
    /// </summary>
    /// <value></value>
    public int user_id
    {
        get
        {
            Claim? claim = User.Claims.FirstOrDefault(P => P.Type == JwtRegisteredClaimNames.Aud);
            if (claim != null)
            {
                return Convert.ToInt32(claim.Value);
            }
            return 0;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="environment"></param>
    /// <param name="provider"></param>
    /// <param name="logger"></param>
    public OrderController(IServiceProvider provider, IConfiguration configuration, IHostEnvironment environment, ILogger<OrderController> logger)
    {

    }


    /// <summary>
    /// 挂单
    /// </summary>
    /// <param name="market"></param>
    /// <param name="orders"></param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult PlaceOrder(string symbol, List<PlaceOrder> orders)
    {
        List<Orders> matchOrders = new List<Orders>();
        long market = FactoryService.instance.market_info_db.GetMarketBySymbol(symbol);
        foreach (var item in orders)
        {
            Orders orderResult = new Orders();
            orderResult.order_id = FactoryService.instance.constant.worker.NextId();
            orderResult.client_id = item.client_id;
            orderResult.market = market;
            orderResult.symbol = symbol;
            orderResult.uid = user_id;
            orderResult.price = item.price ?? 0;
            orderResult.amount = item.amount;
            orderResult.total = item.price ?? 0 * item.amount;
            orderResult.create_time = DateTimeOffset.UtcNow;
            orderResult.amount_unsold = item.amount;
            orderResult.amount_done = 0;
            orderResult.deal_last_time = null;
            orderResult.side = item.side;
            orderResult.state = E_OrderState.unsold;
            orderResult.type = item.type;
            orderResult.data = null;
            orderResult.remarks = null;
            matchOrders.Add(orderResult);
        }
        Res<List<Orders>> res = FactoryService.instance.order_service.PlaceOrder(market, matchOrders);
        CallRequest<List<Orders>> result = new CallRequest<List<Orders>>();
        result.data = new List<Orders>();
        foreach (var item in res.data)
        {
            result.data.Add(item);
        }
        return Json(result);
    }

    /// <summary>
    /// 撤单
    /// </summary>
    /// <param name="market"></param>
    /// <param name="type">2:按交易对和用户全部撤单,3:按用户和订单id撤单,4:按用户和用户订单id撤单</param>
    /// <param name="orders"></param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult OrderCancel(long market, int type, List<long> data)
    {
        CallResponse<bool> call_res = new CallResponse<bool>();
        if (type != 2 || type != 3 || type != 4 || type != 5)
        {
            return Json(call_res);
        }
        CallResponse<KeyValuePair<long, List<long>>> res = FactoryService.instance.order_service.CancelOrder(market, user_id, type, data);
        return Json(res);
    }



    //       /////////////////////////////////////////////////


    [HttpPost]
    public IActionResult PlaceOrderText()
    {
        List<PlaceOrder> orders = new List<PlaceOrder>();
        for (int i = 0; i < 1000; i++)
        {
            PlaceOrder orderResult = new PlaceOrder();
            orderResult.client_id = null;
            orderResult.price = (decimal)FactoryService.instance.constant.random.Next(4, 10);
            orderResult.amount = (decimal)FactoryService.instance.constant.random.NextDouble();
            orderResult.side = i % 2 == 0 ? E_OrderSide.buy : E_OrderSide.sell;
            orderResult.type = i % 2 == 0 ? E_OrderType.price_fixed : E_OrderType.price_market;
            orders.Add(orderResult);
        }
        return PlaceOrder("btc/usdt", orders);
    }

}
