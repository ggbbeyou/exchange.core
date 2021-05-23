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
        /// 撮合算法
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
                    //市价买单与市价卖市撮合
                    foreach (var item in market_ask)
                    {
                        if (item.amount_unsold >= order.amount)
                        {
                            Deal deal = NewMethod(order, now, item);
                            deals.Add(deal);
                            break;
                        }
                        else
                        {
                            Deal deal = NewMethod1(order, now, item);
                            deals.Add(deal);
                            //市价卖单完成,从市价卖单移除
                            market_ask.Remove(item);
                            if (order.amount_unsold <= 0)
                            {
                                break;
                            }
                        }
                    }
                    //市价买单与限价卖单撮合
                    if (order.amount_unsold > 0 && fixed_ask.Count() > 0)
                    {
                        foreach (var item in fixed_ask)
                        {
                            //买单价>=卖单价原则
                            if (order.price < item.price)
                            {
                                break;
                            }
                            //使用撮合价规则
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
                    //市价买单没成交部分添加到市价买单最后,(时间优先原则)
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
        /// 卖单量< 买单量
        /// </summary>
        /// <param name="bid"></param>
        /// <param name="ask"></param>
        /// <param name="new_price"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        private Deal NewMethod1(Order bid, Order ask, decimal new_price, DateTimeOffset now)
        {
            ask.amount_unsold = 0;
            ask.amount_done += ask.amount_unsold;
            ask.deal_last_time = now;
            bid.amount_unsold -= ask.amount_unsold;
            bid.amount_done += ask.amount_unsold;
            bid.deal_last_time = now;
            Deal deal = new Deal()
            {
                id = Util.worker.NextId().ToString(),
                name = this.name,
                uid_bid = bid.uid,
                uid_ask = ask.uid,
                price = new_price,
                amount = ask.amount_unsold,
                total = new_price * bid.amount,
                time = now,
                state = E_DealState.partial,
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
        private Deal NewMethod(Order bid, Order ask, decimal new_price, DateTimeOffset now)
        {
            ask.amount_unsold -= bid.amount;
            ask.amount_done += bid.amount;
            ask.deal_last_time = now;
            bid.amount_unsold = 0;
            bid.amount_done = bid.amount;
            bid.deal_last_time = now;
            Deal deal = new Deal()
            {
                id = Util.worker.NextId().ToString(),
                name = this.name,
                uid_bid = bid.uid,
                uid_ask = ask.uid,
                price = new_price,
                amount = bid.amount,
                total = new_price * bid.amount,
                time = now,
                state = E_DealState.completed,
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
