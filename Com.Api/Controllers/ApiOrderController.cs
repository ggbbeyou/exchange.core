using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Com.Api.Sdk.Enum;
using Com.Api.Sdk.Models;
using Com.Api.Src;
using Com.Bll;
using Com.Db;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    public ApiOrderController(ILogger<ApiOrderController> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// 挂单
    /// </summary>
    /// <param name="api_key">交易对</param>
    /// <param name="symbol">交易对</param>
    /// <param name="orders">订单数据</param>
    /// <returns></returns>
    [HttpPost]
    [Route("OrderPlace")]
    public Res<List<ResOrder>> OrderPlace([FromBody] string symbol, [FromBody] List<ReqOrder> orders)
    {
        //判断用户api是否有交易权限
        if (!service_user.ApiUserTransaction(Request.Headers["api_key"]))
        {
            Res<List<ResOrder>> result = new Res<List<ResOrder>>();
            result.code = E_Res_Code.user_prohibit_place_order;
            result.message = "用户禁止下单";
            return result;
        }
        return service_order.PlaceOrder(symbol, login.user_id, login.user_name, orders);
    }

    /// <summary>
    /// 撤单
    /// </summary>
    /// <param name="api_key">交易对</param>
    /// <param name="symbol">交易对</param>
    /// <param name="type">2:按交易对和用户全部撤单,3:按用户和订单id撤单,4:按用户和用户订单id撤单</param>
    /// <param name="data"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("OrderCancel")]
    public Res<bool> OrderCancel([FromHeader] string api_key, string symbol, int type, List<long> data)
    {
        if (!service_user.ApiUserTransaction(api_key))
        {
            Res<bool> result = new Res<bool>();
            result.code = E_Res_Code.user_prohibit_place_order;
            result.message = "用户禁止撤单";
            return result;
        }
        return this.service_order.CancelOrder(symbol, login.user_id, type, data);
    }

}