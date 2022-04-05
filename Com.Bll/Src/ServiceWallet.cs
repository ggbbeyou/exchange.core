/*  
 * ......................我佛慈悲...................... 
 *                       _oo0oo_ 
 *                      o8888888o 
 *                      88" . "88 
 *                      (| -_- |) 
 *                      0\  =  /0 
 *                    ___/`---'\___ 
 *                  .' \\|     |// '. 
 *                 / \\|||  :  |||// \ 
 *                / _||||| -卍-|||||- \ 
 *               |   | \\\  -  /// |   | 
 *               | \_|  ''\---/''  |_/ | 
 *               \  .-\__  '-'  ___/-. / 
 *             ___'. .'  /--.--\  `. .'___ 
 *          ."" '<  `.___\_<|>_/___.' >' "". 
 *         | | :  `- \`.;`\ _ /`;.`/ - ` : | | 
 *         \  \ `_.   \_ __\ /__ _/   .-` /  / 
 *     =====`-.____`.___ \_____/___.-`___.-'===== 
 *                       `=---=' 
 *                        
 *..................佛祖开光 ,永无BUG................... 
 *  
 */

using System.Linq.Expressions;
using Com.Db;
using Com.Api.Sdk.Enum;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Data;

namespace Com.Bll;

/// <summary>
/// Service:计账钱包
/// </summary>
public class ServiceWallet
{

