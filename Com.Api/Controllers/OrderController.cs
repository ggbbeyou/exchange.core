using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Com.Api.Models;
using Com.Bll;
using Com.Db;
using Com.Api.Sdk.Enum;
using Com.Db.Model;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Com.Api.Sdk.Models;

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
    public ServiceMarket service_market = new ServiceMarket();

    /// <summary>
    /// Service:订单
    /// </summary>
    public ServiceOrder service_order = new ServiceOrder();

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
    /// <param name="symbol">交易对</param>
    /// <param name="orders">订单数据</param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult PlaceOrder(string symbol, List<ReqOrder> orders)
    {
        //判断用户api是否有交易权限
        ResCall<List<Orders>> res = service_order.PlaceOrder(symbol, user_id, orders);
        ResCall<List<ResOrder>> result = new ResCall<List<ResOrder>>()
        {
            success = res.success,
            code = res.code,
            message = res.message,
            op = res.op,
            market = res.market,
            data = res.data.ConvertAll(P => (ResOrder)P)
        };
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
        ResCall<bool> call_res = new ResCall<bool>();
        if (type != 2 || type != 3 || type != 4 || type != 5)
        {
            return Json(call_res);
        }
        ResCall<KeyValuePair<long, List<long>>> res = this.service_order.CancelOrder(market, user_id, type, data);
        return Json(res);
    }



    //       /////////////////////////////////////////////////




}
