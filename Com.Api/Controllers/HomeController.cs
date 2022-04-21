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
[ApiController]
public class HomeController : ControllerBase
{
    /// <summary>
    /// 日志
    /// </summary>
    private readonly ILogger<HomeController> logger;
    /// <summary>
    /// 配置接口
    /// </summary>
    private readonly IConfiguration config;
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
    /// <param name="config"></param>
    /// <param name="logger"></param>
    public HomeController(IConfiguration config, ILogger<HomeController> logger)
    {
        this.logger = logger;
        this.config = config;
    }

    /// <summary>
    /// 获取基本信息
    /// </summary>
    /// <param name="site">站点</param>
    /// <returns></returns>
    [HttpGet]
    [Route("GetBaseInfo")]
    [ResponseCache(CacheProfileName = "cache_3")]
    public Res<ResBaseInfo> GetBaseInfo(int site = 1)
    {
        Res<ResBaseInfo> res = new Res<ResBaseInfo>();
        res.data = new ResBaseInfo()
        {
            website_name = "模拟交易",
            website_icon = "https://freeware.iconfactory.com/assets/engb/preview.png",
            website_time = DateTimeOffset.UtcNow,
            website_serivcefile = config["minio:endpoint"],
        };
        return res;
    }





}