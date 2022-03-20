using System.Text;
using Com.Bll;
using Com.Db;
using Com.Db.Enum;
using Com.Db.Model;
using Com.Service.Match;
using Com.Service.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;

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
    /// 数据库
    /// </summary>
    public DbContextEF db = null!;
    /// <summary>
    /// 交易记录Db操作
    /// </summary>
    /// <returns></returns>
    public DealService deal_service = new DealService();
    /// <summary>
    /// 订单服务
    /// </summary>
    /// <returns></returns>
    public OrderService order_service = new OrderService();
    /// <summary>
    /// K线服务
    /// </summary>
    /// <returns></returns>
    public KlineService kline_service = new KlineService();
    /// <summary>
    /// 临时变量
    /// </summary>
    /// <returns></returns>
    private ResWebsocker<List<Deal>> res_deal = new ResWebsocker<List<Deal>>();
    /// <summary>
    /// 临时变量
    /// </summary>
    /// <typeparam name="Kline?"></typeparam>
    /// <returns></returns>
    private ResWebsocker<Kline?> res_kline = new ResWebsocker<Kline?>();
    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="model"></param>
    public Core(MatchModel model)
    {
        this.model = model;
        var scope = FactoryService.instance.constant.provider.CreateScope();
        this.db = scope.ServiceProvider.GetService<DbContextEF>()!;
        ReceiveMatchOrder();
        ReceiveMatchCancelOrder();
    }

    /// <summary>
    /// 接收撮合传过来的成交订单
    /// </summary>
    public void ReceiveMatchOrder()
    {
        FactoryService.instance.constant.MqWorker(FactoryService.instance.GetMqOrderDeal(this.model.info.market), (b) =>
        {
            string json = Encoding.UTF8.GetString(b);
            // FactoryService.instance.constant.logger.LogInformation($"接收撮合传过来的成交订单:{json}");
            List<Deal>? deals = JsonConvert.DeserializeObject<List<Deal>>(json);
            if (deals != null && deals.Count > 0)
            {
                ReceiveDealOrder(deals);
            }
            return true;
        });
    }

    /// <summary>
    /// 接收撮合传过来的取消订单
    /// </summary>
    public void ReceiveMatchCancelOrder()
    {
        FactoryService.instance.constant.MqWorker(FactoryService.instance.GetMqOrderCancelSuccess(this.model.info.market), (b) =>
        {
            if (!this.model.run)
            {
                return false;
            }
            else
            {
                string json = Encoding.UTF8.GetString(b);
                FactoryService.instance.constant.logger.LogInformation($"接收撮合传过来的取消订单:{json}");
                List<Orders>? deals = JsonConvert.DeserializeObject<List<Orders>>(json);
                if (deals != null && deals.Count > 0)
                {
                    ReceiveCancelOrder(deals);
                }
                return true;
            }
        });
    }

    /// <summary>
    /// 接收到成交订单
    /// </summary>
    /// <param name="match"></param>
    private void ReceiveDealOrder(List<Deal> match)
    {
        FactoryService.instance.constant.stopwatch.Restart();
        deal_service.AddOrUpdateDeal(match);
        FactoryService.instance.constant.stopwatch.Stop();
        FactoryService.instance.constant.logger.LogTrace($"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};DB=>插入{match.Count}条成交记录");
        List<(long, decimal, DateTimeOffset)> list = new List<(long, decimal, DateTimeOffset)>();
        FactoryService.instance.constant.stopwatch.Restart();
        var bid = from deal in match
                  group deal by new { deal.bid_id } into g
                  select new
                  {
                      g.Key.bid_id,
                      amount = g.Sum(x => x.amount),
                      deal_last_time = g.OrderBy(P => P.time).Last().time,
                  };
        foreach (var item in bid)
        {
            list.Add((item.bid_id, item.amount, item.deal_last_time));
        }
        var ask = from deal in match
                  group deal by new { deal.ask_id } into g
                  select new
                  {
                      g.Key.ask_id,
                      amount = g.Sum(x => x.amount),
                      deal_last_time = g.OrderBy(P => P.time).Last().time,
                  };
        foreach (var item in ask)
        {
            list.Add((item.ask_id, item.amount, item.deal_last_time));
        }
        order_service.UpdateOrder(list);
        FactoryService.instance.constant.stopwatch.Stop();
        FactoryService.instance.constant.logger.LogTrace($"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};DB=>更新{list.Count}条订单记录");
        DateTimeOffset now = DateTimeOffset.UtcNow;
        now = now.AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond);
        DateTimeOffset end = now.AddMilliseconds(-1);
        FactoryService.instance.constant.stopwatch.Restart();
        this.kline_service.DBtoRedised(this.model.info.market, this.model.info.symbol, end);
        this.kline_service.DBtoRedising(this.model.info.market);
        FactoryService.instance.constant.stopwatch.Stop();
        FactoryService.instance.constant.logger.LogTrace($"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};DB=>同步K线记录");
        FactoryService.instance.constant.stopwatch.Restart();
        SortedSetEntry[] entries = new SortedSetEntry[match.Count()];
        for (int i = 0; i < match.Count(); i++)
        {
            entries[i] = new SortedSetEntry(JsonConvert.SerializeObject(match[i]), match[i].time.ToUnixTimeMilliseconds());
        }
        FactoryService.instance.constant.redis.SortedSetAdd(FactoryService.instance.GetRedisDeal(this.model.info.market), entries);
        res_deal.success = true;
        res_deal.op = E_WebsockerOp.subscribe_date;
        res_deal.channel = E_WebsockerChannel.trades;
        res_deal.data = match;
        FactoryService.instance.constant.MqPublish(FactoryService.instance.GetMqSubscribe(E_WebsockerChannel.trades, this.model.info.market), JsonConvert.SerializeObject(res_deal));
        FactoryService.instance.constant.stopwatch.Stop();
        FactoryService.instance.constant.logger.LogTrace($"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};Mq,Redis=>推送交易记录");
        FactoryService.instance.constant.stopwatch.Restart();
        HashEntry[] hashes = FactoryService.instance.constant.redis.HashGetAll(FactoryService.instance.GetRedisKlineing(this.model.info.market));
        res_kline.success = true;
        res_kline.op = E_WebsockerOp.subscribe_date;
        foreach (var item in hashes)
        {
            res_kline.channel = (E_WebsockerChannel)Enum.Parse(typeof(E_WebsockerChannel), item.Name.ToString());
            res_kline.data = JsonConvert.DeserializeObject<Kline>(item.Value);
            FactoryService.instance.constant.MqPublish(FactoryService.instance.GetMqSubscribe(res_kline.channel, this.model.info.market), JsonConvert.SerializeObject(res_kline));
        }
        Ticker? ticker = deal_service.Get24HoursTicker(this.model.info.market);
        deal_service.PushTicker(ticker);
        FactoryService.instance.constant.stopwatch.Stop();
        FactoryService.instance.constant.logger.LogTrace($"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};Mq,Redis=>推送K线记录和聚合行情");
    }

    /// <summary>
    /// 接收到取消订单
    /// </summary>
    /// <param name="cancel"></param>
    private void ReceiveCancelOrder(List<Orders> cancel)
    {
        // List<OrderBook> orderBooks = GetOrderBooks(null, deals);
        // this.mq.SendOrderBook(orderBooks);
        // Kline? kline = SetKlink(deals);
        // this.mq.SendKline(kline);
        // foreach (var item in deal)
        // {

        // }
    }




}
