using System.Linq.Expressions;
using Com.Db;
using Com.Db.Enum;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Com.Bll;

/// <summary>
/// Service:钱包
/// </summary>
public class WalletService
{
    /// <summary>
    /// 数据库
    /// </summary>
    public DbContextEF db = null!;

    /// <summary>
    /// 初始化
    /// </summary>
    public WalletService()
    {
        var scope = FactoryService.instance.constant.provider.CreateScope();
        this.db = scope.ServiceProvider.GetService<DbContextEF>()!;
    }

    /// <summary>
    /// 资产冻结变更
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="coin_id"></param>
    /// <param name="freeze">正数:增加冻结,负数:减少冻结</param>
    /// <returns></returns>
    public bool FreezeChange(long uid, long coin_id, decimal freeze)
    {
        Wallet? wallet = this.db.Wallet.Where(x => x.user_id == uid && x.coin_id == coin_id).SingleOrDefault();
        if (wallet == null)
        {
            return false;
        }
        if (freeze > 0)
        {
            if (wallet.available < freeze)
            {
                return false;
            }
        }
        else if (freeze < 0)
        {
            if (wallet.freeze < Math.Abs(freeze))
            {
                return false;
            }
        }
        else
        {
            return false;
        }
        wallet.freeze += freeze;
        wallet.available -= freeze;
        return this.db.SaveChanges() > 0;
    }





}