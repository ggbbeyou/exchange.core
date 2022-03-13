using System.Text;
using Com.Bll;
using Com.Db;
using Com.Db.Enum;
using Com.Db.Model;
using Com.Service.Match;
using Com.Service.Models;
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
    /// 买盘 高->低
    /// </summary>
    /// <typeparam name="OrderBook">买盘</typeparam>
    /// <returns></returns>
    public List<BaseOrderBook> bid = new List<BaseOrderBook>();
    /// <summary>
    /// 卖盘 低->高
    /// </summary>
    /// <typeparam name="OrderBook">卖盘</typeparam>
    /// <returns></returns>
    public List<BaseOrderBook> ask = new List<BaseOrderBook>();
    /// <summary>
    /// 最后一分钟K线
    /// </summary>
    /// <returns></returns>
    public Kline kline_minute = null!;

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="market"></param>
    /// <param name="constant"></param>
    public Core(MatchModel model)
    {
        this.model = model;
        ReceiveMatchOrder();
        ReceiveMatchCancelOrder();
    }

    /// <summary>
    /// 接收撮合传过来的成交订单
    /// </summary>
    public void ReceiveMatchOrder()
    {
        FactoryMatching.instance.constant.i_model.ExchangeDeclare(exchange: this.model.mq.key_deal, type: ExchangeType.Direct, durable: true, autoDelete: false, arguments: null);
        string queueName = FactoryMatching.instance.constant.i_model.QueueDeclare().QueueName;
        FactoryMatching.instance.constant.i_model.QueueBind(queue: queueName, exchange: this.model.mq.key_deal, routingKey: this.model.info.market.ToString());
        EventingBasicConsumer consumer = new EventingBasicConsumer(FactoryMatching.instance.constant.i_model);
        consumer.Received += (model, ea) =>
        {
            if (!this.model.run)
            {
                FactoryMatching.instance.constant.i_model.BasicNack(deliveryTag: ea.DeliveryTag, multiple: true, requeue: true);
            }
            else
            {
                string json = Encoding.UTF8.GetString(ea.Body.ToArray());
                FactoryMatching.instance.constant.logger.LogInformation($"接收撮合传过来的成交订单:{json}");
                List<(Orders order, List<Deal> deal)>? deals = JsonConvert.DeserializeObject<List<(Orders order, List<Deal> deal)>>(json);
                if (deals != null && deals.Count > 0)
                {
                    ReceiveDealOrder(deals);
                }
                FactoryMatching.instance.constant.i_model.BasicAck(ea.DeliveryTag, true);
            }
        };
        FactoryMatching.instance.constant.i_model.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
    }

    /// <summary>
    /// 接收撮合传过来的取消订单
    /// </summary>
    public void ReceiveMatchCancelOrder()
    {
        FactoryMatching.instance.constant.i_model.ExchangeDeclare(exchange: this.model.mq.key_order_cancel_success, type: ExchangeType.Direct, durable: true, autoDelete: false, arguments: null);
        string queueName = FactoryMatching.instance.constant.i_model.QueueDeclare().QueueName;
        FactoryMatching.instance.constant.i_model.QueueBind(queue: queueName, exchange: this.model.mq.key_order_cancel_success, routingKey: this.model.info.market.ToString());
        EventingBasicConsumer consumer = new EventingBasicConsumer(FactoryMatching.instance.constant.i_model);
        consumer.Received += (model, ea) =>
        {
            if (!this.model.run)
            {
                FactoryMatching.instance.constant.i_model.BasicNack(deliveryTag: ea.DeliveryTag, multiple: true, requeue: true);
            }
            else
            {
                string json = Encoding.UTF8.GetString(ea.Body.ToArray());
                FactoryMatching.instance.constant.logger.LogInformation($"接收撮合传过来的取消订单:{json}");
                List<Orders>? deals = JsonConvert.DeserializeObject<List<Orders>>(json);
                if (deals != null && deals.Count > 0)
                {
                    ReceiveCancelOrder(deals);
                }
                FactoryMatching.instance.constant.i_model.BasicAck(ea.DeliveryTag, true);
            }
        };
        FactoryMatching.instance.constant.i_model.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
    }

    /// <summary>
    /// 接收到成交订单
    /// </summary>
    /// <param name="match"></param>
    private void ReceiveDealOrder(List<(Orders order, List<Deal> deal)> match)
    {
        List<BaseOrderBook> orderBooks = new List<BaseOrderBook>();
        List<Kline> klines = new List<Kline>();
        List<Db.Deal> total = new List<Db.Deal>();
        foreach ((Orders order, List<Deal> deal) item in match)
        {
            orderBooks.AddRange(GetOrderBooks(item.order, item.deal));
            foreach (var item1 in item.deal)
            {
                total.Add(new Db.Deal()
                {
                    trade_id = item1.trade_id,
                    market = item1.market,
                    price = item1.price,
                    amount = item1.amount,
                    total = item1.total,
                    trigger_side = item1.trigger_side,
                    bid_id = item1.bid_id,
                    ask_id = item1.ask_id,
                    time = item1.time
                });
            }
        }
        if (FactoryService.instance.deal_db.AddOrUpdateDeal(total) > 0)
        {
            FactoryMatching.instance.ServiceWarmCache(new MarketInfo() { market = this.model.info.market });
        }
        PushKline();
        PullDepth(orderBooks);
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
            depth.Add(UpdateOrderBook(order.side, (double)order.price, order.amount_unsold, order.deal_last_time ?? DateTimeOffset.UtcNow, true));
        }
        foreach (Deal item in deals)
        {
            Orders opponent = order.side == E_OrderSide.buy ? item.ask : item.bid;
            if (opponent.type == E_OrderType.price_fixed)
            {
                depth.Add(UpdateOrderBook(opponent.side, (double)opponent.price, item.amount, item.time, false));
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
    public BaseOrderBook UpdateOrderBook(E_OrderSide side, double price, decimal amount, DateTimeOffset deal_time, bool is_add)
    {
        string key = FactoryService.instance.GetRedisDepth(this.model.info.market, side);
        BaseOrderBook orderBook = new BaseOrderBook();
        StackExchange.Redis.RedisValue[] redisValues = FactoryMatching.instance.constant.redis.SortedSetRangeByScore(key, price);
        if (redisValues.Count() == 0)
        {
            orderBook.market = this.model.info.market;
            orderBook.price = (decimal)price;
            orderBook.amount = amount;
            orderBook.count = 1;
            orderBook.direction = side;
            orderBook.last_time = deal_time;
            FactoryMatching.instance.constant.redis.SortedSetAdd(key, JsonConvert.SerializeObject(orderBook), price);
        }
        else
        {
            BaseOrderBook? temp = JsonConvert.DeserializeObject<BaseOrderBook>(redisValues.First());
            if (temp == null)
            {
                orderBook.market = this.model.info.market;
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
            FactoryMatching.instance.constant.redis.SortedSetAdd(key, JsonConvert.SerializeObject(orderBook), price);
        }
        return orderBook!;
    }

    /// <summary>
    /// 消息队列推送=>更新Depth
    /// </summary>
    /// <param name="depth"></param>
    private void PullDepth(List<BaseOrderBook> depth)
    {
        if (depth.Count == 0)
        {
            return;
        }
        string json = JsonConvert.SerializeObject(depth);
        FactoryMatching.instance.constant.i_model.BasicPublish(exchange: FactoryService.instance.GetMqSubscribeDepth(this.model.info.market), routingKey: "", basicProperties: null, body: Encoding.UTF8.GetBytes(json));
    }

    /// <summary>
    /// K线往消息队列推送
    /// </summary>
    /// <param name="depth"></param>
    private void PushKline()
    {
        HashEntry[] hashes = FactoryMatching.instance.constant.redis.HashGetAll(FactoryService.instance.GetRedisKlineing(this.model.info.market));
        foreach (var item in hashes)
        {
            FactoryMatching.instance.constant.i_model.BasicPublish(exchange: FactoryService.instance.GetMqSubscribeKline(this.model.info.market), routingKey: item.Name, basicProperties: null, body: Encoding.UTF8.GetBytes(item.Value));
        }
    }


    #endregion


}
