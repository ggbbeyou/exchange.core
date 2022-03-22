using System.Linq.Expressions;
using Com.Db;
using Com.Db.Enum;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Com.Bll;

/// <summary>
/// Service:计账钱包
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
    /// <param name="wallet_type">钱包类型</param>
    /// <param name="uid"></param>
    /// <param name="coin_id"></param>
    /// <param name="freeze">正数:增加冻结,负数:减少冻结</param>
    /// <returns></returns>
    public bool FreezeChange(E_WalletType wallet_type, long uid, long coin_id, decimal freeze)
    {
        Wallet? wallet = this.db.Wallet.Where(P => P.wallet_type == wallet_type && P.user_id == uid && P.coin_id == coin_id).SingleOrDefault();
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
        using (var transaction = this.db.Database.BeginTransaction())
        {
            try
            {
                Wallet? wallet_from = this.db.Wallet.Where(P => P.wallet_type == wallet_type && P.coin_id == coin_id && P.user_id == from).SingleOrDefault();
                Wallet? wallet_to = this.db.Wallet.Where(P => P.wallet_type == wallet_type && P.coin_id == coin_id && P.user_id == to).SingleOrDefault();
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
            catch (Exception ex)
            {
                transaction.Rollback();
                FactoryService.instance.constant.logger.LogError(ex, ex.Message);
                return false;
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
    /// <param name="settlement_uid">结算用户</param>
    /// <param name="rate_buy">买手续费</param>
    /// <param name="rate_sell">卖手续费</param>
    /// <param name="amount">成交量</param>
    /// <param name="price">成交价</param>
    /// <returns>是否成功</returns>
    public bool Transaction(E_WalletType wallet_type, long coin_id_base, long coin_id_quote, long buy_uid, long sell_uid, long settlement_uid, decimal rate_buy, decimal rate_sell, decimal amount, decimal price)
    {
        if (amount == 0 || price == 0)
        {
            return false;
        }
        using (var transaction = this.db.Database.BeginTransaction())
        {
            try
            {
                Wallet? buy_base = this.db.Wallet.Where(P => P.wallet_type == wallet_type && P.coin_id == coin_id_base && P.user_id == buy_uid).SingleOrDefault();
                Wallet? buy_quote = this.db.Wallet.Where(P => P.wallet_type == wallet_type && P.coin_id == coin_id_quote && P.user_id == buy_uid).SingleOrDefault();
                Wallet? sell_base = this.db.Wallet.Where(P => P.wallet_type == wallet_type && P.coin_id == coin_id_base && P.user_id == sell_uid).SingleOrDefault();
                Wallet? sell_quote = this.db.Wallet.Where(P => P.wallet_type == wallet_type && P.coin_id == coin_id_quote && P.user_id == sell_uid).SingleOrDefault();
                Wallet? settlement_quote = this.db.Wallet.Where(P => P.wallet_type == wallet_type && P.coin_id == coin_id_quote && P.user_id == settlement_uid).SingleOrDefault();
                if (buy_base == null || buy_quote == null || sell_base == null || sell_quote == null || settlement_quote == null)
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
                settlement_quote.available += buy_fee + sell_fee;
                buy_base.total = buy_base.available + buy_base.freeze;
                buy_quote.total = buy_quote.available + buy_quote.freeze;
                sell_base.total = sell_base.available + sell_base.freeze;
                sell_quote.total = sell_quote.available + sell_quote.freeze;
                settlement_quote.total = settlement_quote.available + settlement_quote.freeze;
                this.db.SaveChanges();
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