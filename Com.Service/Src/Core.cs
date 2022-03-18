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
    public DealDb deal_db = new DealDb();
    /// <summary>
    /// 订单Db操作
    /// </summary>
    /// <returns></returns>
    public OrdersDb orders_db = new OrdersDb();
    /// <summary>
    /// K线服务
    /// </summary>
    /// <returns></returns>
    public KlineService kline_service = new KlineService();

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
            List<(Orders order, List<Deal> deal)>? deals = JsonConvert.DeserializeObject<List<(Orders order, List<Deal> deal)>>(json);
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
    private void ReceiveDealOrder(List<(Orders order, List<Deal> deal)> match)
    {
        List<BaseOrderBook> orderBooks = new List<BaseOrderBook>();
        List<Orders> orders = new List<Orders>();
        List<Deal> deals = new List<Deal>();
        foreach ((Orders order, List<Deal> deal) item in match)
        {
            orders.Add(item.order);
            deals.AddRange(item.deal);
            orderBooks.AddRange(GetOrderBooks(item.order, item.deal));
        }
        deal_db.AddOrUpdateDeal(deals);
        orders_db.AddOrUpdateOrder(orders);
        string json = JsonConvert.SerializeObject(orderBooks);
        FactoryService.instance.constant.MqPublish($"{E_WebsockerChannel.books10}_{this.model.info.market}", json);
        FactoryService.instance.constant.MqPublish($"{E_WebsockerChannel.books50}_{this.model.info.market}", json);
        FactoryService.instance.constant.MqPublish($"{E_WebsockerChannel.books200}_{this.model.info.market}", json);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        now = now.AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond);
        DateTimeOffset end = now.AddMilliseconds(-1);
        this.kline_service.DBtoRedised(this.model.info.market, this.model.info.symbol, end);
        this.kline_service.DBtoRedising(this.model.info.market);
        HashEntry[] hashes = FactoryService.instance.constant.redis.HashGetAll(FactoryService.instance.GetRedisKlineing(this.model.info.market));
        foreach (var item in hashes)
        {
            FactoryService.instance.constant.MqPublish($"{item.Name}_{this.model.info.market}", item.Value);
        }
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

    #region Depth

    /// <summary>
    /// 
    /// </summary>
    /// <param name="order">触发单</param>
    /// <param name="deals">成交单</param>
    /// <returns></returns>
    public List<BaseOrderBook> GetOrderBooks(Orders order, List<Deal> deals)
    {
        List<BaseOrderBook> depth = new List<BaseOrderBook>();
        if (order.type == E_OrderType.price_fixed && order.amount_unsold > 0)
        {
            depth.Add(UpdateOrderBook(order.side, order.price, order.amount_unsold, order.deal_last_time ?? DateTimeOffset.UtcNow, true));
        }
        foreach (Deal item in deals)
        {
            Orders opponent = item.trigger_side == E_OrderSide.buy ? item.ask : item.bid;
            if (opponent.type == E_OrderType.price_fixed)
            {
                depth.Add(UpdateOrderBook(opponent.side, opponent.price, item.amount, item.time, false));
            }
        }
        return depth;
    }

    /// <summary>
    /// 更新Depth
    /// </summary>
    /// <param name="side"></param>
    /// <param name="price"></param>
    /// <param name="amount"></param>
    /// <param name="is_add"></param>
    /// <returns></returns>
    public BaseOrderBook UpdateOrderBook(E_OrderSide side, decimal price, decimal amount, DateTimeOffset deal_time, bool is_add)
    {
        string key = FactoryService.instance.GetRedisDepth(this.model.info.market, side);
        BaseOrderBook orderBook = new BaseOrderBook();
        StackExchange.Redis.RedisValue[] redisValues = FactoryService.instance.constant.redis.SortedSetRangeByScore(key, (double)price, take: 1);
        if (redisValues.Count() == 0)
        {
            orderBook.market = this.model.info.market;
            orderBook.symbol = this.model.info.symbol;
            orderBook.price = (decimal)price;
            orderBook.amount = amount;
            orderBook.count = 1;
            orderBook.direction = side;
            orderBook.last_time = deal_time;
            FactoryService.instance.constant.redis.SortedSetAdd(key, JsonConvert.SerializeObject(orderBook), (double)price, When.NotExists);
        }
        else
        {
            BaseOrderBook? temp = JsonConvert.DeserializeObject<BaseOrderBook>(redisValues.First());
            if (temp == null)
            {
                orderBook.market = this.model.info.market;
                orderBook.symbol = this.model.info.symbol;
                orderBook.price = (decimal)price;
                orderBook.amount = amount;
                orderBook.count = 1;
                orderBook.direction = side;
                orderBook.last_time = deal_time;
            }
            else
            {
                orderBook.amount += temp.amount + amount;
                orderBook.count = is_add ? temp.count + 1 : temp.count;
                orderBook.last_time = deal_time;
            }
            FactoryService.instance.constant.redis.SortedSetAdd(key, JsonConvert.SerializeObject(orderBook), (double)price, When.NotExists);
        }
        return orderBook!;
    }




    #endregion


}
