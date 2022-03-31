using Com.Db;
using Com.Api.Sdk.Enum;

using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using StackExchange.Redis;
using Com.Api.Sdk.Models;
using Com.Bll.Models;

namespace Com.Bll;

/// <summary>
/// Service:深度行情
/// </summary>
public class ServiceDepth
{
    /// <summary>
    /// 单例类的实例
    /// </summary>
    /// <returns></returns>
    public static readonly ServiceDepth instance = new ServiceDepth();

    /// <summary>
    /// 初始化
    /// </summary>
    private ServiceDepth()
    {
    }

    /// <summary>
    /// (全部)转换深度行情
    /// </summary>
    /// <param name="bid"></param>
    /// <param name="orderbook"></param>
    /// <returns></returns>
    public Dictionary<E_WebsockerChannel, ResDepth> ConvertDepth(long market, string symbol, (List<OrderBook> bid, List<OrderBook> ask) orderbook)
    {
        Dictionary<E_WebsockerChannel, ResDepth> depths = new Dictionary<E_WebsockerChannel, ResDepth>();
        depths.Add(E_WebsockerChannel.books10, new ResDepth());
        depths.Add(E_WebsockerChannel.books50, new ResDepth());
        depths.Add(E_WebsockerChannel.books200, new ResDepth());
        decimal total_bid = 0;
        for (int i = 0; i < orderbook.bid.Count; i++)
        {
            total_bid += orderbook.bid[i].amount * orderbook.bid[i].price;
            if (i < 10)
            {
                depths[E_WebsockerChannel.books10].bid.Add(new List<decimal>() { orderbook.bid[i].price, orderbook.bid[i].amount });
                depths[E_WebsockerChannel.books10].total_bid = total_bid;
            }
            if (i < 50)
            {
                depths[E_WebsockerChannel.books50].bid.Add(new List<decimal>() { orderbook.bid[i].price, orderbook.bid[i].amount });
                depths[E_WebsockerChannel.books50].total_bid = total_bid;
            }
            if (i < 200)
            {
                depths[E_WebsockerChannel.books200].bid.Add(new List<decimal>() { orderbook.bid[i].price, orderbook.bid[i].amount });
                depths[E_WebsockerChannel.books200].total_bid = total_bid;
            }
        }
        decimal total_ask = 0;
        for (int i = 0; i < orderbook.ask.Count; i++)
        {
            total_ask += orderbook.ask[i].amount * orderbook.ask[i].price;
            if (i < 10)
            {
                depths[E_WebsockerChannel.books10].ask.Add(new List<decimal>() { orderbook.ask[i].price, orderbook.ask[i].amount });
                depths[E_WebsockerChannel.books10].total_ask = total_ask;
            }
            if (i < 50)
            {
                depths[E_WebsockerChannel.books50].ask.Add(new List<decimal>() { orderbook.ask[i].price, orderbook.ask[i].amount });
                depths[E_WebsockerChannel.books50].total_ask = total_ask;
            }
            if (i < 200)
            {
                depths[E_WebsockerChannel.books200].ask.Add(new List<decimal>() { orderbook.ask[i].price, orderbook.ask[i].amount });
                depths[E_WebsockerChannel.books200].total_ask = total_ask;
            }
        }
        return depths;
    }

    /// <summary>
    /// (部分)转换深度行情
    /// </summary>
    /// <param name="bid"></param>
    /// <param name="orderbook"></param>
    /// <returns></returns>
    public Dictionary<E_WebsockerChannel, ResDepth> ConvertDepth(long market, string symbol, (List<(int index, OrderBook orderbook)> bid, List<(int index, OrderBook orderbook)> ask) orderbook)
    {
        Dictionary<E_WebsockerChannel, ResDepth> depths = new Dictionary<E_WebsockerChannel, ResDepth>();
        depths.Add(E_WebsockerChannel.books10_inc, new ResDepth());
        depths.Add(E_WebsockerChannel.books50_inc, new ResDepth());
        depths.Add(E_WebsockerChannel.books200_inc, new ResDepth());
        decimal total_bid = 0;
        for (int i = 0; i < orderbook.bid.Count; i++)
        {
            total_bid += orderbook.bid[i].orderbook.amount * orderbook.bid[i].orderbook.price;
            if (i < 10)
            {
                depths[E_WebsockerChannel.books10].bid.Add(new List<decimal>() { orderbook.bid[i].orderbook.price, orderbook.bid[i].orderbook.amount });
                depths[E_WebsockerChannel.books10].total_bid = total_bid;
            }
            if (i < 50)
            {
                depths[E_WebsockerChannel.books50].bid.Add(new List<decimal>() { orderbook.bid[i].orderbook.price, orderbook.bid[i].orderbook.amount });
                depths[E_WebsockerChannel.books50].total_bid = total_bid;
            }
            if (i < 200)
            {
                depths[E_WebsockerChannel.books200].bid.Add(new List<decimal>() { orderbook.bid[i].orderbook.price, orderbook.bid[i].orderbook.amount });
                depths[E_WebsockerChannel.books200].total_bid = total_bid;
            }
        }
        decimal total_ask = 0;
        for (int i = 0; i < orderbook.ask.Count; i++)
        {
            total_ask += orderbook.ask[i].orderbook.amount * orderbook.ask[i].orderbook.price;
            if (i < 10)
            {
                depths[E_WebsockerChannel.books10].ask.Add(new List<decimal>() { orderbook.ask[i].orderbook.price, orderbook.ask[i].orderbook.amount });
                depths[E_WebsockerChannel.books10].total_ask = total_ask;
            }
            if (i < 50)
            {
                depths[E_WebsockerChannel.books50].ask.Add(new List<decimal>() { orderbook.ask[i].orderbook.price, orderbook.ask[i].orderbook.amount });
                depths[E_WebsockerChannel.books50].total_ask = total_ask;
            }
            if (i < 200)
            {
                depths[E_WebsockerChannel.books200].ask.Add(new List<decimal>() { orderbook.ask[i].orderbook.price, orderbook.ask[i].orderbook.amount });
                depths[E_WebsockerChannel.books200].total_ask = total_ask;
            }
        }
        return depths;
    }

