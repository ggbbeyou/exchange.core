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
    public string market { get; set; }
    /// <summary>
    /// 常用接口
    /// </summary>
    public FactoryConstant constant = null!;
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
    /// 最近K线
    /// </summary>
    /// <typeparam name="E_KlineType">K线类型</typeparam>
    /// <typeparam name="Kline">K线</typeparam>
    /// <returns></returns>
    public Dictionary<E_KlineType, BaseKline> kline = new Dictionary<E_KlineType, BaseKline>();
    /// <summary>
    /// (Direct)发送历史成交记录
    /// </summary>
    /// <value></value>
    public string key_exchange_deal = "deal";

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="market"></param>
    /// <param name="constant"></param>
    public Core(string market, FactoryConstant constant)
    {
        this.market = market;
        this.constant = constant;
        this.kline_minute = new BaseKline()
        {
            market = market,
            type = E_KlineType.min1,
        };
        this.kline.Add(E_KlineType.min1, new BaseKline()
        {
            market = market,
            type = E_KlineType.min1,
        });
        this.kline.Add(E_KlineType.min5, new BaseKline()
        {
            market = market,
            type = E_KlineType.min5,
        });
        this.kline.Add(E_KlineType.min15, new BaseKline()
        {
            market = market,
            type = E_KlineType.min15,
        });
        this.kline.Add(E_KlineType.min30, new BaseKline()
        {
            market = market,
            type = E_KlineType.min30,
        });
        this.kline.Add(E_KlineType.hour1, new BaseKline()
        {
            market = market,
            type = E_KlineType.hour1,
        });      
        this.kline.Add(E_KlineType.hour6, new BaseKline()
        {
            market = market,
            type = E_KlineType.hour6,
        });
        this.kline.Add(E_KlineType.hour12, new BaseKline()
        {
            market = market,
            type = E_KlineType.hour12,
        });
        this.kline.Add(E_KlineType.day1, new BaseKline()
        {
            market = market,
            type = E_KlineType.day1,
        });
        this.kline.Add(E_KlineType.week1, new BaseKline()
        {
            market = market,
            type = E_KlineType.week1,
        });
        this.kline.Add(E_KlineType.month1, new BaseKline()
        {
            market = market,
            type = E_KlineType.month1,
        });

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
        FactoryMatching.instance.constant.i_model.QueueBind(queue: queueName, exchange: this.key_exchange_deal, routingKey: this.market);
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
                List<MatchDeal>? deals = JsonConvert.DeserializeObject<List<MatchDeal>>(json);
                if (deals != null)
                {
                    MatchDeal? deal = deals.FirstOrDefault();
                    MatchOrder order = deal!.trigger_side == E_OrderSide.buy ? deal.bid : deal.ask;
                    List<BaseOrderBook> orderBooks = GetOrderBooks(order, deals);
                    // this.mq.SendOrderBook(orderBooks);
                    BaseKline? kline = SetKlink(deals);
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
        FactoryMatching.instance.constant.i_model.QueueBind(queue: queueName, exchange: this.key_exchange_deal, routingKey: this.market);
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
    /// <returns>变更的orderbook</returns>
    public List<BaseOrderBook> GetOrderBooks(MatchOrder order, List<MatchDeal> deals)
    {
        List<BaseOrderBook> orderBooks = new List<BaseOrderBook>();
        if (order == null || deals == null || deals.Count == 0)
        {
            return orderBooks;
        }
        decimal amount_deal = deals.Sum(P => P.amount);
        BaseOrderBook? orderBook;
        if (order.type == E_OrderType.price_fixed && order.amount > amount_deal)
        {
            //未完全成交,增加或修改orderBook
            if (order.side == E_OrderSide.buy)
            {
                orderBook = this.bid.FirstOrDefault(P => P.price == order.price);
                if (orderBook == null)
                {
                    orderBook = new BaseOrderBook()
                    {
                        market = this.market,
                        price = order.price,
                        amount = 0,
                        count = 0,
                        last_time = DateTimeOffset.UtcNow,
                        direction = E_OrderSide.buy,
                    };
                    this.bid.Add(orderBook);
                }
                orderBook.amount += (order.amount - amount_deal);
                orderBook.count += 1;
                orderBook.last_time = DateTimeOffset.UtcNow;
                orderBooks.Add(orderBook);
                var fixed_ask = deals.Where(P => P.ask.type == E_OrderType.price_fixed).GroupBy(P => P.price).Select(P => new { price = P.Key, amount = P.Sum(T => T.amount), complet_count = P.Count(T => T.ask.state == E_OrderState.completed) }).ToList();
                foreach (var item in fixed_ask)
                {
                    BaseOrderBook? orderBook_ask = this.ask.FirstOrDefault(P => P.price == item.price);
                    if (orderBook_ask != null)
                    {
                        orderBook_ask.amount -= item.amount;
                        orderBook_ask.count -= item.complet_count;
                        orderBook_ask.last_time = DateTimeOffset.UtcNow;
                        orderBooks.Add(orderBook_ask);
                    }
                    this.ask.RemoveAll(P => P.amount <= 0);
                }
            }
            else if (order.side == E_OrderSide.sell)
            {
                orderBook = this.ask.FirstOrDefault(P => P.price == order.price);
                if (orderBook == null)
                {
                    orderBook = new BaseOrderBook()
                    {
                        market = this.market,
                        price = order.price,
                        amount = 0,
                        count = 0,
                        last_time = DateTimeOffset.UtcNow,
                        direction = E_OrderSide.sell,
                    };
                    this.ask.Add(orderBook);
                }
                orderBook.amount += (order.amount - amount_deal);
                orderBook.count += 1;
                orderBook.last_time = DateTimeOffset.UtcNow;
                orderBooks.Add(orderBook);
                //对手方，则减少orderBook
                var fixed_bid = deals.Where(P => P.bid.type == E_OrderType.price_fixed).GroupBy(P => P.price).Select(P => new { price = P.Key, amount = P.Sum(T => T.amount), complet_count = P.Count(T => T.bid.state == E_OrderState.completed) }).ToList();
                foreach (var item in fixed_bid)
                {
                    BaseOrderBook? orderBook_bid = this.bid.FirstOrDefault(P => P.price == item.price);
                    if (orderBook_bid != null)
                    {
                        orderBook_bid.amount -= item.amount;
                        orderBook_bid.count -= item.complet_count;
                        orderBook_bid.last_time = DateTimeOffset.UtcNow;
                        orderBooks.Add(orderBook_bid);
                    }
                    this.bid.RemoveAll(P => P.amount <= 0);
                }
            }
        }
        return orderBooks;
    }


    /// <summary>
    /// 撤消订单
    /// </summary>
    /// <param name="order_id">订单ID</param>
    /// <returns>orderbook变更</returns>
    public BaseOrderBook CancelOrder(List<string> order_id)
    {

        BaseOrderBook orderBook = new BaseOrderBook();
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
    public BaseKline? SetKlink(List<MatchDeal> deals)
    {
        if (deals == null || deals.Count == 0)
        {
            return null;
        }
        IEnumerable<IGrouping<double, MatchDeal>> deals_minutes = deals.GroupBy(P => (DateTimeOffset.UtcNow - P.time).TotalMinutes);
        foreach (var item in deals_minutes)
        {
            List<MatchDeal> deal = item.ToList();
            if (deal == null || deal.Count == 0)
            {
                return null;
            }
            // if (kline_minute.minute != minute)
            {
                kline_minute.amount = deal.Sum(P => P.amount);
                kline_minute.count = 1;
                kline_minute.total = deal.Sum(P => P.amount * P.price);
                kline_minute.open = deal[0].price;
                kline_minute.close = deal[deal.Count - 1].price;
                kline_minute.low = deal.Min(P => P.price);
                kline_minute.high = deal.Max(P => P.price);
                // kline_minute.time_start = now.AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond);
                kline_minute.time_end = deal[deal.Count - 1].time;
                // kline_minute.minute = 1;
            }
            // else
            {
                kline_minute.amount += deal.Sum(P => P.amount);
                kline_minute.count += 1;
                kline_minute.total += deal.Sum(P => P.amount * P.price);
                kline_minute.close = deal[deal.Count - 1].price;
                kline_minute.low = deal.Min(P => P.price);
                kline_minute.high = deal.Max(P => P.price);
                kline_minute.time_end = deal[deal.Count - 1].time;
            }
        }
        return kline_minute;
    }

}
