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

/// <summary>
/// 撮合服务
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("[controller]")]
public class ServiceController : ControllerBase
{
    /// <summary>
    /// 数据库
    /// </summary>
    public DbContextEF db = null!;
    /// <summary>
    /// service:公共服务
    /// </summary>
    private ServiceCommon service_common = new ServiceCommon();
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
    /// 用户服务
    /// </summary>
    /// <returns></returns>
    private ServiceUser service_user = new ServiceUser();

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="db">db上下文</param>
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
    [Route("Manage")]
    public async Task<Res<bool>> Manage(long market, int status)
    {
        Res<bool> res = new Res<bool>();
        Market? marketInfo = this.db.Market.FirstOrDefault(P => P.market == market);
        if (marketInfo == null)
        {
            res.code = E_Res_Code.symbol_not_found;
            res.msg = "交易对不存在";
            res.data = false;
        }
        else
        {
            bool? rsult = null;
            if (status == 0)
            {
                rsult = await FactoryAdmin.instance.ServiceGetStatus(marketInfo) ?? marketInfo.status;
            }
            else if (status == 1)
            {
                rsult = await FactoryAdmin.instance.ServiceStart(marketInfo) ?? marketInfo.status;
            }
            else if (status == 2)
            {
                rsult = await FactoryAdmin.instance.ServiceStop(marketInfo) ?? marketInfo.status;
            }
            if (rsult == null)
            {
                res.code = E_Res_Code.network_error;
                res.data = false;
                return res;
            }
            else
            {
                res.code = E_Res_Code.ok;
                res.data = rsult.Value;
                marketInfo.status = rsult.Value;
            }
            this.db.SaveChanges();
        }
        return res;
    }

}