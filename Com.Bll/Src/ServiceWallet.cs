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
                else if (amount_base == 0)
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
    /// 撮合成交后资产变动(批量),手续费内扣(到手资产里面再去扣手续费)
    /// </summary>
    /// <param name="market">市场</param>
    /// <param name="orders">相关订单</param>
    /// <param name="deals">成交记录</param>
    /// <returns></returns>
    public (bool result, List<Running> running) Transaction(Market market, List<Orders> orders, List<Deal> deals)
    {
        List<Running> runnings = new List<Running>();
        if (deals == null || deals.Count == 0 || orders == null || orders.Count == 0)
        {
            return (false, runnings);
        }
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        E_WalletType wallet_type = E_WalletType.main;
                        if (market.market_type == E_MarketType.spot)
                        {
                            wallet_type = E_WalletType.spot;
                        }
                        List<long> user_id = deals.Select(T => T.bid_uid).ToList();
                        user_id.AddRange(deals.Select(T => T.ask_uid).ToList());
                        user_id = user_id.Distinct().ToList();
                        List<Users> users = db.Users.AsNoTracking().Where(P => user_id.Contains(P.user_id)).ToList();
                        List<Vip> vips = db.Vip.AsNoTracking().Where(P => users.Select(P => P.vip).Distinct().Contains(P.id)).ToList();
                        List<Wallet> wallets = db.Wallet.Where(P => P.wallet_type == wallet_type && user_id.Contains(P.user_id) && (P.coin_id == market.coin_id_base || P.coin_id == market.coin_id_quote)).ToList();
                        Wallet? settlement_base = db.Wallet.Where(P => P.wallet_type == E_WalletType.main && P.user_id == market.settlement_uid && P.coin_id == market.coin_id_base).FirstOrDefault();
                        Wallet? settlement_quote = db.Wallet.Where(P => P.wallet_type == E_WalletType.main && P.user_id == market.settlement_uid && P.coin_id == market.coin_id_quote).FirstOrDefault();
                        foreach (var item in deals)
                        {
                            if (settlement_base == null || settlement_quote == null)
                            {
                                FactoryService.instance.constant.logger.LogError($"{market.symbol}:交易对没有找到结算账户");
                                return (false, runnings);
                            }
                            Users? user_buy = users.FirstOrDefault(P => P.user_id == item.bid_uid);
                            Users? user_sell = users.FirstOrDefault(P => P.user_id == item.ask_uid);
                            if (user_buy == null || user_sell == null)
                            {
                                FactoryService.instance.constant.logger.LogError($"{market.symbol}:未找到交易账户");
                                return (false, runnings);
                            }
                            Vip? vip_buy = vips.FirstOrDefault(P => P.id == user_buy.vip);
                            Vip? vip_sell = vips.FirstOrDefault(P => P.id == user_sell.vip);
                            if (vip_buy == null || vip_sell == null)
                            {
                                FactoryService.instance.constant.logger.LogError($"{market.symbol}:用户:{user_buy.user_name}/{user_sell.user_name},未找到交易账户vip等级");
                                return (false, runnings);
                            }
                            Wallet? buy_base = wallets.Where(P => P.coin_id == market.coin_id_base && P.user_id == item.bid_uid).FirstOrDefault();
                            Wallet? buy_quote = wallets.Where(P => P.coin_id == market.coin_id_quote && P.user_id == item.bid_uid).FirstOrDefault();
                            Wallet? sell_base = wallets.Where(P => P.coin_id == market.coin_id_base && P.user_id == item.ask_uid).FirstOrDefault();
                            Wallet? sell_quote = wallets.Where(P => P.coin_id == market.coin_id_quote && P.user_id == item.ask_uid).FirstOrDefault();
                            if (buy_base == null || buy_quote == null || sell_base == null || sell_quote == null)
                            {
                                FactoryService.instance.constant.logger.LogError($"{market.symbol}:用户:{user_buy.user_name}/{user_sell.user_name},未找到交易账户钱包");
                                return (false, runnings);
                            }
                            sell_base.freeze -= item.amount;
                            buy_quote.freeze -= item.total;
                            decimal temp_base = 0;
                            decimal temp_quote = 0;
                            decimal fee_base = 0;
                            decimal fee_quote = 0;
                            if (item.trigger_side == E_OrderSide.buy)
                            {
                                // 买单为吃单,卖单为挂单
                                fee_base = vip_buy.fee_taker * item.amount;
                                fee_quote = vip_sell.fee_maker * item.total;
                                temp_base = (item.amount - fee_base);
                                temp_quote = (item.total - fee_quote);
                            }
                            else if (item.trigger_side == E_OrderSide.sell)
                            {
                                // 卖单为吃单,买单为挂单
                                fee_quote = vip_sell.fee_taker * item.total;
                                fee_base = vip_buy.fee_maker * item.amount;
                                temp_base = (item.amount - fee_base);
                                temp_quote = (item.total - fee_quote);
                            }
                            buy_base.available += temp_base;
                            sell_quote.available += temp_quote;
                            buy_base.total = buy_base.available + buy_base.freeze;
                            buy_quote.total = buy_quote.available + buy_quote.freeze;
                            sell_base.total = sell_base.available + sell_base.freeze;
                            sell_quote.total = sell_quote.available + sell_quote.freeze;
                            runnings.Add(AddRunning(item.trade_id, wallet_type, wallet_type, temp_base, sell_base, buy_base, $"交易:{sell_base.user_name}=>{buy_base.user_name},{temp_base}{sell_base.coin_name}"));
                            runnings.Add(AddRunning(item.trade_id, wallet_type, wallet_type, temp_quote, buy_quote, sell_quote, $"交易:{buy_quote.user_name}=>{sell_quote.user_name},{temp_quote}{buy_quote.coin_name}"));
                            settlement_base.available += fee_base;
                            settlement_quote.available += fee_quote;
                            settlement_base.total = settlement_base.available + settlement_base.freeze;
                            settlement_quote.total = settlement_quote.available + settlement_quote.freeze;
                            runnings.Add(AddRunning(item.trade_id, wallet_type, E_WalletType.main, fee_base, buy_base, settlement_base, $"手续费:{buy_base.user_name}=>结算账户:{settlement_base.user_name},{fee_base}{buy_base.coin_name}"));
                            runnings.Add(AddRunning(item.trade_id, wallet_type, E_WalletType.main, fee_quote, sell_quote, settlement_quote, $"手续费:{sell_quote.user_name}=>结算账户:{settlement_quote.user_name},{fee_quote}{sell_quote.coin_name}"));
                        }
                        List<Orders> order = orders.Where(P => P.state == E_OrderState.completed && P.unsold > 0).Distinct().ToList();
                        foreach (Orders item in order)
                        {
                            if (item.side == E_OrderSide.buy)
                            {
                                Wallet? buy_quote = wallets.Where(P => P.coin_id == market.coin_id_quote && P.user_id == item.uid).FirstOrDefault();
                                if (buy_quote != null)
                                {
                                    buy_quote.freeze -= item.unsold;
                                    buy_quote.available += item.unsold;
                                    item.remarks = $"订单完全成交,解冻多余的冻结资金:{item.unsold}";
                                    item.unsold = 0;
                                }
                            }
                            else if (item.side == E_OrderSide.sell)
                            {
                                Wallet? sell_base = wallets.Where(P => P.coin_id == market.coin_id_base && P.user_id == item.uid).FirstOrDefault();
                                if (sell_base != null)
                                {
                                    sell_base.freeze -= item.unsold;
                                    sell_base.available += item.unsold;
                                    item.remarks = $"订单完全成交,解冻多余的冻结资金:{item.unsold}";
                                    item.unsold = 0;
                                }
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
                        FactoryService.instance.constant.logger.LogError(ex, market.symbol + ":" + ex.Message);
                        return (false, runnings);
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
            operation_uid = 0,
            time = DateTimeOffset.UtcNow,
            remarks = remarks,
        };
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