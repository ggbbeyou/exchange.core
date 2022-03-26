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




using Com.Db;
using Com.Api.Sdk.Enum;

using Com.Service.Models;
using Com.Bll;
using Com.Bll.Models;
using Com.Api.Sdk.Models;
using System.Text;
using Newtonsoft.Json;

namespace Com.Service.Match;

/// <summary>
/// 撮合算法核心类 ,注:撮合引擎不保存数据
/// </summary>
public class MatchCore
{
    /// <summary>
    /// 撮合服务对象
    /// </summary>
    /// <value></value>
    public MatchModel model { get; set; } = null!;
    /// <summary>
    /// 最后价格
    /// </summary>
    private decimal last_price;
    /// <summary>
    /// 市价买单
    /// </summary>
    /// <typeparam name="Order">订单</typeparam>
    /// <returns></returns>
    public List<Orders> market_bid = new List<Orders>();
    /// <summary>
    /// 市价卖单
    /// </summary>
    /// <typeparam name="Order">订单</typeparam>
    /// <returns></returns>
    public List<Orders> market_ask = new List<Orders>();
    /// <summary>
    /// 限价买单 高->低
    /// </summary>
    /// <typeparam name="Order">订单</typeparam>
    /// <returns></returns>
    public List<Orders> fixed_bid = new List<Orders>();
    /// <summary>
    /// 限价卖单 低->高
    /// </summary>
    /// <typeparam name="Order">订单</typeparam>
    /// <returns></returns>
    public List<Orders> fixed_ask = new List<Orders>();
    /// <summary>
    /// 触发单
    /// </summary>
    /// <typeparam name="Orders"></typeparam>
    /// <returns></returns>
    public List<Orders> trigger = new List<Orders>();

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="model">撮合服务对象</param>
    public MatchCore(MatchModel model)
    {
        this.model = model;
        this.last_price = model.info.last_price;
    }

    /// <summary>
    /// 获取最新成交价
    /// 撮合价格
    /// 买入价:A,卖出价:B,前一价:C,最新价:D
    /// 前提:A>=B
    /// 规则:
    /// A<=C    D=A
    /// B>=C    D=B
    /// B<C<A   D=C
    ///价格优先,时间优先
    /// </summary>
    /// <param name="bid">买入价</param>
    /// <param name="ask">卖出价</param>
    /// <param name="last">最后价格</param>
    /// <returns>最新价</returns>
    public decimal GetNewPrice(decimal bid, decimal ask, decimal last)
    {
        if (bid == 0 || ask == 0 || last == 0)
        {
            return 0;
        }
        if (bid < ask)
        {
            return 0;
        }
        if (bid <= last)
        {
            return bid;
        }
        else if (ask >= last)
        {
            return ask;
        }
        else if (ask < last && last < bid)
        {
            return last;
        }
        return 0;
    }

    /// <summary>
    /// 撤消订单
    /// </summary>
    /// <param name="order_id">订单ID</param>
    /// <returns>orderbook变更</returns>
    public List<Orders> CancelOrder(long uid, List<long> order_id)
    {
        List<Orders> cancel_market_bid = this.market_bid.Where(P => P.uid == uid && order_id.Contains(P.order_id)).ToList();
        this.market_bid.RemoveAll(P => cancel_market_bid.Select(P => P.order_id).Contains(P.order_id));
        List<Orders> cancel_market_ask = this.market_ask.Where(P => P.uid == uid && order_id.Contains(P.order_id)).ToList();
        this.market_ask.RemoveAll(P => cancel_market_ask.Select(P => P.order_id).Contains(P.order_id));
        List<Orders> cancel_fixed_bid = this.fixed_bid.Where(P => P.uid == uid && order_id.Contains(P.order_id)).ToList();
        this.fixed_bid.RemoveAll(P => cancel_fixed_bid.Select(P => P.order_id).Contains(P.order_id));
        List<Orders> cancel_fixed_ask = this.fixed_ask.Where(P => P.uid == uid && order_id.Contains(P.order_id)).ToList();
        this.fixed_ask.RemoveAll(P => cancel_fixed_ask.Select(P => P.order_id).Contains(P.order_id));
        List<Orders> cancel_trigger = this.trigger.Where(P => P.uid == uid && order_id.Contains(P.order_id)).ToList();
        this.trigger.RemoveAll(P => cancel_trigger.Select(P => P.order_id).Contains(P.order_id));
        List<Orders> cancel = new List<Orders>();
        cancel.AddRange(cancel_market_bid);
        cancel.AddRange(cancel_market_ask);
        cancel.AddRange(cancel_fixed_bid);
        cancel.AddRange(cancel_fixed_ask);
        cancel.AddRange(cancel_trigger);
        cancel.ForEach(P => { P.state = E_OrderState.cancel; P.deal_last_time = DateTimeOffset.UtcNow; });
        return cancel;
    }

