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
    public ServiceDeal deal_service = new ServiceDeal();
    /// <summary>
    /// 订单服务
    /// </summary>
    /// <returns></returns>
    public ServiceOrder order_service = new ServiceOrder();
    /// <summary>
    /// K线服务
    /// </summary>
    /// <returns></returns>
    public ServiceKline kline_service = new ServiceKline();
    /// <summary>
    /// 钱包服务
    /// </summary>
    /// <returns></returns>
    private ServiceWallet wallet_service = new ServiceWallet();
    /// <summary>
    /// 秒表
    /// </summary>
    /// <returns></returns>
    public Stopwatch stopwatch = new Stopwatch();
    /// <summary>
    /// 临时变量
    /// </summary>
    /// <returns></returns>
    private ResWebsocker<ResDeal> res_deal = new ResWebsocker<ResDeal>();
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
        res_order.op = E_WebsockerOp.subscribe_date;
        res_order.channel = E_WebsockerChannel.orders;
        res_deal.success = true;
        res_deal.op = E_WebsockerOp.subscribe_date;
        res_deal.channel = E_WebsockerChannel.trades;
        res_kline.success = true;
        res_kline.op = E_WebsockerOp.subscribe_date;
        res_kline.data = new List<Kline>();
        ReceiveMatchOrder();
    }

    /// <summary>
    /// 接收撮合传过来的成交订单
    /// </summary>
    public void ReceiveMatchOrder()
    {
        FactoryService.instance.constant.MqWorker(FactoryService.instance.GetMqOrderDeal(this.model.info.market), (b) =>
        {
            string json = Encoding.UTF8.GetString(b);
            (List<Orders> orders, List<Deal> deals, List<Orders> cancels) deals = JsonConvert.DeserializeObject<(List<Orders> orders, List<Deal> deals, List<Orders> cancels)>(json);
            this.stopwatch.Restart();
            bool result = ReceiveDealOrder(deals.orders, deals.deals, deals.cancels);
            this.stopwatch.Stop();
            FactoryService.instance.constant.logger.LogTrace(this.model.eventId, $"计算耗时:{this.stopwatch.Elapsed.ToString()};撮合后续处理总时间(结果{result}),成交记录:{deals.deals.Count}");
            return result;
        });
    }

    /// <summary>
    /// 接收到成交订单
    /// </summary>
    /// <param name="deals"></param>
    private bool ReceiveDealOrder(List<Orders> orders, List<Deal> deals, List<Orders> cancels)
    {
        if (deals.Count > 0)
        {
            FactoryService.instance.constant.stopwatch.Restart();
            (bool result, List<Running> running) transaction = wallet_service.Transaction(E_WalletType.main, this.model.info, deals);
            wallet_service.AddRunning(transaction.running);
            FactoryService.instance.constant.stopwatch.Stop();
            FactoryService.instance.constant.logger.LogTrace(this.model.eventId, $"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};DB=>成交记录{deals.Count}条,实际资产转移(结果{transaction.result})");
            if (!transaction.result)
            {
                FactoryService.instance.constant.logger.LogError(this.model.eventId, $"DB=>成交记录{deals.Count}条,实际资产转移失败");
                return false;
            }
            FactoryService.instance.constant.stopwatch.Restart();
            int deal_add = deal_service.AddOrUpdateDeal(deals);
            FactoryService.instance.constant.stopwatch.Stop();
            FactoryService.instance.constant.logger.LogTrace(this.model.eventId, $"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};DB=>记录:{deals.Count},实际插入{deal_add}条成交记录");
            if (orders.Count > 0 && transaction.result && deal_add > 0)
            {
                FactoryService.instance.constant.stopwatch.Restart();
                order_service.UpdateOrder(orders);
                FactoryService.instance.constant.stopwatch.Stop();
                FactoryService.instance.constant.logger.LogTrace(this.model.eventId, $"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};DB=>更新{orders.Count}条订单记录");
                FactoryService.instance.constant.stopwatch.Restart();
                var uid_order = orders.GroupBy(P => P.uid).ToList();
                foreach (var item in uid_order)
                {
                    res_order.data = item.ToList();
                    FactoryService.instance.constant.MqPublish(FactoryService.instance.GetMqSubscribe(E_WebsockerChannel.orders, this.model.info.market, item.Key), JsonConvert.SerializeObject(item.ToList()));
                }
                FactoryService.instance.constant.stopwatch.Stop();
                FactoryService.instance.constant.logger.LogTrace(this.model.eventId, $"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};Mq=>推送订单更新");
            }
        }
        if (deals.Count > 0)
        {
            FactoryService.instance.constant.stopwatch.Restart();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            now = now.AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond);
            DateTimeOffset end = now.AddMilliseconds(-1);
            Dictionary<E_KlineType, DateTimeOffset> last_kline = new Dictionary<E_KlineType, DateTimeOffset>();
            foreach (E_KlineType cycle in System.Enum.GetValues(typeof(E_KlineType)))
            {
                Kline? Last_kline = kline_service.GetRedisLastKline(this.model.info.market, cycle);
                if (Last_kline == null)
                {
                    last_kline.Add(cycle, FactoryService.instance.system_init);
                }
                else
                {
                    last_kline.Add(cycle, Last_kline.time_start);
                }
            }
            this.kline_service.DBtoRedised(this.model.info.market, this.model.info.symbol, end);
            this.kline_service.DBtoRedising(this.model.info.market, this.model.info.symbol, deals);
            FactoryService.instance.constant.stopwatch.Stop();
            FactoryService.instance.constant.logger.LogTrace(this.model.eventId, $"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};DB=>同步K线记录");
            FactoryService.instance.constant.stopwatch.Restart();
            SortedSetEntry[] entries = new SortedSetEntry[deals.Count()];
            for (int i = 0; i < deals.Count(); i++)
            {
                entries[i] = new SortedSetEntry(JsonConvert.SerializeObject(deals[i]), deals[i].time.ToUnixTimeMilliseconds());
            }
            FactoryService.instance.constant.redis.SortedSetAdd(FactoryService.instance.GetRedisDeal(this.model.info.market), entries);
            res_deal.data = deal_service.ConvertDeal(this.model.info.symbol, deals);
            FactoryService.instance.constant.MqPublish(FactoryService.instance.GetMqSubscribe(E_WebsockerChannel.trades, this.model.info.market), JsonConvert.SerializeObject(res_deal));
            FactoryService.instance.constant.stopwatch.Stop();
            FactoryService.instance.constant.logger.LogTrace(this.model.eventId, $"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};Mq,Redis=>推送交易记录");
            FactoryService.instance.constant.stopwatch.Restart();
            HashEntry[]
            hashes = FactoryService.instance.constant.redis.HashGetAll(FactoryService.instance.GetRedisKlineing(this.model.info.market));
            foreach (var item in hashes)
            {
                res_kline.data.Clear();
                E_KlineType klineType = (E_KlineType)Enum.Parse(typeof(E_KlineType), item.Name.ToString());
                if (last_kline.ContainsKey(klineType))
                {
                    res_kline.data.AddRange(kline_service.GetRedisKline(this.model.info.market, klineType, last_kline[klineType], null));
                }
                res_kline.channel = (E_WebsockerChannel)Enum.Parse(typeof(E_WebsockerChannel), item.Name.ToString());
                res_kline.data.Add(JsonConvert.DeserializeObject<Kline>(item.Value)!);
                FactoryService.instance.constant.MqPublish(FactoryService.instance.GetMqSubscribe(res_kline.channel, this.model.info.market), JsonConvert.SerializeObject(res_kline));
            }
            ResTicker? ticker = deal_service.Get24HoursTicker(this.model.info.market);
            deal_service.PushTicker(ticker);
            FactoryService.instance.constant.stopwatch.Stop();
            FactoryService.instance.constant.logger.LogTrace(this.model.eventId, $"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};Mq,Redis=>推送K线记录和聚合行情");
        }
        if (cancels.Count > 0)
        {
            FactoryService.instance.constant.stopwatch.Restart();
            order_service.UpdateOrder(cancels);
            FactoryService.instance.constant.stopwatch.Stop();
            FactoryService.instance.constant.logger.LogTrace(this.model.eventId, $"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};DB=>撤单{cancels.Count}条订单记录");
            var uid_order = cancels.GroupBy(P => P.uid).ToList();
            foreach (var item in uid_order)
            {
                res_order.data = item.ToList();
                FactoryService.instance.constant.MqPublish(FactoryService.instance.GetMqSubscribe(E_WebsockerChannel.orders, this.model.info.market, item.Key), JsonConvert.SerializeObject(item.ToList()));
            }
            FactoryService.instance.constant.stopwatch.Stop();
            FactoryService.instance.constant.logger.LogTrace(this.model.eventId, $"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};Mq=>推送撤单订单");
        }
        return true;
    }

}