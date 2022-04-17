using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Com.Api.Sdk.Enum;
using Com.Api.Sdk.Models;
using Com.Bll;
using Com.Db;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Com.Api.Controllers;

/// <summary>
/// 钱包接口
/// </summary>
[Route("[controller]")]
[Authorize]
[ApiController]
public class WalletController : ControllerBase
{
    /// <summary>
    /// 日志
    /// </summary>
    private readonly ILogger<WalletController> logger;
    /// <summary>
    /// 登录信息
    /// </summary>
    private (long user_id, long no, string user_name, E_App app, string public_key) login
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
    /// Service:钱包
    /// </summary>
    private ServiceWallet service_wallet = new ServiceWallet();

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="logger"></param>
    public WalletController(ILogger<WalletController> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// 划转(同一账户同一币种不同钱包类型划转资金)
    /// </summary>
    /// <param name="coin_id">币id</param>
    /// <param name="from">支付钱包类型</param>
    /// <param name="to">接收钱包类型</param>
    /// <param name="amount">金额</param>
    /// <returns></returns>
    [HttpPost]
    [Route("Transfer")]
    public Res<bool> Transfer(long coin_id, E_WalletType from, E_WalletType to, decimal amount)
    {
        return service_wallet.Transfer(login.user_id, coin_id, from, to, amount);
    }

    /// <summary>
    /// 获取用户所有钱包
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("Transfer")]
    public List<Wallet> GetWallet()
    {
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                return db.Wallet.Where(P => P.user_id == login.user_id).AsNoTracking().ToList();
            }
        }
    }

}