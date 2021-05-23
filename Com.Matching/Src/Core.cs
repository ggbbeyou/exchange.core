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
using Com.Model.Base;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Com.Matching
{
    /// <summary>
    /// 撮合算法核心类
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
        /// 配置接口
        /// </summary>
        public IConfiguration configuration;
        /// <summary>
        /// 日志接口
        /// </summary>
        public ILogger logger;

        public Core(string name, IConfiguration configuration, ILogger logger)
        {
            this.name = name;
            this.logger = logger;
            this.configuration = configuration;
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
                            Deal deal = AmountAskBid(order, market_ask[i], this.price_last, now);
                            deals.Add(deal);
                            if (market_ask[i].amount_unsold == order.amount_unsold)
                            {
                                market_ask.Remove(market_ask[i]);
                            }
                            break;
                        }
                        else if (market_ask[i].amount_unsold < order.amount_unsold)
                        {
                            Deal deal = AmountBidAsk(order, market_ask[i], this.price_last, now);
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
                            decimal new_price = Util.GetNewPrice(order.price, fixed_ask[i].price, this.price_last);
                            if (new_price <= 0)
                            {
                                break;
                            }
                            if (fixed_ask[i].amount_unsold >= order.amount_unsold)
                            {
                                Deal deal = AmountAskBid(order, fixed_ask[i], this.price_last, now);
                                deals.Add(deal);
                                if (fixed_ask[i].amount_unsold == order.amount_unsold)
                                {
                                    fixed_ask.Remove(fixed_ask[i]);
                                }
                                break;
                            }
                            else if (fixed_ask[i].amount_unsold < order.amount_unsold)
                            {
                                Deal deal = AmountBidAsk(order, fixed_ask[i], this.price_last, now);
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
                            Deal deal = AmountAskBid(order, market_ask[i], order.price, now);
                            deals.Add(deal);
                            if (market_ask[i].amount_unsold == order.amount_unsold)
                            {
                                market_ask.Remove(market_ask[i]);
                            }
                            break;
                        }
                        else if (market_ask[i].amount_unsold < order.amount_unsold)
                        {
                            Deal deal = AmountBidAsk(order, market_ask[i], order.price, now);
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
                                Deal deal = AmountAskBid(order, fixed_ask[i], new_price, now);
                                deals.Add(deal);
                                if (fixed_ask[i].amount_unsold == order.amount_unsold)
                                {
                                    fixed_ask.Remove(fixed_ask[i]);
                                }
                                break;
                            }
                            else if (fixed_ask[i].amount_unsold < order.amount_unsold)
                            {
                                Deal deal = AmountBidAsk(order, fixed_ask[i], new_price, now);
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
                        for (int i = 0; i < fixed_bid.Count; i++)
                        {
                            if (order.price >= fixed_bid[i].price && order.time < fixed_bid[i].time)
                            {
                                fixed_bid.Insert(i, order);
                                break;
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
                            Deal deal = AmountBidAsk(market_bid[i], order, this.price_last, now);
                            deals.Add(deal);
                            if (deal.bid.state == E_DealState.completed)
                            {
                                market_bid.Remove(market_bid[i]);
                            }
                            break;
                        }
                        else if (market_bid[i].amount_unsold < order.amount_unsold)
                        {
                            Deal deal = AmountAskBid(market_bid[i], order, this.price_last, now);
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
                                Deal deal = AmountBidAsk(fixed_bid[i], order, new_price, now);
                                deals.Add(deal);
                                if (fixed_bid[i].amount_unsold == order.amount_unsold)
                                {
                                    fixed_bid.Remove(fixed_bid[i]);
                                }
                                break;
                            }
                            else if (fixed_bid[i].amount_unsold < order.amount_unsold)
                            {
                                Deal deal = AmountAskBid(fixed_bid[i], order, new_price, now);
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
                            Deal deal = AmountBidAsk(order, market_bid[i], order.price, now);
                            deals.Add(deal);
                            if (deal.bid.state == E_DealState.completed)
                            {
                                market_bid.Remove(market_bid[i]);
                            }
                            break;
                        }
                        else if (market_bid[i].amount_unsold < order.amount_unsold)
                        {
                            Deal deal = AmountAskBid(market_bid[i], order, order.price, now);
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
                                Deal deal = AmountBidAsk(fixed_bid[i], order, new_price, now);
                                deals.Add(deal);
                                if (deal.bid.state == E_DealState.completed)
                                {
                                    fixed_bid.Remove(fixed_bid[i]);
                                }
                                break;
                            }
                            else if (fixed_bid[i].amount_unsold < order.amount_unsold)
                            {
                                Deal deal = AmountAskBid(fixed_bid[i], order, new_price, now);
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
                        for (int i = 0; i < fixed_ask.Count; i++)
                        {
                            if (order.price <= fixed_ask[i].price && order.time < fixed_ask[i].time)
                            {
                                fixed_ask.Insert(i, order);
                                break;
                            }
                        }
                    }
                }
            }
            return deals;
        }

        /// <summary>
        /// 买单量>卖单量
        /// </summary>
        /// <param name="bid"></param>
        /// <param name="ask"></param>
        /// <param name="new_price"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        private Deal AmountBidAsk(Order bid, Order ask, decimal new_price, DateTimeOffset now)
        {
            ask.amount_unsold = 0;
            ask.amount_done += ask.amount_unsold;
            ask.deal_last_time = now;
            ask.state = E_DealState.completed;
            bid.amount_unsold -= ask.amount_unsold;
            bid.amount_done += ask.amount_unsold;
            bid.deal_last_time = now;
            if (bid.amount_unsold <= 0)
            {
                bid.state = E_DealState.completed;
            }
            else
            {
                bid.state = E_DealState.partial;
            }
            Deal deal = new Deal()
            {
                id = Util.worker.NextId().ToString(),
                name = this.name,
                uid_bid = bid.uid,
                uid_ask = ask.uid,
                price = new_price,
                amount = ask.amount_unsold,
                total = new_price * bid.amount_unsold,
                time = now,
                bid = bid,
                ask = ask,
            };
            return deal;
        }

        /// <summary>
        /// 卖单量>=买单量
        /// </summary>
        /// <param name="bid"></param>
        /// <param name="ask"></param>
        /// <param name="new_price"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        private Deal AmountAskBid(Order bid, Order ask, decimal new_price, DateTimeOffset now)
        {
            ask.amount_unsold -= bid.amount_unsold;
            ask.amount_done += bid.amount_unsold;
            ask.deal_last_time = now;
            if (ask.amount_unsold <= 0)
            {
                ask.state = E_DealState.completed;
            }
            else
            {
                ask.state = E_DealState.partial;
            }
            bid.amount_unsold = 0;
            bid.amount_done = bid.amount_unsold;
            bid.deal_last_time = now;
            bid.state = E_DealState.completed;
            Deal deal = new Deal()
            {
                id = Util.worker.NextId().ToString(),
                name = this.name,
                uid_bid = bid.uid,
                uid_ask = ask.uid,
                price = new_price,
                amount = bid.amount_unsold,
                total = new_price * bid.amount_unsold,
                time = now,
                bid = bid,
                ask = ask,
            };
            return deal;
        }

        /// <summary>
        /// 从MQ获取到撤消订单
        /// </summary>
        /// <param name="order">挂单订单</param>
        public void RemoveOrder(Order order)
        {
            if (order.name != this.name)
            {
                return;
            }

        }

        /// <summary>
        /// 成交订单发送到MQ
        /// </summary>
        /// <param name="deal">成交订单</param>
        public void AddDeal(Deal deal)
        {

        }



    }
}