    /// <summary>
    /// 撤消订单
    /// </summary>
    /// <param name="uid">用户ID</param>
    /// <returns>orderbook变更</returns>
    public List<Orders> CancelOrder(long uid)
    {
        List<Orders> cancel_market_bid = this.market_bid.Where(P => P.uid == uid).ToList();
        this.market_bid.RemoveAll(P => cancel_market_bid.Select(P => P.order_id).Contains(P.order_id));
        List<Orders> cancel_market_ask = this.market_ask.Where(P => P.uid == uid).ToList();
        this.market_ask.RemoveAll(P => cancel_market_ask.Select(P => P.order_id).Contains(P.order_id));
        List<Orders> cancel_fixed_bid = this.fixed_bid.Where(P => P.uid == uid).ToList();
        this.fixed_bid.RemoveAll(P => cancel_fixed_bid.Select(P => P.order_id).Contains(P.order_id));
        List<Orders> cancel_fixed_ask = this.fixed_ask.Where(P => P.uid == uid).ToList();
        this.fixed_ask.RemoveAll(P => cancel_fixed_ask.Select(P => P.order_id).Contains(P.order_id));
        List<Orders> cancel_trigger = this.trigger.Where(P => P.uid == uid).ToList();
        this.trigger.RemoveAll(P => cancel_trigger.Select(P => P.order_id).Contains(P.order_id));
        List<Orders> cancel = new List<Orders>();
        cancel.AddRange(cancel_market_bid);
        cancel.AddRange(cancel_market_ask);
        cancel.AddRange(cancel_fixed_bid);
        cancel.AddRange(cancel_fixed_ask);
        cancel.AddRange(cancel_trigger);
        cancel.ForEach(P => { P.state = E_OrderState.cancel; P.deal_last_time = DateTimeOffset.UtcNow; });
        return cancel;
    }

    /// <summary>
    /// 撤消订单
    /// </summary>
    /// <param name="client_id">客户订单ID</param>
    /// <returns>orderbook变更</returns>
    public List<Orders> CancelOrder(long uid, long[] client_id)
    {
        List<Orders> cancel_market_bid = this.market_bid.Where(P => P.uid == uid && client_id.Contains(P.order_id)).ToList();
        this.market_bid.RemoveAll(P => cancel_market_bid.Select(P => P.order_id).Contains(P.order_id));
        List<Orders> cancel_market_ask = this.market_ask.Where(P => P.uid == uid && client_id.Contains(P.order_id)).ToList();
        this.market_ask.RemoveAll(P => cancel_market_ask.Select(P => P.order_id).Contains(P.order_id));
        List<Orders> cancel_fixed_bid = this.fixed_bid.Where(P => P.uid == uid && client_id.Contains(P.order_id)).ToList();
        this.fixed_bid.RemoveAll(P => cancel_fixed_bid.Select(P => P.order_id).Contains(P.order_id));
        List<Orders> cancel_fixed_ask = this.fixed_ask.Where(P => P.uid == uid && client_id.Contains(P.order_id)).ToList();
        this.fixed_ask.RemoveAll(P => cancel_fixed_ask.Select(P => P.order_id).Contains(P.order_id));
        List<Orders> cancel_trigger = this.trigger.Where(P => P.uid == uid && client_id.Contains(P.order_id)).ToList();
        this.trigger.RemoveAll(P => cancel_trigger.Select(P => P.order_id).Contains(P.order_id));
        List<Orders> cancel = new List<Orders>();
        cancel.AddRange(cancel_market_bid);
        cancel.AddRange(cancel_market_ask);
        cancel.AddRange(cancel_fixed_bid);
        cancel.AddRange(cancel_fixed_ask);
        cancel.AddRange(cancel_trigger);
        cancel.ForEach(P => { P.state = E_OrderState.cancel; P.deal_last_time = DateTimeOffset.UtcNow; });
        return cancel;
    }

