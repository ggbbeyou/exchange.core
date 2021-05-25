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


/*
撮合价格

买入价:A,卖出价:B,前一价:C,最新价:D

前提:A>=B

规则:
A<=C    D=A
B>=C    D=B
B<C<A   D=C

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Com.Model.Base;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Com.Matching
{
    /// <summary>
    /// 撮合算法核心类 ,注:撮合引擎不保存数据，所以K线只提供实时分钟K线
    /// </summary>
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
        /// 上一次成交价
        /// </summary>
        public decimal price_last;
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
        /// 市价买单
        /// </summary>
        /// <typeparam name="Order">订单</typeparam>
        /// <returns></returns>
        public List<Order> market_bid = new List<Order>();
        /// <summary>
        /// 市价卖单
        /// </summary>
        /// <typeparam name="Order">订单</typeparam>
        /// <returns></returns>
        public List<Order> market_ask = new List<Order>();
        /// <summary>
        /// 限价买单 高->低
        /// </summary>
        /// <typeparam name="Order">订单</typeparam>
        /// <returns></returns>
        public List<Order> fixed_bid = new List<Order>();
        /// <summary>
        /// 限价卖单 低->高
        /// </summary>
        /// <typeparam name="Order">订单</typeparam>
        /// <returns></returns>
        public List<Order> fixed_ask = new List<Order>();
        /// <summary>
        /// 一分钟K线
        /// </summary>
        /// <returns></returns>
        public Kline kline_minute = new Kline();
        /// <summary>
        /// RabbitMQ模型接口
        /// </summary>
        public readonly IModel channel;
        /// <summary>
        /// 配置接口
        /// </summary>
        public IConfiguration configuration;
        /// <summary>
        /// 日志接口
        /// </summary>
        public ILogger logger;
        private MQ mq = null;

        public Core(string name, IConfiguration configuration, ILogger logger)
        {
            this.name = name;
            this.logger = logger;
            this.configuration = configuration;
            this.mq = new MQ(this);

            
        }

        /// <summary>
        /// 开启
        /// </summary>
        /// <param name="price_last">最后价格</param>
        public void Start(decimal price_last)
        {
            this.price_last = price_last;
            this.run = true;
        }

        public void Stop()
        {
            this.run = false;
        }

        /// <summary>
        /// 主要流程
        /// </summary>
        public void Process(Order order)
        {
            List<Deal> deals = Match(order);
            List<OrderBook> orderBooks = GetOrderBooks(order, deals);
            Kline kline = SetKlink(deals);
        }

        /// <summary>
        /// 撤消订单
        /// </summary>
        /// <param name="order_id">订单ID</param>
        /// <returns>orderbook变更</returns>
        public OrderBook RemoveOrder(string order_id)
        {
            OrderBook orderBook = new OrderBook();
            if (this.market_bid.Exists(P => P.id == order_id))
            {
                this.market_bid.RemoveAll(P => P.id == order_id);
            }
            else if (this.market_ask.Exists(P => P.id == order_id))
            {
                this.market_ask.RemoveAll(P => P.id == order_id);
            }
            else if (this.fixed_bid.Exists(P => P.id == order_id))
            {
                Order order = this.fixed_bid.FirstOrDefault();
                this.fixed_bid.RemoveAll(P => P.id == order_id);
                if (order != null)
                {
                    OrderBook orderBook_bid = bid.FirstOrDefault(P => P.price == order.price);
                    if (orderBook_bid != null)
                    {
                        orderBook_bid.amount -= order.amount_unsold;
                        orderBook_bid.count -= 1;
                        orderBook_bid.last_time = DateTimeOffset.UtcNow;
                        orderBook.name = order.name;
                        orderBook.price = order.price;
                        orderBook.amount = orderBook_bid.amount;
                        orderBook.count = orderBook_bid.count;
                        orderBook.last_time = orderBook_bid.last_time;
                        orderBook.direction = orderBook_bid.direction;
                    }
                }
            }
            else if (this.fixed_ask.Exists(P => P.id == order_id))
            {
                Order order = this.fixed_ask.FirstOrDefault();
                this.fixed_ask.RemoveAll(P => P.id == order_id);
                if (order != null)
                {
                    OrderBook orderBook_ask = ask.FirstOrDefault(P => P.price == order.price);
                    if (orderBook_ask != null)
                    {
                        orderBook_ask.amount -= order.amount_unsold;
                        orderBook_ask.count -= 1;
                        orderBook_ask.last_time = DateTimeOffset.UtcNow;
                        orderBook.name = order.name;
                        orderBook.price = order.price;
                        orderBook.amount = orderBook_ask.amount;
                        orderBook.count = orderBook_ask.count;
                        orderBook.last_time = orderBook_ask.last_time;
                        orderBook.direction = orderBook_ask.direction;
                    }
                }
            }
            return orderBook;
        }

        /// <summary>
        /// 设置当前分钟K线
        /// </summary>
        /// <param name="deals">成交记录</param>
        /// <returns>当前一分钟K线</returns>
        public Kline SetKlink(List<Deal> deals)
        {
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

        /// <summary>
        /// 获取有变量的orderbook(增量)
        /// </summary>
        /// <param name="order">订单</param>
        /// <param name="deals">成交记录</param>
        /// <returns></returns>
        public List<OrderBook> GetOrderBooks(Order order, List<Deal> deals)
        {
            List<OrderBook> orderBooks = new List<OrderBook>();
            decimal amount_deal = deals.Sum(P => P.amount);
            OrderBook orderBook = null;
            if (order.amount > amount_deal)
            {
                //未完全成交,增加orderBook
                if (order.type == E_OrderType.price_fixed && order.direction == E_Direction.bid)
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
                            direction = E_Direction.bid,
                        };
                        bid.Add(orderBook);
                    }
                    orderBook.amount += (order.amount - amount_deal);
                    orderBook.count += 1;
                    orderBook.last_time = DateTimeOffset.UtcNow;
                    orderBooks.Add(orderBook);
                }
                if (order.type == E_OrderType.price_fixed && order.direction == E_Direction.ask)
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
                            direction = E_Direction.ask,
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
            var fixed_bid = deals.Where(P => P.bid.type == E_OrderType.price_fixed).GroupBy(P => P.price).Select(P => new { price = P.Key, amount = P.Sum(T => T.amount), complet_count = P.Count(T => T.bid.state == E_DealState.completed) }).ToList();
            foreach (var item in fixed_bid)
            {
                OrderBook orderBook_bid = bid.FirstOrDefault(P => P.price == item.price);
                if (orderBook_bid != null)
                {
                    orderBook_bid.amount -= item.amount;
                    orderBook_bid.count -= item.complet_count;
                    orderBook_bid.last_time = DateTimeOffset.UtcNow;
                    orderBooks.Add(orderBook_bid);
                }
            }
            var fixed_ask = deals.Where(P => P.ask.type == E_OrderType.price_fixed).GroupBy(P => P.price).Select(P => new { price = P.Key, amount = P.Sum(T => T.amount), complet_count = P.Count(T => T.ask.state == E_DealState.completed) }).ToList();
            foreach (var item in fixed_ask)
            {
                OrderBook orderBook_ask = ask.FirstOrDefault(P => P.price == item.price);
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
        /// 撮合算法(不考虑手续费问题)
        /// </summary>
        /// <param name="order">挂单订单(手续费问题在推送到撮合之前扣除)</param>
        public List<Deal> Match(Order order)
        {
            List<Deal> deals = new List<Deal>();
            if (order.name != this.name || order.amount_unsold <= 0)
            {
                return deals;
            }
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (order.direction == E_Direction.bid)
            {
                //先市价成交,再限价成交
                if (order.type == E_OrderType.price_market)
                {
                    //市价买单与市价卖市撮合
                    for (int i = 0; i < market_ask.Count; i++)
                    {
                        if (market_ask[i].amount_unsold >= order.amount_unsold)
                        {
                            Deal deal = Util.AmountAskBid(this.name, order, market_ask[i], this.price_last, now);
                            deals.Add(deal);
                            if (market_ask[i].amount_unsold == order.amount_unsold)
                            {
                                market_ask.Remove(market_ask[i]);
                            }
                            break;
                        }
                        else if (market_ask[i].amount_unsold < order.amount_unsold)
                        {
                            Deal deal = Util.AmountBidAsk(this.name, order, market_ask[i], this.price_last, now);
                            deals.Add(deal);
                            //市价卖单完成,从市价卖单移除
                            market_ask.Remove(market_ask[i]);
                        }
                        //量全部处理完了
                        if (order.amount_unsold <= 0)
                        {
                            break;
                        }
                    }
                    //市价买单与限价卖单撮合
                    if (order.amount_unsold > 0 && fixed_ask.Count() > 0)
                    {
                        for (int i = 0; i < fixed_ask.Count; i++)
                        {
                            //使用撮合价规则
                            decimal new_price = Util.GetNewPrice(fixed_ask[i].price, fixed_ask[i].price, this.price_last);
                            if (new_price <= 0)
                            {
                                break;
                            }
                            if (fixed_ask[i].amount_unsold >= order.amount_unsold)
                            {
                                Deal deal = Util.AmountAskBid(this.name, order, fixed_ask[i], this.price_last, now);
                                deals.Add(deal);
                                if (fixed_ask[i].amount_unsold == order.amount_unsold)
                                {
                                    fixed_ask.Remove(fixed_ask[i]);
                                }
                                break;
                            }
                            else if (fixed_ask[i].amount_unsold < order.amount_unsold)
                            {
                                Deal deal = Util.AmountBidAsk(this.name, order, fixed_ask[i], this.price_last, now);
                                deals.Add(deal);
                                //市价卖单完成,从市价卖单移除
                                fixed_ask.Remove(fixed_ask[i]);
                            }
                            this.price_last = new_price;
                            //量全部处理完了
                            if (order.amount_unsold <= 0)
                            {
                                break;
                            }
                        }
                    }
                    //市价买单没成交部分添加到市价买单最后,(时间优先原则)
                    if (order.amount_unsold > 0)
                    {
                        market_bid.Add(order);
                    }
                }
                else if (order.type == E_OrderType.price_fixed)
                {

                    //限价买单与市价卖单撮合
                    for (int i = 0; i < market_ask.Count; i++)
                    {
                        if (market_ask[i].amount_unsold >= order.amount_unsold)
                        {
                            Deal deal = Util.AmountAskBid(this.name, order, market_ask[i], order.price, now);
                            deals.Add(deal);
                            if (market_ask[i].amount_unsold == order.amount_unsold)
                            {
                                market_ask.Remove(market_ask[i]);
                            }
                            break;
                        }
                        else if (market_ask[i].amount_unsold < order.amount_unsold)
                        {
                            Deal deal = Util.AmountBidAsk(this.name, order, market_ask[i], order.price, now);
                            deals.Add(deal);
                            //市价卖单完成,从市价卖单移除
                            market_ask.Remove(market_ask[i]);
                        }
                        //量全部处理完了
                        if (order.amount_unsold <= 0)
                        {
                            break;
                        }
                    }
                    //限价买单与限价卖单撮合
                    if (order.amount_unsold > 0 && fixed_ask.Count() > 0)
                    {
                        for (int i = 0; i < fixed_ask.Count; i++)
                        {
                            //使用撮合价规则
                            decimal new_price = Util.GetNewPrice(order.price, fixed_ask[i].price, this.price_last);
                            if (new_price <= 0)
                            {
                                break;
                            }
                            if (fixed_ask[i].amount_unsold >= order.amount_unsold)
                            {
                                Deal deal = Util.AmountAskBid(this.name, order, fixed_ask[i], new_price, now);
                                deals.Add(deal);
                                if (fixed_ask[i].amount_unsold == order.amount_unsold)
                                {
                                    fixed_ask.Remove(fixed_ask[i]);
                                }
                                break;
                            }
                            else if (fixed_ask[i].amount_unsold < order.amount_unsold)
                            {
                                Deal deal = Util.AmountBidAsk(this.name, order, fixed_ask[i], new_price, now);
                                deals.Add(deal);
                                //市价卖单完成,从市价卖单移除
                                fixed_ask.Remove(fixed_ask[i]);
                            }
                            this.price_last = new_price;
                            //量全部处理完了
                            if (order.amount_unsold <= 0)
                            {
                                break;
                            }
                        }
                    }
                    //限价买单没成交部分添加到限价买单相应的位置,(价格优先,时间优先原则)
                    if (order.amount_unsold > 0)
                    {
                        if (fixed_bid.Count == 0)
                        {
                            fixed_bid.Add(order);
                        }
                        else
                        {
                            int index = fixed_bid.FindIndex(P => P.price <= order.price && P.time < order.time);
                            if (index == -1)
                            {
                                fixed_bid.Add(order);
                            }
                            else
                            {
                                fixed_bid.Insert(index, order);
                            }
                        }
                    }
                }
            }
            else if (order.direction == E_Direction.ask)
            {
                //先市价成交,再限价成交
                if (order.type == E_OrderType.price_market)
                {
                    //市价卖单与市价买单撮合
                    for (int i = 0; i < market_bid.Count; i++)
                    {
                        if (market_bid[i].amount_unsold >= order.amount_unsold)
                        {
                            Deal deal = Util.AmountBidAsk(this.name, market_bid[i], order, this.price_last, now);
                            deals.Add(deal);
                            if (deal.bid.state == E_DealState.completed)
                            {
                                market_bid.Remove(market_bid[i]);
                            }
                            break;
                        }
                        else if (market_bid[i].amount_unsold < order.amount_unsold)
                        {
                            Deal deal = Util.AmountAskBid(this.name, market_bid[i], order, this.price_last, now);
                            deals.Add(deal);
                            //市价买单完成,从市价买单移除
                            market_bid.Remove(market_bid[i]);
                        }
                        //量全部处理完了
                        if (order.amount_unsold <= 0)
                        {
                            break;
                        }
                    }
                    //市价卖单与限价买单撮合
                    if (order.amount_unsold > 0 && fixed_bid.Count() > 0)
                    {
                        for (int i = 0; i < fixed_bid.Count; i++)
                        {
                            //使用撮合价规则
                            decimal new_price = Util.GetNewPrice(fixed_bid[i].price, fixed_bid[i].price, this.price_last);
                            if (new_price <= 0)
                            {
                                break;
                            }
                            if (fixed_bid[i].amount_unsold >= order.amount_unsold)
                            {
                                Deal deal = Util.AmountBidAsk(this.name, fixed_bid[i], order, new_price, now);
                                deals.Add(deal);
                                if (fixed_bid[i].amount_unsold == order.amount_unsold)
                                {
                                    fixed_bid.Remove(fixed_bid[i]);
                                }
                                break;
                            }
                            else if (fixed_bid[i].amount_unsold < order.amount_unsold)
                            {
                                Deal deal = Util.AmountAskBid(this.name, fixed_bid[i], order, new_price, now);
                                deals.Add(deal);
                                //市价买单完成,从市价买单移除
                                fixed_bid.Remove(fixed_bid[i]);
                            }
                            this.price_last = new_price;
                            //量全部处理完了
                            if (order.amount_unsold <= 0)
                            {
                                break;
                            }
                        }
                    }
                    //市价卖单没成交部分添加到市价卖单最后,(时间优先原则)
                    if (order.amount_unsold > 0)
                    {
                        market_ask.Add(order);
                    }
                }
                else if (order.type == E_OrderType.price_fixed)
                {
                    //限价卖单与市价买市撮合
                    for (int i = 0; i < market_bid.Count; i++)
                    {
                        if (market_bid[i].amount_unsold >= order.amount_unsold)
                        {
                            Deal deal = Util.AmountBidAsk(this.name, order, market_bid[i], order.price, now);
                            deals.Add(deal);
                            if (deal.bid.state == E_DealState.completed)
                            {
                                market_bid.Remove(market_bid[i]);
                            }
                            break;
                        }
                        else if (market_bid[i].amount_unsold < order.amount_unsold)
                        {
                            Deal deal = Util.AmountAskBid(this.name, market_bid[i], order, order.price, now);
                            deals.Add(deal);
                            //市价买单完成,从市价买单移除
                            market_bid.Remove(market_bid[i]);
                        }
                        //量全部处理完了
                        if (order.amount_unsold <= 0)
                        {
                            break;
                        }
                    }
                    //限价卖单与限价买单撮合
                    if (order.amount_unsold > 0 && fixed_bid.Count() > 0)
                    {
                        for (int i = 0; i < fixed_bid.Count; i++)
                        {
                            //使用撮合价规则
                            decimal new_price = Util.GetNewPrice(fixed_bid[i].price, order.price, this.price_last);
                            if (new_price <= 0)
                            {
                                break;
                            }
                            if (fixed_bid[i].amount_unsold >= order.amount_unsold)
                            {
                                Deal deal = Util.AmountBidAsk(this.name, fixed_bid[i], order, new_price, now);
                                deals.Add(deal);
                                if (deal.bid.state == E_DealState.completed)
                                {
                                    fixed_bid.Remove(fixed_bid[i]);
                                }
                                break;
                            }
                            else if (fixed_bid[i].amount_unsold < order.amount_unsold)
                            {
                                Deal deal = Util.AmountAskBid(this.name, fixed_bid[i], order, new_price, now);
                                deals.Add(deal);
                                //市价买单完成,从市价买单移除
                                fixed_bid.Remove(fixed_bid[i]);
                            }
                            this.price_last = new_price;
                            //量全部处理完了
                            if (order.amount_unsold <= 0)
                            {
                                break;
                            }
                        }
                    }
                    //限价卖单没成交部分添加到限价卖单相应的位置,(价格优先,时间优先原则)
                    if (order.amount_unsold > 0)
                    {
                        if (fixed_ask.Count == 0)
                        {
                            fixed_ask.Add(order);
                        }
                        else
                        {
                            int index = fixed_ask.FindIndex(P => P.price > order.price && P.time < order.time);
                            if (index == -1)
                            {
                                fixed_ask.Add(order);
                            }
                            else
                            {
                                fixed_ask.Insert(index, order);
                            }
                        }
                    }
                }
            }
            return deals;
        }

    }
}
