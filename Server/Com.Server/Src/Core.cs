using System.Text;
using Com.Common;
using Com.Model;
using Com.Model.Enum;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Com.Server;

public class Core
{
    /// <summary>
    /// 是否运行
    /// </summary>
    public bool run;
    /// <summary>
    /// 交易对名称
    /// </summary>
    /// <value></value>
    public string name { get; set; }
    /// <summary>
    /// 常用接口
    /// </summary>
    public FactoryConstant constant = null!;
    /// <summary>
    /// 买盘 高->低
    /// </summary>
    /// <typeparam name="OrderBook">买盘</typeparam>
    /// <returns></returns>
    public List<OrderBook> bid = new List<OrderBook>();
    /// <summary>
    /// 卖盘 低->高
    /// </summary>
    /// <typeparam name="OrderBook">卖盘</typeparam>
    /// <returns></returns>
    public List<OrderBook> ask = new List<OrderBook>();
    /// <summary>
    /// 一分钟K线
    /// </summary>
    /// <returns></returns>
    public Kline? kline_minute;
    /// <summary>
    /// (Direct)发送历史成交记录
    /// </summary>
    /// <value></value>
    public string key_exchange_deal = "deal";

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="name"></param>
    /// <param name="constant"></param>
    public Core(string name, FactoryConstant constant)
    {
        this.name = name;
        this.constant = constant;
        ReceiveMatchOrder();
    }

    /// <summary>
    /// 开启撮合服务
    /// </summary>
    /// <param name="price_last">最后价格</param>
    public void Start()
    {
        this.run = true;
    }

    /// <summary>
    /// 关闭撮合服务
    /// </summary>
    public void Stop()
    {
        this.run = false;
    }

    /// <summary>
    /// 接收撮合传过来的成交订单
    /// </summary>
    public void ReceiveMatchOrder()
    {
        FactoryMatching.instance.constant.i_model.ExchangeDeclare(exchange: this.key_exchange_deal, type: ExchangeType.Direct, durable: true, autoDelete: false, arguments: null);
        string queueName = FactoryMatching.instance.constant.i_model.QueueDeclare().QueueName;
        FactoryMatching.instance.constant.i_model.QueueBind(queue: queueName, exchange: this.key_exchange_deal, routingKey: this.name);
        EventingBasicConsumer consumer = new EventingBasicConsumer(FactoryMatching.instance.constant.i_model);
        consumer.Received += (model, ea) =>
        {
            if (!this.run)
            {
                FactoryMatching.instance.constant.i_model.BasicNack(deliveryTag: ea.DeliveryTag, multiple: true, requeue: true);
            }
            else
            {
                string json = Encoding.UTF8.GetString(ea.Body.ToArray());
                this.constant.logger.LogInformation($"接收撮合传过来的成交订单:{json}");
                KeyValuePair<Order, List<Deal>>? deals = JsonConvert.DeserializeObject<KeyValuePair<Order, List<Deal>>>(json);
                if (deals != null)
                {

                    List<OrderBook> orderBooks = GetOrderBooks(deals?.Key ?? new Order(), deals?.Value ?? new List<Deal>());
                    // this.mq.SendOrderBook(orderBooks);
                    Kline? kline = SetKlink(deals);
                    // this.mq.SendKline(kline);
                    // foreach (var item in deal)
                    // {

                    // }
                    FactoryMatching.instance.constant.i_model.BasicAck(ea.DeliveryTag, true);
                }
            }
        };
        FactoryMatching.instance.constant.i_model.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
    }

