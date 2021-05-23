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
        /// 限价买单
        /// </summary>
        /// <typeparam name="Order">订单</typeparam>
        /// <returns></returns>
        public List<Order> fixed_bid = new List<Order>();
        /// <summary>
        /// 限价卖单
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
        /// 从MQ获取到新的订单
        /// </summary>
        /// <param name="order">挂单订单(手续费问题在推送到撮合之前扣除)</param>
        public List<Deal> AddOrder(Order order)
        {
            List<Deal> deals = new List<Deal>();
            if (order == null || order.name != this.name || order.amount_unsold <= 0)
            {
                return deals;
            }
            order.amount_unsold = order.amount;
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (order.direction == E_Direction.bid)
            {
                if (order.type == E_OrderType.price_market)
                {
                    foreach (var item in market_ask)
                    {
                        if (item.amount_unsold >= order.amount)
                        {
                            item.amount_unsold -= order.amount;
                            item.amount_done += order.amount;
                            item.deal_last_time = now;
                            order.amount_unsold = 0;
                            order.amount_done = order.amount;
                            order.deal_last_time = now;
                            Deal deal = new Deal()
                            {
                                id = Util.worker.NextId().ToString(),
                                name = this.name,
                                uid_bid = order.uid,
                                uid_ask = item.uid,
                                price = this.price_last,
                                amount = order.amount,
                                total = this.price_last * order.amount,
                                time = now,
                                state = E_DealState.completed,
                                bid = order,
                                ask = item,
                            };
                            deals.Add(deal);
                            break;
                        }
                        else
                        {
                            item.amount_unsold = 0;
                            item.amount_done += item.amount_unsold;
                            item.deal_last_time = now;
                            order.amount_unsold -= item.amount_unsold;
                            order.amount_done += item.amount_unsold;
                            order.deal_last_time = now;
                            Deal deal = new Deal()
                            {
                                id = Util.worker.NextId().ToString(),
                                name = this.name,
                                uid_bid = order.uid,
                                uid_ask = item.uid,
                                price = this.price_last,
                                amount = item.amount_unsold,
                                total = this.price_last * order.amount,
                                time = now,
                                state = E_DealState.partial,
                                bid = order,
                                ask = item,
                            };
                            deals.Add(deal);
                            market_ask.Remove(item);
                            if (order.amount_unsold <= 0)
                            {
                                break;
                            }
                        }
                    }
                    if (order.amount_unsold > 0 && fixed_ask.Count() > 0)
                    {
                        foreach (var item in fixed_ask)
                        {
                            if (order.price < item.price)
                            {
                                break;
                            }
                            decimal new_price = Util.GetNewPrice(order.price, item.price, this.price_last);
                            if (new_price <= 0)
                            {
                                break;
                            }
                            if (item.amount_unsold >= order.amount)
                            {
                                item.amount_unsold -= order.amount;
                                order.amount_done = order.amount;
                                Deal deal = new Deal()
                                {
                                    id = Util.worker.NextId().ToString(),
                                    name = this.name,
                                    state = E_DealState.completed,
                                };
                                deals.Add(deal);
                            }
                            else if (item.amount_unsold < order.amount)
                            {
                                item.amount_unsold = 0;
                                order.amount_done -= item.amount_unsold;
                                Deal deal = new Deal()
                                {
                                    id = Util.worker.NextId().ToString(),
                                    name = this.name,
                                    state = E_DealState.partial,
                                };
                                deals.Add(deal);
                            }
                            this.price_last = new_price;
                        }
                    }
                    if (order.amount_unsold > 0)
                    {
                        market_bid.Append(order);
                    }
                }
                else if (order.type == E_OrderType.price_fixed)
                {

                }
            }
            else if (order.direction == E_Direction.ask)
            {

            }

            return deals;
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
