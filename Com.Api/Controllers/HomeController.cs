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
/// 基础接口
/// </summary>
[Route("[controller]")]
[Authorize]
[ApiController]
public class HomeController : ControllerBase
{
    /// <summary>
    /// 日志
    /// </summary>
    private readonly ILogger<HomeController> logger;
    /// <summary>
    /// 登录信息
    /// </summary>
    private (long no, long user_id, string user_name, E_App app, string public_key) login
    {
        get
        {
            return this.service_user.GetLoginUser(User);
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
    /// Service:成交单
    /// </summary>
    private ServiceDeal service_deal = new ServiceDeal();

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="logger"></param>
    public HomeController(ILogger<HomeController> logger)
    {
        this.logger = logger;
    }

    // /// <summary>
    // /// 当前用户成交记录
    // /// </summary>
    // /// <param name="skip">跳过多少行</param>
    // /// <param name="take">获取多少行</param>
    // /// <param name="start">开始时间</param>
    // /// <param name="end">结束时间</param>
    // /// <returns></returns>
    // [HttpGet]
    // [Route("GetDealByuid")]
    // [ResponseCache(CacheProfileName = "cache_1")]
    // public Res<List<ResDeal>> GetDealByuid(int skip, int take, DateTimeOffset? start = null, DateTimeOffset? end = null)
    // {
    //     return this.service_deal.GetDealByuid(login.user_id, skip, take, start, end);
    // }



}