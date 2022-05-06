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




using System.Diagnostics;
using System.Text;
using Com.Bll;
using Com.Db;
using Com.Api.Sdk.Enum;

using Com.Service.Match;
using Com.Service.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;
using Com.Api.Sdk.Models;
using Com.Bll.Util;

namespace Com.Service;

/// <summary>
/// Service:核心服务
/// </summary>
public class Core
{
    /// <summary>
    /// 撮合服务对象
    /// </summary>
    /// <value></value>
    public MatchModel model { get; set; } = null!;

    /// <summary>
    /// 交易记录Db操作
    /// </summary>
    /// <returns></returns>
    public ServiceDeal service_deal = new ServiceDeal();
    /// <summary>
    /// 订单服务
    /// </summary>
    /// <returns></returns>
    public ServiceOrder service_order = new ServiceOrder();
    /// <summary>
    /// K线服务
    /// </summary>
    /// <returns></returns>
    public ServiceKline service_kline = new ServiceKline();
    /// <summary>
    /// 钱包服务
    /// </summary>
    /// <returns></returns>
    private ServiceWallet service_wallet = new ServiceWallet();
    /// <summary>
    /// 秒表
    /// </summary>
    /// <returns></returns>
    public Stopwatch stopwatch = new Stopwatch();
    /// <summary>
    /// 临时变量
    /// </summary>
    /// <returns></returns>
    private ResWebsocker<List<ResDeal>> res_deal = new ResWebsocker<List<ResDeal>>();
    /// <summary>
    /// 临时变量
    /// </summary>
    /// <typeparam name="Kline?"></typeparam>
    /// <returns></returns>
    private ResWebsocker<List<Kline>> res_kline = new ResWebsocker<List<Kline>>();
    /// <summary>
    /// 临时变量
    /// </summary>
    /// <typeparam name="Kline?"></typeparam>
    /// <returns></returns>
    private ResWebsocker<List<Orders>> res_order = new ResWebsocker<List<Orders>>();

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="model"></param>
    public Core(MatchModel model)
    {
        this.model = model;
        res_order.success = true;
        res_order.op = E_WebsockerOp.subscribe_event;
        res_order.channel = E_WebsockerChannel.orders;
        res_deal.success = true;
        res_deal.op = E_WebsockerOp.subscribe_event;
        res_deal.channel = E_WebsockerChannel.trades;
        res_kline.success = true;
        res_kline.op = E_WebsockerOp.subscribe_event;
        res_kline.data = new List<Kline>();
    }

    /// <summary>
    /// 接收撮合传过来的成交订单
    /// </summary>
    /// <param name="queue_name">列队名称</param>
    /// <param name="consume_tag">消费器标签</param>
    /// <returns></returns>
    public (string queue_name, string consume_tag) ReceiveMatchOrder()
    {
        string queue_name = FactoryService.instance.GetMqOrderDeal(this.model.info.market);
        string consume_tag = FactoryService.instance.constant.MqWorker(queue_name, (b) =>
        {
            string json = Encoding.UTF8.GetString(b);
            (long no, List<Orders> orders, List<Deal> deals, List<Orders> cancels) deals = JsonConvert.DeserializeObject<(long no, List<Orders> orders, List<Deal> deals, List<Orders> cancels)>(json);
            this.stopwatch.Restart();
            RedisValue rv = FactoryService.instance.constant.redis.HashGet(FactoryService.instance.GetRedisProcess(), deals.no);
            if (!rv.HasValue)
            {
                return true;
            }
            Processing? process = JsonConvert.DeserializeObject<Processing>(rv);
            if (process == null || process.match == false)
            {
                FactoryService.instance.constant.redis.HashDelete(FactoryService.instance.GetRedisProcess(), deals.no);
                return true;
            }
            ReceiveDealOrder(process, deals.orders, deals.deals, deals.cancels);
            this.stopwatch.Stop();
            FactoryService.instance.constant.logger.LogTrace(this.model.eventId, $"计算耗时:{this.stopwatch.Elapsed.ToString()};{this.model.eventId.Name}:撮合后续处理总时间(成交记录数:{deals.deals.Count},成交订单数:{deals.orders.Count},撤单数:{deals.cancels.Count}),处理结果:{JsonConvert.SerializeObject(process)}");
            if (process.match && process.asset && process.running_fee && process.deal && process.order && process.order_cancel && process.order_complete_thaw && process.push_order && process.push_order_cancel && process.sync_kline && process.push_kline && process.push_deal && process.push_ticker)
            {
                FactoryService.instance.constant.redis.HashDelete(FactoryService.instance.GetRedisProcess(), process.no);
                return true;
            }
            else
            {
                FactoryService.instance.constant.redis.HashSet(FactoryService.instance.GetRedisProcess(), process.no, JsonConvert.SerializeObject(process));
                return false;
            }
        });
        return (queue_name, consume_tag);
    }