    /// <summary>
    /// (全部)深度行情保存到redis并且推送到MQ
    /// </summary>
    /// <param name="depth"></param>
    public void Push(long market, Dictionary<E_WebsockerChannel, ResDepth> depths, bool all)
    {
        ResWebsocker<ResDepth> resWebsocker = new ResWebsocker<ResDepth>();
        resWebsocker.success = true;
        resWebsocker.op = E_WebsockerOp.subscribe_date;
        foreach (var item in depths)
        {
            if (all)
            {
                FactoryService.instance.constant.redis.HashSet(FactoryService.instance.GetRedisDepth(market), item.Key.ToString(), JsonConvert.SerializeObject(item.Value));
            }
            resWebsocker.channel = item.Key;
            resWebsocker.data = item.Value;
            FactoryService.instance.constant.MqPublish(FactoryService.instance.GetMqSubscribe(item.Key, market), JsonConvert.SerializeObject(resWebsocker));
        }
    }

    /// <summary>
    /// 删除redis中的深度行情
    /// </summary>
    /// <param name="market"></param>
    public void DeleteRedisDepth(long market)
    {
        FactoryService.instance.constant.redis.KeyDelete(FactoryService.instance.GetRedisDepth(market));
    }

    /// <summary>
    /// (部分)深度差异
    /// </summary>
    /// <param name="bid"></param>
    /// <param name="bid"></param>
    /// <param name="book_old"></param>
    /// <param name="bid"></param>
    /// <param name="book_new"></param>
    /// <returns></returns>
    public (List<(int, OrderBook)> bid, List<(int, OrderBook)> ask) DiffOrderBook((List<OrderBook> bid, List<OrderBook> ask) book_old, (List<OrderBook> bid, List<OrderBook> ask) book_new)
    {
        List<(int index, OrderBook orderbook)> bid_diff = new List<(int index, OrderBook orderbook)>();
        List<(int index, OrderBook orderbook)> ask_diff = new List<(int index, OrderBook orderbook)>();
        if (book_old.bid == null || book_old.bid.Count == 0)
        {
            for (int i = 0; i < book_new.bid.Count; i++)
            {
                bid_diff.Add((i + 1, book_new.bid[i]));
            }
        }
        else
        {
            bid_diff = DiffOrderBook(book_old.bid, book_new.bid);
        }
        if (book_old.ask == null || book_old.ask.Count == 0)
        {
            for (int i = 0; i < book_new.ask.Count; i++)
            {
                ask_diff.Add((i + 1, book_new.ask[i]));
            }
        }
        else
        {
            ask_diff = DiffOrderBook(book_old.ask, book_new.ask);
        }
        return (bid_diff, ask_diff);
    }

    /// <summary>
    /// (部分)差异
    /// </summary>
    /// <param name="book_old"></param>
    /// <param name="book_new"></param>
    /// <returns></returns>
    public List<(int index, OrderBook orderbook)> DiffOrderBook(List<OrderBook> book_old, List<OrderBook> book_new)
    {
        List<(int index, OrderBook orderbook)> diff = new List<(int index, OrderBook orderbook)>();
        for (int i = 0; i < book_old.Count; i++)
        {
            OrderBook item = book_old[i];
            OrderBook? book = book_new.FirstOrDefault(x => x.price == item.price);
            if (book == null)
            {
                diff.Add((-i - 1, new OrderBook()
                {
                    market = item.market,
                    symbol = item.symbol,
                    price = item.price,
                    amount = 0,
                    count = 0,
                    direction = item.direction,
                    last_time = DateTimeOffset.UtcNow,
                }));
            }
            else
            {
                diff.Add((book_new.IndexOf(book) + 1, new OrderBook()
                {
                    market = book.market,
                    symbol = book.symbol,
                    price = book.price,
                    amount = book.amount,
                    count = book.count,
                    direction = book.direction,
                    last_time = book.last_time,
                }));
            }
        }
        List<OrderBook> add = book_new.Where(P => !diff.Select(T => T.orderbook.price).Contains(P.price)).ToList();
        foreach (var item in add)
        {
            diff.Add((book_new.IndexOf(item) + 1, new OrderBook()
            {
                market = item.market,
                symbol = item.symbol,
                price = item.price,
                amount = item.amount,
                count = item.count,
                direction = item.direction,
                last_time = item.last_time,
            }));
        }
        if (diff.Count > 0)
        {
            if (diff[0].orderbook.direction == E_OrderSide.buy)
            {
                diff = diff.OrderByDescending(P => P.orderbook.price).ToList();
            }
            else if (diff[0].orderbook.direction == E_OrderSide.sell)
            {
                diff = diff.OrderBy(P => P.orderbook.price).ToList();
            }
        }
        return diff;
    }

}