    /// <summary>
    /// 接收撮合传过来的取消订单
    /// </summary>
    public void ReceiveMatchCancelOrder()
    {
        FactoryMatching.instance.constant.i_model.ExchangeDeclare(exchange: this.key_exchange_deal, type: ExchangeType.Direct, durable: true, autoDelete: false, arguments: null);
        string queueName = FactoryMatching.instance.constant.i_model.QueueDeclare().QueueName;
        FactoryMatching.instance.constant.i_model.QueueBind(queue: queueName, exchange: this.key_exchange_deal, routingKey: this.name);
        EventingBasicConsumer consumer = new EventingBasicConsumer(FactoryMatching.instance.constant.i_model);
        consumer.Received += (model, ea) =>
        {
            if (!this.run)
            {
                FactoryMatching.instance.constant.i_model.BasicNack(deliveryTag: ea.DeliveryTag, multiple: true, requeue: true);
            }
            else
            {
                string json = Encoding.UTF8.GetString(ea.Body.ToArray());
                this.constant.logger.LogInformation($"接收撮合传过来的取消订单:{json}");
                List<string>? deals = JsonConvert.DeserializeObject<List<string>>(json);
                if (deals != null)
                {
                    // List<OrderBook> orderBooks = GetOrderBooks(null, deals);
                    // this.mq.SendOrderBook(orderBooks);
                    // Kline? kline = SetKlink(deals);
                    // this.mq.SendKline(kline);
                    // foreach (var item in deal)
                    // {

                    // }
                    FactoryMatching.instance.constant.i_model.BasicAck(ea.DeliveryTag, true);
                }
            }
        };
        FactoryMatching.instance.constant.i_model.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
    }


    /// <summary>
    /// 获取有变量的orderbook(增量)
    /// </summary>
    /// <param name="order">订单</param>
    /// <param name="deals">成交记录</param>
    /// <returns></returns>
    public List<OrderBook> GetOrderBooks(Order order, List<Deal> deals)
    {
        List<OrderBook> orderBooks = new List<OrderBook>();
        // if (order == null && deals == null)
        // {
        //     return orderBooks;
        // }
        decimal amount_deal = deals.Sum(P => P.amount);
        OrderBook? orderBook;
        if (order.amount > amount_deal)
        {
            //未完全成交,增加orderBook
            if (order.type == E_OrderType.price_fixed && order.side == E_OrderSide.buy)
            {
                orderBook = bid.FirstOrDefault(P => P.price == order.price);
                if (orderBook == null)
                {
                    orderBook = new OrderBook()
                    {
                        name = this.name,
                        price = order.price,
                        amount = 0,
                        count = 0,
                        last_time = DateTimeOffset.UtcNow,
                        direction = E_OrderSide.buy,
                    };
                    bid.Add(orderBook);
                }
                orderBook.amount += (order.amount - amount_deal);
                orderBook.count += 1;
                orderBook.last_time = DateTimeOffset.UtcNow;
                orderBooks.Add(orderBook);
            }
            if (order.type == E_OrderType.price_fixed && order.side == E_OrderSide.sell)
            {
                orderBook = ask.FirstOrDefault(P => P.price == order.price);
                if (orderBook == null)
                {
                    orderBook = new OrderBook()
                    {
                        name = this.name,
                        price = order.price,
                        amount = 0,
                        count = 0,
                        last_time = DateTimeOffset.UtcNow,
                        direction = E_OrderSide.sell,
                    };
                    ask.Add(orderBook);
                }
                orderBook.amount += (order.amount - amount_deal);
                orderBook.count += 1;
                orderBook.last_time = DateTimeOffset.UtcNow;
                orderBooks.Add(orderBook);
            }
        }
        //已成交，则减少orderBook
        var fixed_bid = deals.Where(P => P.bid.type == E_OrderType.price_fixed).GroupBy(P => P.price).Select(P => new { price = P.Key, amount = P.Sum(T => T.amount), complet_count = P.Count(T => T.bid.state == E_OrderState.completed) }).ToList();
        foreach (var item in fixed_bid)
        {
            OrderBook? orderBook_bid = bid.FirstOrDefault(P => P.price == item.price);
            if (orderBook_bid != null)
            {
                orderBook_bid.amount -= item.amount;
                orderBook_bid.count -= item.complet_count;
                orderBook_bid.last_time = DateTimeOffset.UtcNow;
                orderBooks.Add(orderBook_bid);
            }
        }
        var fixed_ask = deals.Where(P => P.ask.type == E_OrderType.price_fixed).GroupBy(P => P.price).Select(P => new { price = P.Key, amount = P.Sum(T => T.amount), complet_count = P.Count(T => T.ask.state == E_OrderState.completed) }).ToList();
        foreach (var item in fixed_ask)
        {
            OrderBook? orderBook_ask = ask.FirstOrDefault(P => P.price == item.price);
            if (orderBook_ask != null)
            {
                orderBook_ask.amount -= item.amount;
                orderBook_ask.count -= item.complet_count;
                orderBook_ask.last_time = DateTimeOffset.UtcNow;
                orderBooks.Add(orderBook_ask);
            }
        }
        return orderBooks;
    }