    /// <summary>
    /// 撤消订单(该交易对所有订单)
    /// </summary>
    /// <returns>orderbook变更</returns>
    public List<Orders> CancelOrder()
    {
        List<Orders> cancel = new List<Orders>();
        cancel.AddRange(this.market_bid);
        cancel.AddRange(this.market_ask);
        cancel.AddRange(this.fixed_bid);
        cancel.AddRange(this.fixed_ask);
        cancel.AddRange(this.trigger);
        cancel.ForEach(P => { P.state = E_OrderState.cancel; P.deal_last_time = DateTimeOffset.UtcNow; });
        this.market_bid.Clear();
        this.market_ask.Clear();
        this.fixed_bid.Clear();
        this.fixed_ask.Clear();
        this.trigger.Clear();
        return cancel;
    }

    /// <summary>
    /// 获取深度行情
    /// </summary>
    /// <param name="bid"></param>
    /// <param name="GetOrderBook("></param>
    /// <returns></returns>
    public (List<OrderBook> bid, List<OrderBook> ask) GetOrderBook()
    {
        var bids = from b in fixed_bid
                   group b by new { b.market, b.symbol, b.price } into g
                   select new OrderBook { market = g.Key.market, symbol = g.Key.symbol, price = g.Key.price ?? 0, amount = g.Sum(p => p.amount_unsold), count = g.Count(), direction = E_OrderSide.buy, last_time = g.Max(p => p.deal_last_time ?? DateTimeOffset.UtcNow) };
        var asks = from a in fixed_ask
                   group a by new { a.market, a.symbol, a.price } into g
                   select new OrderBook { market = g.Key.market, symbol = g.Key.symbol, price = g.Key.price ?? 0, amount = g.Sum(p => p.amount_unsold), count = g.Count(), direction = E_OrderSide.sell, last_time = g.Max(p => p.deal_last_time ?? DateTimeOffset.UtcNow) };
        return (bids.ToList(), asks.ToList());
    }

