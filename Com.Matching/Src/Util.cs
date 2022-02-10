using System;
using Com.Model;
using Com.Model.Enum;
using Snowflake;

namespace Com.Matching;

/// <summary>
/// 工具类
/// </summary>
public static class Util
{
    public static IdWorker worker = new IdWorker(1, 1);

    /// <summary>
    /// 获取最新成交价
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
    /// <param name="name">名称</param>
    /// <param name="bid">买单</param>
    /// <param name="ask">卖单</param>
    /// <param name="price">成交价</param>
    /// <param name="now">成交时间</param>
    /// <returns></returns>
    public static Deal AmountBidAsk(string name, Order bid, Order ask, decimal price, DateTimeOffset now)
    {
        decimal ask_amount = ask.amount_unsold;
        ask.amount_unsold = 0;
        ask.amount_done += ask_amount;
        ask.deal_last_time = now;
        ask.state = E_OrderState.completed;
        bid.amount_unsold -= ask_amount;
        bid.amount_done += ask_amount;
        bid.deal_last_time = now;
        if (bid.amount_unsold <= 0)
        {
            bid.state = E_OrderState.completed;
        }
        else
        {
            bid.state = E_OrderState.partial;
        }
        Deal deal = new Deal()
        {
            id = Util.worker.NextId().ToString(),
            name = name,
            uid_bid = bid.uid,
            uid_ask = ask.uid,
            price = price,
            amount = ask_amount,
            total = price * ask_amount,
            time = now,
            bid = bid,
            ask = ask,
        };
        return deal;
    }

    /// <summary>
    /// 卖单量>=买单量
    /// </summary>
    /// <param name="name">名称</param>
    /// <param name="bid">买单</param>
    /// <param name="ask">卖单</param>
    /// <param name="price">成交价</param>
    /// <param name="now">成交时间</param>
    /// <returns></returns>
    public static Deal AmountAskBid(string name, Order bid, Order ask, decimal price, DateTimeOffset now)
    {
        decimal bid_amount = bid.amount_unsold;
        ask.amount_unsold -= bid_amount;
        ask.amount_done += bid_amount;
        ask.deal_last_time = now;
        if (ask.amount_unsold <= 0)
        {
            ask.state = E_OrderState.completed;
        }
        else
        {
            ask.state = E_OrderState.partial;
        }
        bid.amount_unsold = 0;
        bid.amount_done = bid_amount;
        bid.deal_last_time = now;
        bid.state = E_OrderState.completed;
        Deal deal = new Deal()
        {
            id = Util.worker.NextId().ToString(),
            name = name,
            uid_bid = bid.uid,
            uid_ask = ask.uid,
            price = price,
            amount = bid_amount,
            total = price * bid_amount,
            time = now,
            bid = bid,
            ask = ask,
        };
        return deal;
    }

}