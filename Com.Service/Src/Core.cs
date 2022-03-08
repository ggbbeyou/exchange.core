using System.Text;
using Com.Bll;
using Com.Common;
using Com.Db;
using Com.Model;
using Com.Model.Enum;
using Com.Service.Match;
using Com.Service.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;

namespace Com.Service;

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
    public BaseKline kline_minute = null!;
    /// <summary>
    /// redis zset  depth:{market}:{bid/ask}
    /// </summary>
    /// <value></value>
    public string key_redis_depth = "depth:{0}:{1}";

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
        FactoryMatching.instance.constant.i_model.QueueBind(queue: queueName, exchange: this.model.mq.key_deal, routingKey: this.model.info.market);
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
                List<(MatchOrder order, List<MatchDeal> deal)>? deals = JsonConvert.DeserializeObject<List<(MatchOrder order, List<MatchDeal> deal)>>(json);
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
        FactoryMatching.instance.constant.i_model.QueueBind(queue: queueName, exchange: this.model.mq.key_order_cancel_success, routingKey: this.model.info.market);
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
                List<MatchOrder>? deals = JsonConvert.DeserializeObject<List<MatchOrder>>(json);
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
    /// <param name="deals"></param>
    private void ReceiveDealOrder(List<(MatchOrder order, List<MatchDeal> deal)> match)
    {
        List<BaseOrderBook> orderBooks = new List<BaseOrderBook>();
        List<Kline> klines = new List<Kline>();
        List<MatchDeal> total = new List<MatchDeal>();
        foreach ((MatchOrder order, List<MatchDeal> deal) item in match)
        {
            orderBooks.AddRange(GetOrderBooks(item.order, item.deal));
            total.AddRange(item.deal);
        }
        klines.AddRange(SetKlink(total));
        PullDepth(orderBooks);
    }

    /// <summary>
    /// 接收到取消订单
    /// </summary>
    /// <param name="cancel"></param>
    private void ReceiveCancelOrder(List<MatchOrder> cancel)
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
    public List<BaseOrderBook> GetOrderBooks(MatchOrder order, List<MatchDeal> deals)
    {
        List<BaseOrderBook> depth = new List<BaseOrderBook>();
        if (order.type == E_OrderType.price_fixed && order.amount_unsold > 0)
        {
            depth.Add(UpdateOrderBook(order.side, (double)order.price, order.amount_unsold, order.deal_last_time ?? DateTimeOffset.UtcNow, true));
        }
        foreach (MatchDeal item in deals)
        {
            MatchOrder opponent = order.side == E_OrderSide.buy ? item.ask : item.bid;
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
        string key = string.Format(this.key_redis_depth, this.model.info.market, side.ToString());
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
    /// 深度往消息队列推送
    /// </summary>
    /// <param name="depth"></param>
    private void PullDepth(List<BaseOrderBook> depth)
    {
        if (depth.Count == 0)
        {
            return;
        }


        string json = JsonConvert.SerializeObject(depth);
        FactoryMatching.instance.constant.logger.LogInformation($"推送深度:{json}");
        FactoryMatching.instance.constant.i_model.BasicPublish(exchange: "", routingKey: this.model.info.market, basicProperties: null, body: Encoding.UTF8.GetBytes(json));
    }

    /// <summary>
    /// K线往消息队列推送
    /// </summary>
    /// <param name="depth"></param>
    private void PullKline(List<Kline> klines)
    {
        if (klines.Count == 0)
        {
            return;
        }
        string json = JsonConvert.SerializeObject(klines);
        FactoryMatching.instance.constant.logger.LogInformation($"推送K线:{json}");
        FactoryMatching.instance.constant.i_model.BasicPublish(exchange: "", routingKey: this.model.info.market, basicProperties: null, body: Encoding.UTF8.GetBytes(json));
    }


    #endregion






    /// <summary>
    /// 设置当前分钟K线
    /// </summary>
    /// <param name="deals">成交记录</param>
    /// <returns>当前一分钟K线</returns>
    public List<Kline> SetKlink(List<MatchDeal> deals)
    {
        List<Kline> klines = new List<Kline>();
        foreach (E_KlineType cycle in System.Enum.GetValues(typeof(E_KlineType)))
        {
            BaseKline kline = null!;
            RedisValue redisValue = FactoryMatching.instance.constant.redis.HashGet(string.Format(KlineService.instance.redis_key_klineing, this.model.info.market), cycle.ToString());
            if (redisValue.IsNull)
            {
                kline = new BaseKline();
                kline.market = this.model.info.market;
                kline.type = cycle;
                kline.amount = 0;
                kline.count = 0;
                kline.total = 0;
                kline.open = 0;
                kline.close = 0;
                kline.low = 0;
                kline.high = 0;
                kline.time_start = DateTimeOffset.UtcNow;
                kline.time_end = DateTimeOffset.UtcNow;
                kline.time = DateTimeOffset.UtcNow;
            }
            else
            {

            }
        }
        // IEnumerable<IGrouping<double, MatchDeal>> deals_minutes = deals.GroupBy(P => (DateTimeOffset.UtcNow - P.time).TotalMinutes);
        // foreach (var item in deals_minutes)
        // {
        //     List<MatchDeal> deal = item.ToList();
        //     if (deal == null || deal.Count == 0)
        //     {
        //         return null;
        //     }
        //     // if (kline_minute.minute != minute)
        //     {
        //         kline_minute.amount = deal.Sum(P => P.amount);
        //         kline_minute.count = 1;
        //         kline_minute.total = deal.Sum(P => P.amount * P.price);
        //         kline_minute.open = deal[0].price;
        //         kline_minute.close = deal[deal.Count - 1].price;
        //         kline_minute.low = deal.Min(P => P.price);
        //         kline_minute.high = deal.Max(P => P.price);
        //         // kline_minute.time_start = now.AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond);
        //         kline_minute.time_end = deal[deal.Count - 1].time;
        //         // kline_minute.minute = 1;
        //     }
        //     // else
        //     {
        //         kline_minute.amount += deal.Sum(P => P.amount);
        //         kline_minute.count += 1;
        //         kline_minute.total += deal.Sum(P => P.amount * P.price);
        //         kline_minute.close = deal[deal.Count - 1].price;
        //         kline_minute.low = deal.Min(P => P.price);
        //         kline_minute.high = deal.Max(P => P.price);
        //         kline_minute.time_end = deal[deal.Count - 1].time;
        //     }
        // }
        return klines;
    }

}
