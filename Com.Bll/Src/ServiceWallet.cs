using System.Linq.Expressions;
using Com.Db;
using Com.Api.Sdk.Enum;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Com.Bll;

/// <summary>
/// Service:计账钱包
/// </summary>
public class ServiceWallet
{
    /// <summary>
    /// 数据库
    /// </summary>
    public DbContextEF db = null!;

    /// <summary>
    /// 初始化
    /// </summary>
    public ServiceWallet()
    {
        var scope = FactoryService.instance.constant.provider.CreateScope();
        this.db = scope.ServiceProvider.GetService<DbContextEF>()!;
    }
    /// <summary>
    /// 资产冻结变更
    /// </summary>
    /// <param name="wallet_type">钱包类型</param>
    /// <param name="uid">用户</param>
    /// <param name="coin_base">币种</param>
    /// <param name="amount_base">正数:增加冻结,负数:减少冻结</param>
    /// <returns></returns>
    public bool FreezeChange(E_WalletType wallet_type, long uid, long coin_base, decimal amount_base)
    {
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                Wallet? wallet_base = db.Wallet.Where(P => P.wallet_type == wallet_type && P.user_id == uid && P.coin_id == coin_base).SingleOrDefault();
                if (wallet_base == null)
                {
                    return false;
                }
                if (amount_base > 0)
                {
                    if (wallet_base.available < amount_base)
                    {
                        return false;
                    }
                }
                else if (amount_base < 0)
                {
                    if (wallet_base.freeze < Math.Abs(amount_base))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
                wallet_base.freeze += amount_base;
                wallet_base.available -= amount_base;
                return db.SaveChanges() > 0;
            }
        }
    }


    /// <summary>
    /// 资产冻结变更
    /// </summary>
    /// <param name="wallet_type">钱包类型</param>
    /// <param name="uid"></param>
    /// <param name="coin_base"></param>
    /// <param name="amount_base">正数:增加冻结,负数:减少冻结</param>
    /// <returns></returns>
    public bool FreezeChange(E_WalletType wallet_type, long uid, long coin_base, decimal amount_base, long coin_quote, decimal amount_quote)
    {
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                Wallet? wallet_base = db.Wallet.Where(P => P.wallet_type == wallet_type && P.user_id == uid && P.coin_id == coin_base).SingleOrDefault();
                if (wallet_base == null)
                {
                    return false;
                }
                if (amount_base > 0)
                {
                    if (wallet_base.available < amount_base)
                    {
                        return false;
                    }
                }
                else if (amount_base < 0)
                {
                    if (wallet_base.freeze < Math.Abs(amount_base))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
                Wallet? wallet_quote = db.Wallet.Where(P => P.wallet_type == wallet_type && P.user_id == uid && P.coin_id == coin_quote).SingleOrDefault();
                if (wallet_quote == null)
                {
                    return false;
                }
                if (amount_quote > 0)
                {
                    if (wallet_quote.available < amount_quote)
                    {
                        return false;
                    }
                }
                else if (amount_quote < 0)
                {
                    if (wallet_quote.freeze < Math.Abs(amount_quote))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
                wallet_base.freeze += amount_base;
                wallet_base.available -= amount_base;
                wallet_quote.freeze += amount_quote;
                wallet_quote.available -= amount_quote;
                return db.SaveChanges() > 0;
            }
        }
    }

    /// <summary>
    /// 可用余额转账
    /// </summary>
    /// <param name="wallet_type">钱包类型</param>
    /// <param name="coin_id">币ID</param>
    /// <param name="from">来源:用户id</param>
    /// <param name="to">目的:用户id</param>
    /// <param name="amount">数量</param>
    /// <returns></returns>
    public bool TransferAvailable(E_WalletType wallet_type, long coin_id, long from, long to, decimal amount)
    {
        if (amount == 0)
        {
            return false;
        }
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        Wallet? wallet_from = db.Wallet.Where(P => P.wallet_type == wallet_type && P.coin_id == coin_id && P.user_id == from).SingleOrDefault();
                        Wallet? wallet_to = db.Wallet.Where(P => P.wallet_type == wallet_type && P.coin_id == coin_id && P.user_id == to).SingleOrDefault();
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
                        db.SaveChanges();
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        FactoryService.instance.constant.logger.LogError(ex, ex.Message);
                        return false;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 撮合成交后资产变动
    /// </summary>
    /// <param name="wallet_type">钱包类型</param>
    /// <param name="coin_id_base">基础币种id</param>
    /// <param name="coin_id_quote">报价币种id</param>
    /// <param name="buy_uid">买用户</param>
    /// <param name="sell_uid">卖用户</param>
    /// <param name="rate_buy">买手续费</param>
    /// <param name="rate_sell">卖手续费</param>
    /// <param name="amount">成交量</param>
    /// <param name="price">成交价</param>
    /// <returns>是否成功</returns>
    public bool Transaction(E_WalletType wallet_type, long coin_id_base, long coin_id_quote, long buy_uid, long sell_uid, decimal rate_buy, decimal rate_sell, decimal amount, decimal price)
    {
        if (amount == 0 || price == 0)
        {
            return false;
        }
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        Wallet? buy_base = db.Wallet.Where(P => P.wallet_type == wallet_type && P.coin_id == coin_id_base && P.user_id == buy_uid).SingleOrDefault();
                        Wallet? buy_quote = db.Wallet.Where(P => P.wallet_type == wallet_type && P.coin_id == coin_id_quote && P.user_id == buy_uid).SingleOrDefault();
                        Wallet? sell_base = db.Wallet.Where(P => P.wallet_type == wallet_type && P.coin_id == coin_id_base && P.user_id == sell_uid).SingleOrDefault();
                        Wallet? sell_quote = db.Wallet.Where(P => P.wallet_type == wallet_type && P.coin_id == coin_id_quote && P.user_id == sell_uid).SingleOrDefault();
                        if (buy_base == null || buy_quote == null || sell_base == null || sell_quote == null)
                        {
                            return false;
                        }
                        decimal quote_total = amount * price;
                        decimal buy_fee = quote_total * rate_buy;
                        decimal sell_fee = quote_total * rate_sell;
                        buy_base.available += amount;
                        sell_base.freeze -= amount;
                        buy_quote.freeze -= quote_total;
                        sell_quote.available += quote_total;
                        buy_quote.freeze -= buy_fee;
                        sell_quote.freeze -= sell_fee;
                        buy_base.total = buy_base.available + buy_base.freeze;
                        buy_quote.total = buy_quote.available + buy_quote.freeze;
                        sell_base.total = sell_base.available + sell_base.freeze;
                        sell_quote.total = sell_quote.available + sell_quote.freeze;
                        db.SaveChanges();
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        FactoryService.instance.constant.logger.LogError(ex, ex.Message);
                        return false;
                    }
                }
            }
        }
    }





}