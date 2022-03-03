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
    /// <param name="market">名称</param>
    /// <param name="bid">买单</param>
    /// <param name="ask">卖单</param>
    /// <param name="price">成交价</param>
    /// <param name="trigger_side">触发方向</param>
    /// <param name="now">成交时间</param>
    /// <returns></returns>
    public static MatchDeal AmountBidAsk(string market, MatchOrder bid, MatchOrder ask, decimal price, E_OrderSide trigger_side, DateTimeOffset now)
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
        MatchDeal deal = new MatchDeal()
        {
            trade_id = FactoryMatching.instance.constant.worker.NextId(),
            market = market,
            price = price,
            amount = ask_amount,
           
            trigger_side = trigger_side,
            time = now,
            bid = bid,
            bid_id = bid.order_id,
            ask = ask,
            ask_id = ask.order_id
        };
        return deal;
    }

    /// <summary>
    /// 卖单量>=买单量
    /// </summary>
    /// <param name="market">名称</param>
    /// <param name="bid">买单</param>
    /// <param name="ask">卖单</param>
    /// <param name="price">成交价</param>
    /// <param name="trigger_side">触发方向</param>
    /// <param name="now">成交时间</param>
    /// <returns></returns>
    public static MatchDeal AmountAskBid(string market, MatchOrder bid, MatchOrder ask, decimal price, E_OrderSide trigger_side, DateTimeOffset now)
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
        MatchDeal deal = new MatchDeal()
        {
            trade_id = FactoryMatching.instance.constant.worker.NextId(),
            market = market,
            price = price,
            amount = bid_amount,
            
            trigger_side = trigger_side,
            time = now,
            bid = bid,
            bid_id = bid.order_id,
            ask = ask,
            ask_id = ask.order_id
        };
        return deal;
    }

}