    /// <summary>
    ///  撮合算法
    /// </summary>
    /// <param name="order">挂单订单</param>
    /// <returns>成交订单</returns>
    public (List<Orders> orders, List<Deal> deals, List<Orders> cancels) Match(Orders order)
    {
        List<Orders> orders = new List<Orders>();
        List<Deal> deals = new List<Deal>();
        List<Orders> cancels = new List<Orders>();
        decimal? price = null;
        if (order.market != this.model.info.market || order.amount_unsold <= 0 || order.state == E_OrderState.completed || order.state == E_OrderState.cancel)
        {
            return (orders, deals, cancels);
        }
        if (order.state == E_OrderState.not_match && order.trigger_hanging_price > 0)
        {
            this.trigger.Add(order);
            return (orders, deals, cancels);
        }
        if (order.side == E_OrderSide.buy)
        {
            //先市价成交,再限价成交
            if (order.type == E_OrderType.price_market)
            {
                //市价买单与市价卖市撮合
                if ((order.state == E_OrderState.unsold || order.state == E_OrderState.partial) && this.market_ask.Count() > 0)
                {
                    for (int i = 0; i < this.market_ask.Count; i++)
                    {
                        price = CreateDeal(order, this.market_ask[i], this.last_price, E_OrderSide.buy, orders, deals, cancels);
                        if (order.state == E_OrderState.completed || order.state == E_OrderState.cancel)
                        {
                            break;
                        }
                    }
                    this.market_ask.RemoveAll(P => P.state == E_OrderState.completed || P.state == E_OrderState.cancel);
                }
                //市价买单与限价卖单撮合
                if ((order.state == E_OrderState.unsold || order.state == E_OrderState.partial) && this.fixed_ask.Count() > 0)
                {
                    for (int i = 0; i < this.fixed_ask.Count; i++)
                    {
                        price = CreateDeal(order, this.fixed_ask[i], this.fixed_ask[i].price ?? 0, E_OrderSide.buy, orders, deals, cancels);
                        this.last_price = price ?? this.last_price;
                        if (order.state == E_OrderState.completed || order.state == E_OrderState.cancel)
                        {
                            break;
                        }
                    }
                    this.fixed_ask.RemoveAll(P => P.state == E_OrderState.completed || P.state == E_OrderState.cancel);
                }
                //市价买单没成交部分添加到市价买单最后,(时间优先原则)
                if (order.state == E_OrderState.unsold || order.state == E_OrderState.partial)
                {
                    this.market_bid.Add(order);
                }
            }
            else if (order.type == E_OrderType.price_limit)
            {
                //限价买单与市价卖单撮合
                if ((order.state == E_OrderState.unsold || order.state == E_OrderState.partial) && market_ask.Count() > 0)
                {
                    for (int i = 0; i < market_ask.Count; i++)
                    {
                        price = CreateDeal(order, market_ask[i], order.price ?? 0, E_OrderSide.buy, orders, deals, cancels);
                        if (order.state == E_OrderState.completed || order.state == E_OrderState.cancel)
                        {
                            break;
                        }
                    }
                    this.last_price = price ?? this.last_price;
                    market_ask.RemoveAll(P => P.state == E_OrderState.completed || P.state == E_OrderState.cancel);
                }
                //限价买单与限价卖单撮合
                if ((order.state == E_OrderState.unsold || order.state == E_OrderState.partial) && fixed_ask.Count() > 0)
                {
                    for (int i = 0; i < fixed_ask.Count; i++)
                    {
                        decimal new_price = GetNewPrice(order.price ?? 0, fixed_ask[i].price ?? 0, this.last_price);
                        if (new_price <= 0)
                        {
                            break;
                        }
                        price = CreateDeal(order, fixed_ask[i], new_price, E_OrderSide.buy, orders, deals, cancels);
                        this.last_price = price ?? this.last_price;
                        if (order.state == E_OrderState.completed || order.state == E_OrderState.cancel)
                        {
                            break;
                        }
                    }
                    fixed_ask.RemoveAll(P => P.state == E_OrderState.completed || P.state == E_OrderState.cancel);
                }
                //限价买单没成交部分添加到限价买单相应的位置,(价格优先,时间优先原则)
                if (order.state == E_OrderState.unsold || order.state == E_OrderState.partial)
                {
                    fixed_bid.Add(order);
                    fixed_bid = fixed_bid.OrderByDescending(P => P.price).ThenBy(P => P.create_time).ToList();
                }
            }
        }
        else if (order.side == E_OrderSide.sell)
        {
            //先市价成交,再限价成交
            if (order.type == E_OrderType.price_market)
            {
                if ((order.state == E_OrderState.unsold || order.state == E_OrderState.partial) && market_bid.Count() > 0)
                {
                    //市价卖单与市价买单撮合
                    for (int i = 0; i < market_bid.Count; i++)
                    {
                        price = CreateDeal(market_bid[i], order, this.last_price, E_OrderSide.sell, orders, deals, cancels);
                        if (order.state == E_OrderState.completed || order.state == E_OrderState.cancel)
                        {
                            break;
                        }
                    }
                    market_bid.RemoveAll(P => P.state == E_OrderState.completed || P.state == E_OrderState.cancel);
                }
                //市价卖单与限价买单撮合
                if ((order.state == E_OrderState.unsold || order.state == E_OrderState.partial) && fixed_bid.Count() > 0)
                {
                    for (int i = 0; i < fixed_bid.Count; i++)
                    {
                        price = CreateDeal(fixed_bid[i], order, fixed_bid[i].price ?? 0, E_OrderSide.sell, orders, deals, cancels);
                        this.last_price = price ?? this.last_price;
                        if (order.state == E_OrderState.completed || order.state == E_OrderState.cancel)
                        {
                            break;
                        }
                    }
                    fixed_bid.RemoveAll(P => P.state == E_OrderState.completed || P.state == E_OrderState.cancel);
                }
                //市价卖单没成交部分添加到市价卖单最后,(时间优先原则)
                if (order.state == E_OrderState.unsold || order.state == E_OrderState.partial)
                {
                    market_ask.Add(order);
                }
            }
            else if (order.type == E_OrderType.price_limit)
            {
                //限价卖单与市价买市撮合
                if ((order.state == E_OrderState.unsold || order.state == E_OrderState.partial) && market_bid.Count() > 0)
                {
                    for (int i = 0; i < market_bid.Count; i++)
                    {
                        price = CreateDeal(market_bid[i], order, order.price ?? 0, E_OrderSide.sell, orders, deals, cancels);
                        if (order.state == E_OrderState.completed || order.state == E_OrderState.cancel)
                        {
                            break;
                        }
                    }
                    this.last_price = price ?? this.last_price;
                    market_bid.RemoveAll(P => P.state == E_OrderState.completed || P.state == E_OrderState.cancel);
                }
                //限价卖单与限价买单撮合
                if ((order.state == E_OrderState.unsold || order.state == E_OrderState.partial) && fixed_bid.Count() > 0)
                {
                    for (int i = 0; i < fixed_bid.Count; i++)
                    {
                        decimal new_price = GetNewPrice(fixed_bid[i].price ?? 0, order.price ?? 0, this.last_price);
                        if (new_price <= 0)
                        {
                            break;
                        }
                        price = CreateDeal(fixed_bid[i], order, new_price, E_OrderSide.sell, orders, deals, cancels);
                        this.last_price = price ?? this.last_price;
                        if (order.state == E_OrderState.completed || order.state == E_OrderState.cancel)
                        {
                            break;
                        }
                    }
                    fixed_bid.RemoveAll(P => P.state == E_OrderState.completed || P.state == E_OrderState.cancel);
                }
                //限价卖单没成交部分添加到限价卖单相应的位置,(价格优先,时间优先原则)
                if (order.state == E_OrderState.unsold || order.state == E_OrderState.partial)
                {
                    fixed_ask.Add(order);
                    fixed_ask = fixed_ask.OrderBy(P => P.price).ThenBy(P => P.create_time).ToList();
                }
            }
        }
        return (orders, deals, cancels);
    }

