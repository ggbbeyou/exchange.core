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
    /// <param name="wallet_type">钱包类型</param>
    /// <param name="coin_name">币名</param>
    /// <returns></returns>
    [HttpGet]
    [Route("GetWallet")]
    public Res<List<Wallet>?> GetWallet(E_WalletType wallet_type, string? coin_name)
    {
        Res<List<Wallet>?> res = new Res<List<Wallet>?>();
        res.success = false;
        res.code = E_Res_Code.fail;
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                var linq = from coin in db.Coin
                           join wallet in db.Wallet.Where(P => P.user_id == this.login.user_id && P.wallet_type == wallet_type).WhereIf(!string.IsNullOrWhiteSpace(coin_name), P => P.coin_name.Contains(coin_name!))
                           on coin.coin_id equals wallet.coin_id into temp
                           from bb in temp.DefaultIfEmpty()
                           select new Wallet
                           {
                               wallet_id = bb == null ? FactoryService.instance.constant.worker.NextId() : bb.user_id,
                               wallet_type = wallet_type,
                               user_id = this.login.user_id,
                               user_name = this.login.user_name,
                               coin_id = coin.coin_id,
                               coin_name = coin.coin_name,
                               total = bb == null ? 0 : bb.total,
                               available = bb == null ? 0 : bb.available,
                               freeze = bb == null ? 0 : bb.freeze,
                           };
                res.data = linq.ToList();
            }
        }
        return res;
    }

    /// <summary>
    /// 获取手续费
    /// </summary>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    /// <param name="skip">跳过行数</param>
    /// <param name="take">提取行数</param>
    /// <returns></returns>
    [HttpGet]
    [Route("GetRunning")]
    public Res<List<ResRunning>> GetRunning(DateTimeOffset? start, DateTimeOffset? end, int skip, int take)
    {
        Res<List<ResRunning>> res = new Res<List<ResRunning>>();
        res.success = false;
        res.code = E_Res_Code.fail;
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                res.data = db.Running.AsNoTracking().Where(P => P.uid_from == this.login.user_id && P.type == E_RunningType.fee).WhereIf(start != null, P => P.time >= start).WhereIf(end != null, P => P.time <= end).Skip(skip).Take(take).ToList().ConvertAll(P => (ResRunning)P);
            }
        }
        return res;
    }

}