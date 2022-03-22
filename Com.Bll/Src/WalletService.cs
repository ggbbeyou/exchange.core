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

    /// <summary>
    /// 可用余额转账
    /// </summary>
    /// <param name="coin_id">币ID</param>
    /// <param name="from">来源:用户id</param>
    /// <param name="to">目的:用户id</param>
    /// <param name="amount">数量</param>
    /// <returns></returns>
    public bool TransferAvailable(long coin_id, long from, long to, decimal amount)
    {
        if (amount == 0)
        {
            return false;
        }
        using (var transaction = this.db.Database.BeginTransaction())
        {
            try
            {
                Wallet? wallet_from = this.db.Wallet.Where(x => x.coin_id == coin_id && x.user_id == from).SingleOrDefault();
                Wallet? wallet_to = this.db.Wallet.Where(x => x.coin_id == coin_id && x.user_id == to).SingleOrDefault();
                if (wallet_from == null || wallet_to == null)
                {
                    return false;
                }
                if (amount > 0 && wallet_from.available < amount)
                {
                    return false;
                }
                else if (amount < 0 && wallet_to.available < Math.Abs(amount))
                {
                    return false;
                }
                wallet_from.available -= amount;
                wallet_from.total = wallet_from.available + wallet_from.freeze;
                wallet_to.available += amount;
                wallet_to.total = wallet_to.available + wallet_to.freeze;
                this.db.SaveChanges();
                transaction.Commit();
                return true;
            }
            catch (System.Exception ex)
            {
                transaction.Rollback();
                FactoryService.instance.constant.logger.LogError(ex, ex.Message);
                return false;
            }
        }
    }







}