    /// <summary>
    /// 撤消订单
    /// </summary>
    /// <param name="order_id">订单ID</param>
    /// <returns>orderbook变更</returns>
    public OrderBook CancelOrder(List<string> order_id)
    {

        OrderBook orderBook = new OrderBook();
        // if (this.market_bid.Exists(P => P.id == order_id))
        // {
        //     this.market_bid.RemoveAll(P => P.id == order_id);
        // }
        // else if (this.market_ask.Exists(P => P.id == order_id))
        // {
        //     this.market_ask.RemoveAll(P => P.id == order_id);
        // }
        // else if (this.fixed_bid.Exists(P => P.id == order_id))
        // {
        //     Order? order = this.fixed_bid.FirstOrDefault();
        //     this.fixed_bid.RemoveAll(P => P.id == order_id);
        //     if (order != null)
        //     {
        //         OrderBook? orderBook_bid = bid.FirstOrDefault(P => P.price == order.price);
        //         if (orderBook_bid != null)
        //         {
        //             orderBook_bid.amount -= order.amount_unsold;
        //             orderBook_bid.count -= 1;
        //             orderBook_bid.last_time = DateTimeOffset.UtcNow;
        //             orderBook.name = order.name;
        //             orderBook.price = order.price;
        //             orderBook.amount = orderBook_bid.amount;
        //             orderBook.count = orderBook_bid.count;
        //             orderBook.last_time = orderBook_bid.last_time;
        //             orderBook.direction = orderBook_bid.direction;
        //         }
        //     }
        // }
        // else if (this.fixed_ask.Exists(P => P.id == order_id))
        // {
        //     Order? order = this.fixed_ask.FirstOrDefault();
        //     this.fixed_ask.RemoveAll(P => P.id == order_id);
        //     if (order != null)
        //     {
        //         OrderBook? orderBook_ask = ask.FirstOrDefault(P => P.price == order.price);
        //         if (orderBook_ask != null)
        //         {
        //             orderBook_ask.amount -= order.amount_unsold;
        //             orderBook_ask.count -= 1;
        //             orderBook_ask.last_time = DateTimeOffset.UtcNow;
        //             orderBook.name = order.name;
        //             orderBook.price = order.price;
        //             orderBook.amount = orderBook_ask.amount;
        //             orderBook.count = orderBook_ask.count;
        //             orderBook.last_time = orderBook_ask.last_time;
        //             orderBook.direction = orderBook_ask.direction;
        //         }
        //     }
        // }
        return orderBook;
    }


    /// <summary>
    /// 设置当前分钟K线
    /// </summary>
    /// <param name="deals">成交记录</param>
    /// <returns>当前一分钟K线</returns>
    public Kline? SetKlink(List<Deal> deals)
    {
        if (deals == null || deals.Count == 0)
        {
            return null;
        }
        if (kline_minute == null)
        {
            kline_minute = new Kline();
        }
        foreach (var item in deals)
        {
            int minute = (int)(item.time - DateTimeOffset.UtcNow.Date).TotalMinutes;
            if (kline_minute.minute != minute)
            {
                kline_minute.name = this.name;
                kline_minute.amount = item.amount;
                kline_minute.count = 1;
                kline_minute.total = item.price * item.amount;
                kline_minute.open = item.price;
                kline_minute.close = item.price;
                kline_minute.low = item.price;
                kline_minute.high = item.price;
                kline_minute.time_start = item.time;
                kline_minute.time_end = item.time;
                kline_minute.minute = minute;
            }
            else
            {
                kline_minute.amount += item.amount;
                kline_minute.count += 1;
                kline_minute.total += item.price * item.amount;
                kline_minute.close = item.price;
                kline_minute.low = item.price < kline_minute.low ? item.price : kline_minute.low;
                kline_minute.high = item.price > kline_minute.high ? item.price : kline_minute.high;
                kline_minute.time_end = item.time;
            }
        }
        return kline_minute;
    }

}