    /// <summary>
    /// 接收到成交订单
    /// </summary>
    /// <param name="process">处理进度</param>
    /// <param name="orders">成交的订单</param>
    /// <param name="deals">成交记录</param>
    /// <param name="cancels">撤单订单</param>
    private void ReceiveDealOrder(Processing process, List<Orders> orders, List<Deal> deals, List<Orders> cancels)
    {
        if (deals.Count > 0)
        {
            if (process.asset == false)
            {
                FactoryService.instance.constant.stopwatch.Restart();
                (bool result, List<RunningFee> running_fee, List<RunningTrade> running_trade) result = service_wallet.Transaction(this.model.info, deals);
                process.asset = result.result;
                FactoryService.instance.constant.stopwatch.Stop();
                FactoryService.instance.constant.logger.LogTrace(this.model.eventId, $"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};{this.model.eventId.Name}:DB=>成交记录{deals.Count}条,实际资产转移(结果)");
                if (result.result)
                {
                    if (process.order == false && orders.Count > 0)
                    {
                        FactoryService.instance.constant.stopwatch.Restart();
                        process.order = service_order.UpdateOrder(orders);
                        FactoryService.instance.constant.stopwatch.Stop();
                        FactoryService.instance.constant.logger.LogTrace(this.model.eventId, $"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};{this.model.eventId.Name}:DB=>更新{orders.Count}条订单记录");
                    }
                    else
                    {
                        process.order = true;
                    }
                    if (process.running_fee == false && result.running_fee.Count > 0)
                    {
                        FactoryService.instance.constant.stopwatch.Restart();
                        process.running_fee = service_wallet.AddRunningFee(result.running_fee);
                        FactoryService.instance.constant.stopwatch.Stop();
                        FactoryService.instance.constant.logger.LogTrace(this.model.eventId, $"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};{this.model.eventId.Name}:DB=>添加资金流水(手续费){result.running_fee.Count}条");
                    }
                    else
                    {
                        process.running_fee = true;
                    }
                    if (process.running_trade == false && result.running_trade.Count > 0)
                    {
                        FactoryService.instance.constant.stopwatch.Restart();
                        process.running_trade = service_wallet.AddRunningTrade(result.running_trade);
                        FactoryService.instance.constant.stopwatch.Stop();
                        FactoryService.instance.constant.logger.LogTrace(this.model.eventId, $"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};{this.model.eventId.Name}:DB=>添加资金流水(交易){result.running_trade.Count}条");
                    }
                    else
                    {
                        process.running_trade = true;
                    }
                }
            }
            if (process.deal == false)
            {
                FactoryService.instance.constant.stopwatch.Restart();
                process.deal = service_deal.AddDeal(deals);
                FactoryService.instance.constant.stopwatch.Stop();
                FactoryService.instance.constant.logger.LogTrace(this.model.eventId, $"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};{this.model.eventId.Name}:DB=>成交记录:{deals.Count}");
            }
            if (orders.Count > 0)
            {
                if (process.order_complete_thaw == false)
                {
                    process.order_complete_thaw = true;
                    FactoryService.instance.constant.stopwatch.Restart();
                    E_WalletType wallet_type = E_WalletType.main;
                    if (this.model.info.market_type == E_MarketType.spot)
                    {
                        wallet_type = E_WalletType.spot;
                    }
                    List<Orders> order_buy = orders.Where(P => P.state == E_OrderState.completed && P.unsold > 0 && P.side == E_OrderSide.buy).ToList();
                    var order_buy_uid = from o in order_buy
                                        group o by o.uid into g
                                        select new { uid = g.Key, unsold = g.Sum(P => P.unsold), order = g.ToList() };
                    foreach (var item in order_buy_uid)
                    {
                        if (service_wallet.FreezeChange(wallet_type, item.uid, this.model.info.coin_id_quote, -item.unsold))
                        {
                            item.order.ForEach(P => { P.complete_thaw = P.unsold; P.unsold = 0; });
                        }
                    }
                    process.order_complete_thaw = process.order_complete_thaw && service_order.UpdateOrder(order_buy);
                    List<Orders> order_sell = orders.Where(P => P.state == E_OrderState.completed && P.unsold > 0 && P.side == E_OrderSide.sell).ToList();
                    var order_sell_uid = from o in order_sell
                                         group o by o.uid into g
                                         select new { uid = g.Key, unsold = g.Sum(P => P.unsold), order = g.ToList() };
                    foreach (var item in order_sell_uid)
                    {
                        if (service_wallet.FreezeChange(wallet_type, item.uid, this.model.info.coin_id_base, -item.unsold))
                        {
                            item.order.ForEach(P => { P.complete_thaw = P.unsold; P.unsold = 0; });
                        }
                    }
                    process.order_complete_thaw = process.order_complete_thaw && service_order.UpdateOrder(order_sell);
                    FactoryService.instance.constant.stopwatch.Stop();
                    FactoryService.instance.constant.logger.LogTrace(this.model.eventId, $"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};{this.model.eventId.Name}:DB=>更新订单记录(完成解冻多余资金)");
                }
                if (process.push_order == false)
                {
                    FactoryService.instance.constant.stopwatch.Restart();
                    var uid_order = orders.GroupBy(P => P.uid).ToList();
                    process.push_order = true;
                    foreach (var item in uid_order)
                    {
                        res_order.data = item.ToList();
                        process.push_order = process.push_order && FactoryService.instance.constant.MqPublish(FactoryService.instance.GetMqSubscribe(E_WebsockerChannel.orders, item.Key), JsonConvert.SerializeObject(res_order, new JsonConverterDecimal()));
                    }
                    FactoryService.instance.constant.stopwatch.Stop();
                    FactoryService.instance.constant.logger.LogTrace(this.model.eventId, $"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};{this.model.eventId.Name}:Mq=>推送订单更新");
                }
            }
            else
            {
                process.order_complete_thaw = true;
                process.push_order = true;
            }
            Dictionary<E_KlineType, DateTimeOffset> last_kline = new Dictionary<E_KlineType, DateTimeOffset>();
            if (process.sync_kline == false)
            {
                FactoryService.instance.constant.stopwatch.Restart();
                DateTimeOffset now = DateTimeOffset.UtcNow;
                now = now.AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond);
                DateTimeOffset end = now.AddMilliseconds(-1);
                foreach (E_KlineType cycle in System.Enum.GetValues(typeof(E_KlineType)))
                {
                    Kline? Last_kline = service_kline.GetRedisLastKline(this.model.info.market, cycle);
                    if (Last_kline == null)
                    {
                        last_kline.Add(cycle, FactoryService.instance.system_init);
                    }
                    else
                    {
                        last_kline.Add(cycle, Last_kline.time_start);
                    }
                }
                this.service_kline.DBtoRedised(this.model.info.market, this.model.info.symbol, end);
                this.service_kline.DBtoRedising(this.model.info.market, this.model.info.symbol, deals);
                process.sync_kline = true;
                FactoryService.instance.constant.stopwatch.Stop();
                FactoryService.instance.constant.logger.LogTrace(this.model.eventId, $"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};{this.model.eventId.Name}:DB=>同步K线记录");
            }
            if (process.push_deal == false)
            {
                FactoryService.instance.constant.stopwatch.Restart();
                SortedSetEntry[] entries = new SortedSetEntry[deals.Count()];
                res_deal.data = new List<ResDeal>();
                for (int i = 0; i < deals.Count(); i++)
                {
                    ResDeal resdeal = service_deal.Convert(deals[i]);
                    entries[i] = new SortedSetEntry(JsonConvert.SerializeObject(resdeal), resdeal.time.ToUnixTimeMilliseconds());
                    res_deal.data.Add(resdeal);
                }
                FactoryService.instance.constant.redis.SortedSetAdd(FactoryService.instance.GetRedisDeal(this.model.info.market), entries);
                process.push_deal = FactoryService.instance.constant.MqPublish(FactoryService.instance.GetMqSubscribe(E_WebsockerChannel.trades, this.model.info.market), JsonConvert.SerializeObject(res_deal, new JsonConverterDecimal()));
                FactoryService.instance.constant.stopwatch.Stop();
                FactoryService.instance.constant.logger.LogTrace(this.model.eventId, $"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};{this.model.eventId.Name}:Mq,Redis=>推送交易记录");
            }
            if (process.push_kline == false)
            {
                FactoryService.instance.constant.stopwatch.Restart();
                HashEntry[] hashes = FactoryService.instance.constant.redis.HashGetAll(FactoryService.instance.GetRedisKlineing(this.model.info.market));
                process.push_kline = true;
                foreach (var item in hashes)
                {
                    res_kline.data.Clear();
                    E_KlineType klineType = (E_KlineType)Enum.Parse(typeof(E_KlineType), item.Name.ToString());
                    if (last_kline.ContainsKey(klineType))
                    {
                        res_kline.data.AddRange(service_kline.GetRedisKline(this.model.info.market, klineType, last_kline[klineType], null));
                    }
                    res_kline.channel = (E_WebsockerChannel)Enum.Parse(typeof(E_WebsockerChannel), item.Name.ToString());
                    res_kline.data.Add(JsonConvert.DeserializeObject<Kline>(item.Value)!);
                    process.push_kline = process.push_kline && FactoryService.instance.constant.MqPublish(FactoryService.instance.GetMqSubscribe(res_kline.channel, this.model.info.market), JsonConvert.SerializeObject(res_kline));
                }
                FactoryService.instance.constant.stopwatch.Stop();
                FactoryService.instance.constant.logger.LogTrace(this.model.eventId, $"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};{this.model.eventId.Name}:Mq,Redis=>推送K线记录");
            }
            if (process.push_ticker == false)
            {
                FactoryService.instance.constant.stopwatch.Restart();
                ResTicker? ticker = service_deal.Get24HoursTicker(this.model.info.market);
                process.push_ticker = service_deal.PushTicker(ticker);
                FactoryService.instance.constant.stopwatch.Stop();
                FactoryService.instance.constant.logger.LogTrace(this.model.eventId, $"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};{this.model.eventId.Name}:Mq,Redis=>推送聚合行情");
            }
        }
        else
        {
            process.asset = true;
            process.running_fee = true;
            process.running_trade = true;
            process.deal = true;
            process.order = true;
            process.order_complete_thaw = true;
            process.push_order = true;
            process.sync_kline = true;
            process.push_kline = true;
            process.push_deal = true;
            process.push_ticker = true;
        }
        if (cancels.Count > 0)
        {
            if (process.order_cancel == false)
            {
                FactoryService.instance.constant.stopwatch.Restart();
                E_WalletType wallet_type = E_WalletType.main;
                if (this.model.info.market_type == E_MarketType.spot)
                {
                    wallet_type = E_WalletType.spot;
                }
                List<Orders> order_buy = cancels.Where(P => P.state == E_OrderState.cancel && P.unsold > 0 && P.side == E_OrderSide.buy).ToList();
                var order_buy_uid = from o in order_buy
                                    group o by o.uid into g
                                    select new { uid = g.Key, unsold = g.Sum(P => P.unsold), order = g.ToList() };
                foreach (var item in order_buy_uid)
                {
                    if (service_wallet.FreezeChange(wallet_type, item.uid, this.model.info.coin_id_quote, -item.unsold))
                    {
                        item.order.ForEach(P => { P.complete_thaw = P.unsold; P.unsold = 0; });
                    }
                }
                List<Orders> order_sell = cancels.Where(P => P.state == E_OrderState.cancel && P.unsold > 0 && P.side == E_OrderSide.sell).ToList();
                var order_sell_uid = from o in order_sell
                                     group o by o.uid into g
                                     select new { uid = g.Key, unsold = g.Sum(P => P.unsold), order = g.ToList() };
                foreach (var item in order_sell_uid)
                {
                    if (service_wallet.FreezeChange(wallet_type, item.uid, this.model.info.coin_id_base, -item.unsold))
                    {
                        item.order.ForEach(P => { P.complete_thaw = P.unsold; P.unsold = 0; });
                    }
                }
                process.order_cancel = service_order.UpdateOrder(cancels);
                FactoryService.instance.constant.stopwatch.Stop();
                FactoryService.instance.constant.logger.LogTrace(this.model.eventId, $"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};{this.model.eventId.Name}:DB=>撤单{cancels.Count}条订单记录");
            }
            if (process.push_order_cancel == false)
            {
                var uid_order = cancels.GroupBy(P => P.uid).ToList();
                process.push_order_cancel = true;
                foreach (var item in uid_order)
                {
                    res_order.data = item.ToList();
                    process.push_order_cancel = process.push_order_cancel && FactoryService.instance.constant.MqPublish(FactoryService.instance.GetMqSubscribe(E_WebsockerChannel.orders, item.Key), JsonConvert.SerializeObject(item.ToList(), new JsonConverterDecimal()));
                }
                FactoryService.instance.constant.stopwatch.Stop();
                FactoryService.instance.constant.logger.LogTrace(this.model.eventId, $"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};{this.model.eventId.Name}:Mq=>推送撤单订单");
            }
        }
        else
        {
            process.order_cancel = true;
            process.push_order_cancel = true;
        }
    }

}