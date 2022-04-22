using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Com.Api.Sdk.Enum;
using Com.Api.Sdk.Models;
using Com.Api.Src;
using Com.Bll;
using Com.Db;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Com.Api.Controllers;

/// <summary>
/// 订单接口
/// </summary>
[TypeFilter(typeof(VerificationFilters))]
[ApiController]
[Route("api/order")]
public class ApiOrderController : ControllerBase
{
    /// <summary>
    /// 日志
    /// </summary>
    private readonly ILogger<ApiOrderController> logger;
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
    public ApiOrderController(ILogger<ApiOrderController> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// 挂单
    /// </summary>
    /// <param name="symbol">交易对名称</param>
    /// <param name="trade_model">交易模式,现货(cash)</param>
    /// <param name="type">订单类型</param>
    /// <param name="side">交易方向</param>
    /// <param name="price">挂单价:限价单(有效),其它无效</param>
    /// <param name="amount">挂单量:限价单/市场卖价(有效),其它无效</param>
    /// <param name="total">挂单额:市价买单(有效),其它无效</param>
    /// <param name="trigger_hanging_price">触发挂单价格</param>
    /// <param name="trigger_cancel_price">触发撤单价格</param>
    /// <param name="client_id">客户自定义订单id</param>
    /// <returns></returns>
    [HttpPost]
    [Route("OrderPlace")]
    public Res<List<ResOrder>> OrderPlace(string symbol, E_TradeModel trade_model, E_OrderType type, E_OrderSide side, decimal? price, decimal? amount, decimal? total, decimal? trigger_hanging_price, decimal? trigger_cancel_price, string? client_id)
    {
        (bool transaction, Users? users, UsersApi? api) user_api = service_user.ApiUserTransaction(Request.Headers["api_key"]);
        if (user_api.transaction == false || user_api.users == null)
        {
            Res<List<ResOrder>> result = new Res<List<ResOrder>>();
            result.code = E_Res_Code.user_disable_place_order;
            result.message = "用户禁止交易";
            return result;
        }
        else
        {
            List<ReqOrder> orders = new List<ReqOrder>()
            {
                new ReqOrder()
                {
                    symbol = symbol,
                    trade_model = trade_model,
                    type = type,
                    side = side,
                    price = price,
                    amount = amount,
                    total = total,
                    trigger_hanging_price = trigger_hanging_price??0,
                    trigger_cancel_price = trigger_cancel_price??0,
                    client_id = client_id
                }
            };
            return service_order.PlaceOrder(symbol, user_api.users.user_id, user_api.users.user_name, orders);
        };
    }

    /// <summary>
    /// 批量挂单
    /// </summary>
    /// <param name="data">挂单数据</param>
    /// <returns></returns>
    [HttpPost]
    [Route("OrderPlaces")]
    public Res<List<ResOrder>> OrderPlaces(CallOrder data)
    {
        (bool transaction, Users? users, UsersApi? api) user_api = service_user.ApiUserTransaction(Request.Headers["api_key"]);
        if (user_api.transaction == false || user_api.users == null)
        {
            Res<List<ResOrder>> result = new Res<List<ResOrder>>();
            result.code = E_Res_Code.user_disable_place_order;
            result.message = "用户禁止交易";
            return result;
        }
        else
        {
            return service_order.PlaceOrder(data.symbol, user_api.users.user_id, user_api.users.user_name, data.orders);
        }
    }

    /// <summary>
    /// 按交易对,用户撤单
    /// </summary>
    /// <param name="symbol">交易对</param>
    /// <returns></returns>
    [HttpPost]
    [Route("OrderCancelByUserId")]
    public Res<bool> OrderCancelByUserId(string symbol)
    {
        (bool transaction, Users? users, UsersApi? api) user_api = service_user.ApiUserTransaction(Request.Headers["api_key"]);
        if (user_api.transaction == false || user_api.users == null)
        {
            Res<bool> result = new Res<bool>();
            result.code = E_Res_Code.user_disable_place_order;
            result.message = "用户禁止撤单";
            return result;
        }
        else
        {
            return this.service_order.CancelOrder(symbol, user_api.users.user_id, 2, new List<long>());
        }
    }

    /// <summary>
    ///  按交易对,订单id撤单
    /// </summary>
    /// <param name="model">订单id</param>
    /// <returns></returns>
    [HttpPost]
    [Route("OrderCancelByOrderid")]
    public Res<bool> OrderCancelByOrderid(CallOrderCancel model)
    {
        (bool transaction, Users? users, UsersApi? api) user_api = service_user.ApiUserTransaction(Request.Headers["api_key"]);
        if (user_api.transaction == false || user_api.users == null)
        {
            Res<bool> result = new Res<bool>();
            result.code = E_Res_Code.user_disable_place_order;
            result.message = "用户禁止撤单";
            return result;
        }
        else
        {
            return this.service_order.CancelOrder(model.symbol, user_api.users.user_id, 3, model.data);
        }
    }

    // /// <summary>
    // ///  按用户,交易对,用户自定义id撤单
    // /// </summary>
    // /// <param name="model">订单id</param>
    // /// <returns></returns>
    // [HttpPost]
    // [Route("OrderCancelByClientId")]
    // public Res<bool> OrderCancelByClientId(CallOrderCancel model)
    // {
    //     (bool transaction, Users? users, UsersApi? api) user_api = service_user.ApiUserTransaction(Request.Headers["api_key"]);
    //     if (user_api.transaction == false || user_api.users == null)
    //     {
    //         Res<bool> result = new Res<bool>();
    //         result.code = E_Res_Code.user_disable_place_order;
    //         result.message = "用户禁止撤单";
    //         return result;
    //     }
    //     else
    //     {
    //         return this.service_order.CancelOrder(model.symbol, user_api.users.user_id, 4, model.data);
    //     }
    // }

    /// <summary>
    /// 当前用户正在委托挂单
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
        UsersApi? api = this.service_user.GetApi(Request.Headers["api_key"]);
        return this.service_order.GetOrder(true, api!.user_id, skip, take, start, end);
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
        UsersApi? api = this.service_user.GetApi(Request.Headers["api_key"]);
        return this.service_order.GetOrder(false, api!.user_id, skip, take, start, end);
    }

    /// <summary>
    /// 按订单id查询
    /// </summary>
    /// <param name="market"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("GetOrderById")]
    [ResponseCache(CacheProfileName = "cache_0")]
    public Res<List<ResOrder>> GetOrderById(long market, List<long> data)
    {
        UsersApi? api = this.service_user.GetApi(Request.Headers["api_key"]);
        return this.service_order.GetOrder(market: market, uid: api!.user_id, ids: data);
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
    [ResponseCache(CacheProfileName = "cache_0")]
    public Res<List<ResOrder>> GetOrderByState(long market, E_OrderState state, DateTimeOffset start, DateTimeOffset end)
    {
        UsersApi? api = this.service_user.GetApi(Request.Headers["api_key"]);
        return this.service_order.GetOrder(market: market, uid: api!.user_id, state: state, start: start, end: end);
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
        UsersApi? api = this.service_user.GetApi(Request.Headers["api_key"]);
        return this.service_order.GetOrder(market: market, uid: api!.user_id, start: start, end: end);
    }


}