    /// <summary>
    /// 创建成交
    /// </summary>
    /// <param name="bid">买单</param>
    /// <param name="ask">卖单</param>
    /// <param name="price">成交价</param>
    /// <param name="trigger_side">触发方向</param>
    /// <param name="orders">影响的订单</param>
    /// <param name="deals">生成成交记录</param>
    /// <param name="cancels">自动撤单</param>
    /// <returns>最新成交价</returns>
    public decimal? CreateDeal(Orders bid, Orders ask, decimal price, E_OrderSide trigger_side, List<Orders> orders, List<Deal> deals, List<Orders> cancels)
    {
        if (price <= 0 || bid.state == E_OrderState.cancel || bid.state == E_OrderState.completed || ask.state == E_OrderState.cancel || ask.state == E_OrderState.completed)
        {
            return null;
        }
        DateTimeOffset now = DateTimeOffset.UtcNow;
        decimal bid_amount_unsold = Math.Round(bid.amount_unsold / price / this.model.info.amount_multiple, 0, MidpointRounding.ToNegativeInfinity) * this.model.info.amount_multiple;
        decimal leftover = bid.amount_unsold - (bid_amount_unsold * price);
        decimal amount = 0;
        if (bid_amount_unsold > ask.amount_unsold)
        {
            amount = ask.amount_unsold;
            ask.state = E_OrderState.completed;
            bid.state = E_OrderState.partial;
        }
        else if (bid_amount_unsold < ask.amount_unsold)
        {
            amount = bid_amount_unsold;
            bid.state = E_OrderState.completed;
            ask.state = E_OrderState.partial;
        }
        else if (bid_amount_unsold == ask.amount_unsold)
        {
            amount = bid_amount_unsold;
            bid.state = E_OrderState.completed;
            ask.state = E_OrderState.completed;
        }
        if (amount <= 0)
        {
            return null;
        }
        bid.amount_unsold -= amount * price;
        bid.amount_done += amount * price;
        bid.deal_last_time = now;
        if (bid.type == E_OrderType.price_market)
        {
            bid.amount += amount;
            bid.price = bid.amount_done / bid.amount;
        }
        ask.amount_unsold -= amount;
        ask.amount_done += amount;
        ask.deal_last_time = now;
        Deal deal = new Deal()
        {
            trade_id = FactoryService.instance.constant.worker.NextId(),
            market = this.model.info.market,
            symbol = this.model.info.symbol,
            price = price,
            amount = amount,
            total = amount * price,
            trigger_side = trigger_side,
            bid_id = bid.order_id,
            ask_id = ask.order_id,
            bid_uid = bid.uid,
            ask_uid = ask.uid,
            bid_name = bid.user_name,
            ask_name = ask.user_name,
            bid_amount_unsold = bid.amount_unsold,
            ask_amount_unsold = ask.amount_unsold,
            bid_amount_done = bid.amount_done,
            ask_amount_done = ask.amount_done,
            fee_rate_buy = bid.fee_rate,
            fee_rate_sell = ask.fee_rate,
            fee_buy = (amount * price) * bid.fee_rate,
            fee_sell = (amount * price) * ask.fee_rate,
            fee_coin_buy = this.model.info.coin_id_quote,
            fee_coin_sell = this.model.info.coin_id_base,
            time = now,
        };
        deals.Add(deal);
        if (!orders.Exists(P => P.order_id == bid.order_id))
        {
            orders.Add(bid);
        }
        if (!orders.Exists(P => P.order_id == ask.order_id))
        {
            orders.Add(ask);
        }
        if ((bid.state == E_OrderState.unsold || bid.state == E_OrderState.partial) && ((bid.trigger_cancel_price > 0 && bid.trigger_cancel_price >= price) || bid.amount_unsold / price < this.model.info.amount_multiple || bid.amount_unsold == leftover))
        {
            bid.state = E_OrderState.cancel;
            cancels.Add(bid);
        }
        if ((ask.state == E_OrderState.unsold || ask.state == E_OrderState.partial) && ((bid.trigger_cancel_price > 0 && ask.trigger_cancel_price <= price) || ask.amount_unsold < this.model.info.amount_multiple))
        {
            ask.state = E_OrderState.cancel;
            cancels.Add(ask);
        }
        List<Orders> trigger_order = trigger.Where(P => (P.side == E_OrderSide.buy && P.trigger_hanging_price <= price) || (P.side == E_OrderSide.sell && P.trigger_hanging_price >= price)).ToList();
        this.trigger.RemoveAll(P => trigger_order.Select(T => T.order_id).Contains(P.order_id));
        trigger_order.ForEach(P =>
        {
            P.state = E_OrderState.unsold;
        });
        ReqCall<List<Orders>> call_req = new ReqCall<List<Orders>>();
        call_req.op = E_Op.place;
        call_req.market = this.model.info.market;
        call_req.data = trigger_order;
        FactoryService.instance.constant.MqSend(FactoryService.instance.GetMqOrderPlace(this.model.info.market), Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(call_req)));
        return price;
    }
}