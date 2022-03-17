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
// [Authorize]
[AllowAnonymous]
public class ServiceController : Controller
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
            return 0;
        }
    }

    /// <summary>
    /// 服务管理
    /// </summary>
    /// <param name="market">交易对</param>
    /// <param name="status">状态 1:清除缓存,2:预热缓存,3:服务启动,4:服务停止</param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> Manage(long market, int status)
    {
        Res<long> res = new Res<long>();
        MarketInfo? marketInfo = FactoryService.instance.constant.db.MarketInfo.FirstOrDefault(P => P.market == market);
        if (marketInfo == null)
        {
            res.success = false;
            res.code = E_Res_Code.fail;
            res.message = "交易对不存在";
            res.data = market;
        }
        else
        {
            bool result = false;
            if (status == 1)
            {
                result = await FactoryAdmin.instance.ServiceClearCache(marketInfo);
            }
            else if (status == 2)
            {
                result = await FactoryAdmin.instance.ServiceWarmCache(marketInfo);
            }
            else if (status == 3)
            {
                result = await FactoryAdmin.instance.ServiceStart(marketInfo);
            }
            else if (status == 4)
            {
                result = await FactoryAdmin.instance.ServiceStop(marketInfo);
            }
            if (result)
            {
                res.success = true;
                res.code = E_Res_Code.ok;
                res.message = "操作成功";
                res.data = market;
            }
        }
        return Json(res);
    }

}
