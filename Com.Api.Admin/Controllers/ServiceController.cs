using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

using Com.Bll;
using Com.Db;
using Com.Api.Sdk.Enum;

using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Com.Api.Sdk.Models;

namespace Com.Api.Admin.Controllers;

// [Route("api/[controller]/[action]")]
// [Authorize]
[AllowAnonymous]
[ApiController]
[Route("[controller]")]
public class ServiceController : ControllerBase
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
    /// 数据库
    /// </summary>
    public DbContextEF db = null!;

    public ServiceController(DbContextEF db)
    {
        this.db = db;
    }

    /// <summary>
    /// 服务管理
    /// </summary>
    /// <param name="market">交易对</param>
    /// <param name="status">状态 0:获取状态,1:服务启动,2:服务停止</param>
    /// <returns></returns>
    [HttpPost]
    public async Task<Res<bool?>> Manage(long market, int status)
    {
        Res<bool?> res = new Res<bool?>();
        Market? marketInfo = this.db.Market.FirstOrDefault(P => P.market == market);
        if (marketInfo == null)
        {
            res.success = false;
            res.code = E_Res_Code.fail;
            res.message = "交易对不存在";
            res.data = null;
        }
        else
        {
            Deal? deal = this.db.Deal.OrderByDescending(P => P.time).FirstOrDefault(P => P.market == market);
            if (deal != null)
            {
                marketInfo.last_price = deal.price;
            }
            if (marketInfo.last_price <= 0)
            {
                res.success = false;
                res.code = E_Res_Code.fail;
                res.message = "最后成交价不能小于0";
                res.data = null;
            }
            else
            {
                if (status == 0)
                {
                    marketInfo.status = await FactoryAdmin.instance.ServiceGetStatus(marketInfo) ?? marketInfo.status;
                }
                else if (status == 1)
                {
                    marketInfo.status = await FactoryAdmin.instance.ServiceStart(marketInfo) ?? marketInfo.status;
                }
                else if (status == 2)
                {
                    marketInfo.status = await FactoryAdmin.instance.ServiceStop(marketInfo) ?? marketInfo.status;
                }
                this.db.SaveChanges();
                res.success = true;
                res.code = E_Res_Code.ok;
                res.message = "操作成功";
                res.data = marketInfo.status;
            }
        }
        return res;
    }

}
