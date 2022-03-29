using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Com.Api.Sdk.Models;
using Com.Bll;
using Com.Db;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Com.Api.Controllers;

/// <summary>
/// 
/// </summary>
[Route("[controller]")]
[ApiController]
public class OrderController : ControllerBase
{
    private readonly ILogger<OrderController> logger;
    /// <summary>
    /// 登录玩家id
    /// </summary>
    /// <value></value>
    public int uid
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
    /// 
    /// </summary>
    /// <value></value>
    public string user_name
    {
        get
        {
            Claim? claim = User.Claims.FirstOrDefault(P => P.Type == JwtRegisteredClaimNames.Aud);
            if (claim != null)
            {
                return (claim.Value);
            }
            return "";
        }
    }
    /// <summary>
    /// 用户服务
    /// </summary>
    /// <returns></returns>
    private ServiceUser user_service = new ServiceUser();

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
    /// <param name="logger"></param>
    public OrderController(ILogger<OrderController> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// 挂单
    /// </summary>
    /// <param name="symbol">交易对</param>
    /// <param name="orders">订单数据</param>
    /// <returns></returns>
    [HttpPost]
    [Route("PlaceOrder")]
    // [Authorize]
    public ResCall<List<ResOrder>> PlaceOrder(string symbol, List<ReqOrder> orders)
    {
        //判断用户api是否有交易权限
        ResCall<List<ResOrder>> result = new ResCall<List<ResOrder>>();
        // Users? users = user_service.GetUser(uid);
        // if (users == null)
        // {
        //     result.code = E_Res_Code.no_user;
        //     result.message = "未找到该用户";
        //     return Json(result);
        // }
        // if (users.disabled || !users.transaction)
        // {
        //     result.code = E_Res_Code.no_permission;
        //     result.message = "用户禁止下单";
        //     return Json(result);
        // }
        ResCall<List<Orders>> res = service_order.PlaceOrder(symbol, uid, user_name, orders);
        result = new ResCall<List<ResOrder>>()
        {
            success = res.success,
            code = res.code,
            message = res.message,
            op = res.op,
            market = res.market,
            data = res.data.ConvertAll(P => (ResOrder)P)
        };
        return result;
    }

    /// <summary>
    /// 撤单
    /// </summary>
    /// <param name="market"></param>
    /// <param name="type">2:按交易对和用户全部撤单,3:按用户和订单id撤单,4:按用户和用户订单id撤单</param>
    /// <param name="data"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("OrderCancel")]
    public ResCall<KeyValuePair<long, List<long>>> OrderCancel(long market, int type, List<long> data)
    {
        ResCall<KeyValuePair<long, List<long>>> res = new ResCall<KeyValuePair<long, List<long>>>();
        if (type != 2 || type != 3 || type != 4 || type != 5)
        {
            return res;
        }
        res = this.service_order.CancelOrder(market, uid, type, data);
        return res;
    }

}