    private IsolationLevel isolationLevel = IsolationLevel.ReadCommitted;

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
    /// <param name="coin_id">币种</param>
    /// <param name="amount">正数:增加冻结,负数:减少冻结</param>
    /// <returns></returns>
    public bool FreezeChange(E_WalletType wallet_type, long uid, long coin_id, decimal amount)
    {
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                using (var transaction = db.Database.BeginTransaction(isolationLevel))
                {
                    try
                    {
                        Wallet? wallet = db.Wallet.Where(P => P.wallet_type == wallet_type && P.user_id == uid && P.coin_id == coin_id).SingleOrDefault();
                        if (wallet == null)
                        {
                            return false;
                        }
                        if (amount > 0)
                        {
                            if (wallet.available < amount)
                            {
                                return false;
                            }
                        }
                        else if (amount < 0)
                        {
                            if (wallet.freeze < Math.Abs(amount))
                            {
                                return false;
                            }
                        }
                        else if (amount == 0)
                        {
                            return false;
                        }
                        wallet.freeze += amount;
                        wallet.available -= amount;
                        db.SaveChanges();
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        FactoryService.instance.constant.logger.LogError(ex, "FreezeChange:" + ex.Message);
                        return false;
                    }
                }
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
                using (var transaction = db.Database.BeginTransaction(isolationLevel))
                {
                    try
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
                        else if (amount_base == 0)
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
                        else if (amount_base == 0)
                        {
                            return false;
                        }
                        wallet_base.freeze += amount_base;
                        wallet_base.available -= amount_base;
                        wallet_quote.freeze += amount_quote;
                        wallet_quote.available -= amount_quote;
                        db.SaveChanges();
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        FactoryService.instance.constant.logger.LogError(ex, "FreezeChange:" + ex.Message);
                        return false;
                    }
                }
            }
        }
    }


    /// <summary>
    /// 撮合成交后资产变动(批量),手续费内扣(到手资产里面再去扣手续费)
    /// </summary>
    /// <param name="market">市场</param>
    /// <param name="deals">成交记录</param>
    /// <returns></returns>
    public (bool result, List<Running> running) Transaction(Market market, List<Deal> deals)
    {
        List<Running> runnings = new List<Running>();
        if (deals == null || deals.Count == 0)
        {
            return (false, runnings);
        }
        decimal temp_base = 0;
        decimal temp_quote = 0;
        decimal fee_base = 0;
        decimal fee_quote = 0;
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {

                E_WalletType wallet_type = E_WalletType.main;
                if (market.market_type == E_MarketType.spot)
                {
                    wallet_type = E_WalletType.spot;
                }
                List<long> user_id = deals.Select(T => T.bid_uid).ToList();
                user_id.AddRange(deals.Select(T => T.ask_uid).ToList());
                user_id = user_id.Distinct().ToList();
                using (var transaction = db.Database.BeginTransaction(isolationLevel))
                {
                    try
                    {
                        List<Wallet> wallets = db.Wallet.Where(P => P.wallet_type == wallet_type && user_id.Contains(P.user_id) && (P.coin_id == market.coin_id_base || P.coin_id == market.coin_id_quote)).ToList();
                        List<Wallet> wallets_settlement = db.Wallet.Where(P => P.wallet_type == E_WalletType.main && P.user_id == market.settlement_uid && (P.coin_id == market.coin_id_base || P.coin_id == market.coin_id_quote)).ToList();
                        Wallet? settlement_base = wallets_settlement.Where(P => P.coin_id == market.coin_id_base).SingleOrDefault();
                        Wallet? settlement_quote = wallets_settlement.Where(P => P.coin_id == market.coin_id_quote).SingleOrDefault();
                        if (settlement_base == null || settlement_quote == null)
                        {
                            FactoryService.instance.constant.logger.LogError($"{market.symbol}:交易对没有找到结算账户");
                            return (false, runnings);
                        }
                        foreach (Deal item in deals)
                        {
                            Wallet? buy_base = wallets.Where(P => P.coin_id == market.coin_id_base && P.user_id == item.bid_uid).SingleOrDefault();
                            Wallet? buy_quote = wallets.Where(P => P.coin_id == market.coin_id_quote && P.user_id == item.bid_uid).SingleOrDefault();
                            Wallet? sell_base = wallets.Where(P => P.coin_id == market.coin_id_base && P.user_id == item.ask_uid).SingleOrDefault();
                            Wallet? sell_quote = wallets.Where(P => P.coin_id == market.coin_id_quote && P.user_id == item.ask_uid).SingleOrDefault();
                            if (buy_base == null || buy_quote == null || sell_base == null || sell_quote == null)
                            {
                                FactoryService.instance.constant.logger.LogError($"{market.symbol}:用户:{item.bid_uid}/{item.ask_uid},未找到交易账户钱包");
                                return (false, runnings);
                            }
                            temp_base = 0;
                            temp_quote = 0;
                            fee_base = 0;
                            fee_quote = 0;
                            sell_base.freeze -= item.amount;
                            buy_quote.freeze -= item.total;
                            if (item.trigger_side == E_OrderSide.buy)
                            {
                                // 买单为吃单,卖单为挂单
                                fee_base = item.fee_bid_taker * item.amount;
                                fee_quote = item.fee_ask_maker * item.total;
                                temp_base = (item.amount - fee_base);
                                temp_quote = (item.total - fee_quote);
                            }
                            else if (item.trigger_side == E_OrderSide.sell)
                            {
                                // 卖单为吃单,买单为挂单
                                fee_quote = item.fee_ask_taker * item.total;
                                fee_base = item.fee_bid_maker * item.amount;
                                temp_base = (item.amount - fee_base);
                                temp_quote = (item.total - fee_quote);
                            }
                            buy_base.available += temp_base;
                            sell_quote.available += temp_quote;
                            settlement_base.available += fee_base;
                            settlement_quote.available += fee_quote;
                            runnings.Add(AddRunning(item.trade_id, wallet_type, wallet_type, temp_base, sell_base, buy_base, $"交易:{sell_base.user_name}=>{buy_base.user_name},{(double)temp_base}{sell_base.coin_name}"));
                            runnings.Add(AddRunning(item.trade_id, wallet_type, wallet_type, temp_quote, buy_quote, sell_quote, $"交易:{buy_quote.user_name}=>{sell_quote.user_name},{(double)temp_quote}{buy_quote.coin_name}"));
                            runnings.Add(AddRunning(item.trade_id, wallet_type, E_WalletType.main, fee_base, buy_base, settlement_base, $"手续费:{buy_base.user_name}=>结算账户:{settlement_base.user_name},{(double)fee_base}{buy_base.coin_name}"));
                            runnings.Add(AddRunning(item.trade_id, wallet_type, E_WalletType.main, fee_quote, sell_quote, settlement_quote, $"手续费:{sell_quote.user_name}=>结算账户:{settlement_quote.user_name},{(double)fee_quote}{sell_quote.coin_name}"));
                        }
                        foreach (var item in wallets)
                        {
                            //如果放在上面foreach里面,会非常非常耗时,特别奇怪.怪事
                            item.total = item.available + item.freeze;
                        }
                        foreach (var item in wallets_settlement)
                        {
                            item.total = item.available + item.freeze;
                        }
                        db.Wallet.UpdateRange(wallets);
                        db.Wallet.UpdateRange(wallets_settlement);
                        int savecount = db.SaveChanges();
                        transaction.Commit();
                        return (savecount > 0, runnings);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        FactoryService.instance.constant.logger.LogError(ex, market.symbol + ":Transaction," + ex.Message);
                        runnings.Clear();
                        return (false, runnings);
                    }
                }
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
                using (var transaction = db.Database.BeginTransaction(isolationLevel))
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
                        FactoryService.instance.constant.logger.LogError(ex, "TansferAvailable" + ex.Message);
                        return false;
                    }
                }
            }
        }
    }


    /// <summary>
    /// 添加钱包流水
    /// </summary>
    /// <param name="relation_id">关联id</param>
    /// <param name="wallet_type_to">到账钱包类型</param>
    /// <param name="amount">量</param>
    /// <param name="wallet_from">来源钱包</param>
    /// <param name="wallet_to">到账钱包</param>
    /// <param name="remarks">备注</param>
    /// <returns></returns>
    public Running AddRunning(long relation_id, E_WalletType wallet_type_from, E_WalletType wallet_type_to, decimal amount, Wallet wallet_from, Wallet wallet_to, string remarks)
    {
        return new Running
        {
            id = FactoryService.instance.constant.worker.NextId(),
            relation_id = relation_id,
            coin_id = wallet_from.coin_id,
            coin_name = wallet_from.coin_name,
            wallet_from = wallet_from.wallet_id,
            wallet_to = wallet_to.wallet_id,
            wallet_type_from = wallet_type_from,
            wallet_type_to = wallet_type_to,
            uid_from = wallet_from.user_id,
            uid_to = wallet_to.user_id,
            user_name_from = wallet_from.user_name,
            user_name_to = wallet_to.user_name,
            amount = amount,
            operation_uid = wallet_from.user_id,
            time = DateTimeOffset.UtcNow,
            remarks = remarks,
        };
    }

    /// <summary>
    /// 添加资金流水
    /// </summary>
    /// <param name="runnings"></param>
    public bool AddRunning(List<Running> runnings)
    {
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                db.Running.AddRange(runnings);
                return db.SaveChanges() > 0;
            }
        }
    }


}