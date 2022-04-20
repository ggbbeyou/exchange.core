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
    /// 批量挂单
    /// </summary>
    /// <param name="data"></param>
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
            result.message = "用户禁止下单";
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

}