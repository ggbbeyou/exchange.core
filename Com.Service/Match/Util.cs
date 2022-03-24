using System;
using Com.Bll;
using Com.Db;
using Com.Api.Sdk.Enum;
using Com.Db.Model;
using Snowflake;

namespace Com.Service.Match;

/// <summary>
/// 工具类
/// </summary>
public static class Util
{
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
    public static decimal GetNewPrice(decimal bid, decimal ask, decimal last)
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
    /// 买单量>卖单量
    /// </summary>
    /// <param name="market">名称</param>
    /// <param name="bid">买单</param>
    /// <param name="ask">卖单</param>
    /// <param name="price">成交价</param>
    /// <param name="amount_places">量的小数位数</param>
    /// <param name="trigger_side">触发方向</param>
    /// <param name="now">成交时间</param>
    /// <returns></returns>
    public static Deal CreateDeal(long market, string symbol, Orders bid, Orders ask, decimal price, int amount_places, E_OrderSide trigger_side, List<Orders> orders)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        decimal min = (decimal)Math.Pow(0.1, (double)amount_places) * price;
        decimal bid_amount_unsold = Math.Round(bid.amount_unsold / price, amount_places, MidpointRounding.ToNegativeInfinity);
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
        if (!orders.Exists(P => P.order_id == bid.order_id))
        {
            orders.Add(bid);
        }
        if (!orders.Exists(P => P.order_id == ask.order_id))
        {
            orders.Add(ask);
        }
        if (bid.amount_unsold < min && leftover > 0)
        {
            bid.trigger_cancel_price = price;
        }
        Deal deal = new Deal()
        {
            trade_id = FactoryService.instance.constant.worker.NextId(),
            market = market,
            symbol = symbol,
            price = price,
            amount = amount,
            total = amount * price,
            trigger_side = trigger_side,
            bid_id = bid.order_id,
            bid_uid = bid.uid,
            bid_amount = bid.amount ?? 0,
            bid_amount_unsold = bid.amount_unsold,
            bid_amount_done = bid.amount_done,
            ask_id = ask.order_id,
            ask_uid = ask.uid,
            ask_amount = ask.amount ?? 0,
            ask_amount_unsold = ask.amount_unsold,
            ask_amount_done = ask.amount_done,
            fee_rate_buy = bid.fee_rate,
            fee_rate_sell = ask.fee_rate,
            time = now,
        };
        return deal;
    }




}