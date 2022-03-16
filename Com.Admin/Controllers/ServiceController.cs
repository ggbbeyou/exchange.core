using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

using Com.Bll;
using Com.Db;
using Com.Db.Enum;
using Com.Db.Model;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Com.Api.Controllers;

// [Route("api/[controller]/[action]")]
[Authorize]
public class ServiceController : Controller
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

    // /// <summary>
    // /// 
    // /// </summary>
    // /// <param name="configuration"></param>
    // /// <param name="environment"></param>
    // /// <param name="provider"></param>
    // /// <param name="logger"></param>
    // public ServiceController(IServiceProvider provider, IConfiguration configuration, IHostEnvironment environment, ILogger<OrderController> logger)
    // {
    //     this.constant = new FactoryConstant(provider, configuration, environment, logger);
    // }

    /// <summary>
    /// 服务管理
    /// </summary>
    /// <param name="market">交易对</param>
    /// <param name="status">状态 1:清除缓存,2:预热缓存,3:服务启动,4:服务停止</param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> Manage(long market, int status)
    {
        MarketInfo? marketInfo = FactoryAdmin.instance.constant.db.MarketInfo.FirstOrDefault(P => P.market == market);
        if (marketInfo == null)
        {
            return Json(new { code = 1, msg = "交易对不存在" });
        }
        if (status == 1)
        {
            await FactoryAdmin.instance.ServiceClearCache(marketInfo);
        }
        else if (status == 2)
        {
            await FactoryAdmin.instance.ServiceWarmCache(marketInfo);
        }
        else if (status == 3)
        {
            await FactoryAdmin.instance.ServiceStart(marketInfo);
        }
        else if (status == 4)
        {
            await FactoryAdmin.instance.ServiceStop(marketInfo);
        }
        return Json("");
    }

}
