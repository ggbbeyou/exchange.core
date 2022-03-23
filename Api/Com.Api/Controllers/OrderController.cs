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
            return 5;
        }
    }

    /// <summary>
    /// 交易对基础信息
    /// </summary>
    /// <returns></returns>
    public MarketInfoService market_info_db = new MarketInfoService();
    /// <summary>
    /// Service:订单
    /// </summary>
    public OrderService order_service = new OrderService();

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
        CallRequest<List<Orders>> result = new CallRequest<List<Orders>>();
        List<Orders> matchOrders = new List<Orders>();
        MarketInfo? market = this.market_info_db.GetMarketBySymbol(symbol);
        if (market == null)
        {

        }
        else
        {
            foreach (var item in orders)
            {
                Orders orderResult = new Orders();
                orderResult.order_id = FactoryService.instance.constant.worker.NextId();
                orderResult.market = market.market;
                orderResult.symbol = market.symbol;
                orderResult.client_id = item.client_id;
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
                orderResult.trigger_hanging_price = 0;
                orderResult.trigger_cancel_price = 0;
                orderResult.data = null;
                orderResult.remarks = null;
                matchOrders.Add(orderResult);
            }
            Res<List<Orders>> res = this.order_service.PlaceOrder(market, user_id, matchOrders);
            result.data = new List<Orders>();
            foreach (var item in res.data)
            {
                result.data.Add(item);
            }
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
        CallResponse<KeyValuePair<long, List<long>>> res = this.order_service.CancelOrder(market, user_id, type, data);
        return Json(res);
    }



    //       /////////////////////////////////////////////////


    [HttpPost]
    public IActionResult PlaceOrderText()
    {
        List<PlaceOrder> orders = new List<PlaceOrder>();
        Random r = new Random();
        for (int i = 0; i < 500; i++)
        {
            PlaceOrder orderResult = new PlaceOrder();
            orderResult.side = r.Next(0, 2) == 0 ? E_OrderSide.buy : E_OrderSide.sell;
            orderResult.type = r.Next(0, 2) == 0 ? E_OrderType.price_limit : E_OrderType.price_market;
            orderResult.client_id = null;
            if (orderResult.type == E_OrderType.price_limit)
            {
                orderResult.price = (decimal)FactoryService.instance.constant.random.Next(1, 30);
            }
            else
            {
                orderResult.price = null;
            }
            orderResult.amount = (decimal)FactoryService.instance.constant.random.Next(1, 30);
            orders.Add(orderResult);
        }
        PlaceOrder("eth/usdt", orders);
        List<PlaceOrder> orders1 = new List<PlaceOrder>();
        for (int i = 0; i < 500; i++)
        {
            PlaceOrder orderResult = new PlaceOrder();
            orderResult.side = r.Next(0, 2) == 0 ? E_OrderSide.buy : E_OrderSide.sell;
            orderResult.type = r.Next(0, 2) == 0 ? E_OrderType.price_limit : E_OrderType.price_market;
            orderResult.client_id = null;
            if (orderResult.type == E_OrderType.price_limit)
            {
                orderResult.price = (decimal)FactoryService.instance.constant.random.Next(1, 30);
            }
            else
            {
                orderResult.price = null;
            }
            orderResult.amount = (decimal)FactoryService.instance.constant.random.Next(1, 30);
            orders1.Add(orderResult);
        }
        PlaceOrder("btc/usdt", orders);
        return Json(new { });
    }

}
