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
    /// 常用接口
    /// </summary>
    private FactoryConstant constant = null!;
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
        this.constant = new FactoryConstant(provider, configuration, environment, logger);
    }

    [HttpPost]
    public IActionResult A()
    {
        return Json("");
    }

    /// <summary>
    /// 挂单
    /// </summary>
    /// <param name="market"></param>
    /// <param name="orders"></param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult PlaceOrder(long market, List<PlaceOrder> orders)
    {
        List<Orders> matchOrders = new List<Orders>();
        foreach (var item in orders)
        {
            Orders orderResult = new Orders();
            orderResult.order_id = this.constant.worker.NextId();
            orderResult.client_id = item.client_id;
            orderResult.market = market;
            orderResult.uid = user_id;
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
    /// <param name="orders"></param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult OrderCancel(long market, List<long> cancel)
    {
        List<Orders> matchOrders = new List<Orders>();
        foreach (var item in cancel)
        {
            Orders orderResult = new Orders();
            orderResult.order_id = this.constant.worker.NextId();
            orderResult.client_id = item.client_id;
            orderResult.market = market;
            orderResult.uid = user_id;
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
        CallRequest<List<Orders>> result = new CallRequest<List<Orders>>();
        result.data = new List<Orders>();
        foreach (var item in res.data)
        {
            result.data.Add(item);
        }
        return Json(result);
    }



}
