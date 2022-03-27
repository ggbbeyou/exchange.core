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
    /// 初始化
    /// </summary>
    public ServiceWallet()
    {

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
    /// 撮合成交后资产变动(批量)
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
    public (bool result, List<Running> running) Transaction(E_WalletType wallet_type, Market market, List<Deal> deals)
    {
        List<Running> runnings = new List<Running>();
        if (deals == null || deals.Count == 0)
        {
            return (true, runnings);
        }
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        List<long> user_id = deals.Select(T => T.bid_uid).ToList();
                        user_id.AddRange(deals.Select(T => T.ask_uid).ToList());
                        user_id = user_id.Distinct().ToList();
                        List<Wallet> wallets = db.Wallet.Where(P => P.wallet_type == wallet_type && user_id.Contains(P.user_id) && (P.coin_id == market.coin_id_base || P.coin_id == market.coin_id_quote)).ToList();
                        Wallet? settlement_base = db.Wallet.Where(P => P.wallet_type == E_WalletType.fee && P.user_id == market.settlement_uid && P.coin_id == market.coin_id_base).FirstOrDefault();
                        Wallet? settlement_quote = db.Wallet.Where(P => P.wallet_type == E_WalletType.fee && P.user_id == market.settlement_uid && P.coin_id == market.coin_id_quote).FirstOrDefault();
                        foreach (var item in deals)
                        {
                            Wallet? buy_base = wallets.Where(P => P.coin_id == market.coin_id_base && P.user_id == item.bid_uid).FirstOrDefault();
                            Wallet? buy_quote = wallets.Where(P => P.coin_id == market.coin_id_quote && P.user_id == item.bid_uid).FirstOrDefault();
                            Wallet? sell_base = wallets.Where(P => P.coin_id == market.coin_id_base && P.user_id == item.ask_uid).FirstOrDefault();
                            Wallet? sell_quote = wallets.Where(P => P.coin_id == market.coin_id_quote && P.user_id == item.ask_uid).FirstOrDefault();
                            if (buy_base == null || buy_quote == null || sell_base == null || sell_quote == null)
                            {
                                return (true, runnings);
                            }
                            buy_base.available += item.amount;
                            sell_base.freeze -= (item.amount + item.fee_sell);
                            buy_quote.freeze -= (item.total + item.fee_buy);
                            sell_quote.available += item.total;
                            buy_base.total = buy_base.available + buy_base.freeze;
                            buy_quote.total = buy_quote.available + buy_quote.freeze;
                            sell_base.total = sell_base.available + sell_base.freeze;
                            sell_quote.total = sell_quote.available + sell_quote.freeze;
                            runnings.Add(new Running
                            {
                                id = FactoryService.instance.constant.worker.NextId(),
                                relation_id = item.trade_id,
                                coin_id = market.coin_id_base,
                                coin_name = market.coin_name_base,
                                wallet_from = sell_base.wallet_id,
                                wallet_to = buy_base.wallet_id,
                                wallet_type_from = E_WalletType.main,
                                wallet_type_to = E_WalletType.main,
                                uid_from = sell_base.user_id,
                                uid_to = buy_base.user_id,
                                user_name_from = sell_base.user_name,
                                user_name_to = buy_base.user_name,
                                amount = item.amount,
                                operation_uid = 0,
                                time = item.time,
                                remarks = "卖币成交,基础币种:卖方支付给买方",
                            });
                            runnings.Add(new Running
                            {
                                id = FactoryService.instance.constant.worker.NextId(),
                                relation_id = item.trade_id,
                                coin_id = market.coin_id_quote,
                                coin_name = market.coin_name_quote,
                                wallet_from = buy_quote.wallet_id,
                                wallet_to = sell_quote.wallet_id,
                                wallet_type_from = E_WalletType.main,
                                wallet_type_to = E_WalletType.main,
                                uid_from = buy_quote.user_id,
                                uid_to = sell_quote.user_id,
                                user_name_from = buy_quote.user_name,
                                user_name_to = sell_quote.user_name,
                                amount = item.total,
                                operation_uid = 0,
                                time = item.time,
                                remarks = "买币成交,报价币种:买方支付给卖方",
                            });
                            if (settlement_base != null && item.fee_sell > 0)
                            {
                                settlement_base.available += item.fee_sell;
                                settlement_base.total = settlement_base.available + settlement_base.freeze;
                                runnings.Add(new Running
                                {
                                    id = FactoryService.instance.constant.worker.NextId(),
                                    relation_id = item.trade_id,
                                    coin_id = market.coin_id_base,
                                    coin_name = market.coin_name_base,
                                    wallet_from = sell_base.wallet_id,
                                    wallet_to = settlement_base.wallet_id,
                                    wallet_type_from = E_WalletType.main,
                                    wallet_type_to = E_WalletType.fee,
                                    uid_from = sell_base.user_id,
                                    uid_to = settlement_base.user_id,
                                    user_name_from = sell_base.user_name,
                                    user_name_to = settlement_base.user_name,
                                    amount = item.fee_sell,
                                    operation_uid = settlement_base.user_id,
                                    time = item.time,
                                    remarks = "卖币手续费",
                                });
                            }
                            if (settlement_quote != null && item.fee_buy > 0)
                            {
                                settlement_quote.available += item.fee_buy;
                                settlement_quote.total = settlement_quote.available + settlement_quote.freeze;
                                runnings.Add(new Running
                                {
                                    id = FactoryService.instance.constant.worker.NextId(),
                                    relation_id = item.trade_id,
                                    coin_id = market.coin_id_quote,
                                    coin_name = market.coin_name_quote,
                                    wallet_from = buy_quote.wallet_id,
                                    wallet_to = settlement_quote.wallet_id,
                                    wallet_type_from = E_WalletType.main,
                                    wallet_type_to = E_WalletType.fee,
                                    uid_from = buy_quote.user_id,
                                    uid_to = settlement_quote.user_id,
                                    user_name_from = buy_quote.user_name,
                                    user_name_to = settlement_quote.user_name,
                                    amount = item.fee_buy,
                                    operation_uid = settlement_quote.user_id,
                                    time = item.time,
                                    remarks = "买币手续费",
                                });
                            }
                        }
                        db.SaveChanges();
                        transaction.Commit();
                        return (true, runnings);
                    }
                    catch (Exception ex)
                    {
                        runnings.Clear();
                        transaction.Rollback();
                        FactoryService.instance.constant.logger.LogError(ex, ex.Message);
                        return (false, runnings);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 添加资金流水
    /// </summary>
    /// <param name="runnings"></param>
    public void AddRunning(List<Running> runnings)
    {
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                db.Running.AddRange(runnings);
                db.SaveChanges();
            }
        }
    }




}