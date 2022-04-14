using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Com.Api.Sdk.Enum;
using Com.Api.Sdk.Models;
using Com.Bll;
using Com.Db;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Com.Api.Controllers;

/// <summary>
/// 订单接口
/// </summary>
[Route("[controller]")]
[Authorize]
[ApiController]
public class OrderController : ControllerBase
{
    /// <summary>
    /// 日志
    /// </summary>
    private readonly ILogger<OrderController> logger;
    /// <summary>
    /// 登录信息
    /// </summary>
    private (long user_id, long no, string user_name, string app, string public_key) login
    {
        get
        {
            return this.service_common.GetLoginUser(User);
        }
    }
    /// <summary>
    /// service:公共服务
    /// </summary>
    private ServiceCommon service_common = new ServiceCommon();
    /// <summary>
    /// 用户服务
    /// </summary>
    /// <returns></returns>
    private ServiceUser service_user = new ServiceUser();
    /// <summary>
    /// Service:订单
    /// </summary>
    private ServiceOrder service_order = new ServiceOrder();

    /// <summary>
    /// 初始化
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
    [Route("OrderPlace")]
    public Res<List<ResOrder>> OrderPlace(string symbol, List<ReqOrder> orders)
    {
        //判断用户api是否有交易权限
        Res<List<ResOrder>> result = new Res<List<ResOrder>>();
        Users? users = service_user.GetUser(login.user_id);
        if (users == null)
        {
            result.code = E_Res_Code.user_not_found;
            result.message = "未找到该用户";
            return result;
        }
        if (users.disabled || !users.transaction)
        {
            result.code = E_Res_Code.no_permission;
            result.message = "用户禁止下单";
            return result;
        }
        return service_order.PlaceOrder(symbol, login.user_id, login.user_name, orders);
    }

    /// <summary>
    /// 撤单
    /// </summary>
    /// <param name="symbol">交易对</param>
    /// <param name="type">2:按交易对和用户全部撤单,3:按用户和订单id撤单,4:按用户和用户订单id撤单</param>
    /// <param name="data"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("OrderCancel")]
    public Res<bool> OrderCancel(string symbol, int type, List<long> data)
    {
        return this.service_order.CancelOrder(symbol, login.user_id, type, data);
    }

    /// <summary>
    /// 当前用户委托挂单
    /// </summary>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    /// <param name="skip">跳过多少行</param>
    /// <param name="take">获取多少行</param>
    /// <returns></returns>
    [HttpGet]
    [Route("GetOrderCurrent")]
    [ResponseCache(CacheProfileName = "cache_0")]
    public Res<List<ResOrder>> GetOrderCurrent(DateTimeOffset? start, DateTimeOffset? end, int skip = 0, int take = 50)
    {
        return this.service_order.GetOrder(true, login.user_id, skip, take, start, end);
    }

    /// <summary>
    /// 当前用户历史委托挂单
    /// </summary>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    /// <param name="skip">跳过多少行</param>
    /// <param name="take">获取多少行</param>
    /// <returns></returns>
    [HttpGet]
    [Route("GetOrderHistory")]
    [ResponseCache(CacheProfileName = "cache_1")]
    public Res<List<ResOrder>> GetOrderHistory(DateTimeOffset? start, DateTimeOffset? end, int skip = 0, int take = 50)
    {
        return this.service_order.GetOrder(false, login.user_id, skip, take, start, end);
    }

    /// <summary>
    /// 按订单id查询
    /// </summary>
    /// <param name="market"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("GetOrderById")]
    [ResponseCache(CacheProfileName = "cache_2")]
    public Res<List<ResOrder>> GetOrderById(long market, List<long> data)
    {
        return this.service_order.GetOrder(market: market, uid: login.user_id, ids: data);
    }

    /// <summary>
    /// 按订单状态查询
    /// </summary>
    /// <param name="market"></param>
    /// <param name="state"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("GetOrderByState")]
    [ResponseCache(CacheProfileName = "cache_2")]
    public Res<List<ResOrder>> GetOrderByState(long market, E_OrderState state, DateTimeOffset start, DateTimeOffset end)
    {
        return this.service_order.GetOrder(market: market, uid: login.user_id, state: state, start: start, end: end);
    }

    /// <summary>
    /// 订单时间查询
    /// </summary>
    /// <param name="market"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("GetOrderByDate")]
    [ResponseCache(CacheProfileName = "cache_2")]
    public Res<List<ResOrder>> GetOrderByDate(long market, DateTimeOffset start, DateTimeOffset end)
    {
        return this.service_order.GetOrder(market: market, uid: login.user_id, start: start, end: end);
    }

}