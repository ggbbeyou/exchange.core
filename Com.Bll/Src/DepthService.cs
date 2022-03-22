using Com.Db;
using Com.Db.Enum;
using Com.Db.Model;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Com.Bll;

/// <summary>
/// Service:深度行情
/// </summary>
public class DepthService
{
    /// <summary>
    /// 单例类的实例
    /// </summary>
    /// <returns></returns>
    public static readonly DepthService instance = new DepthService();

    /// <summary>
    /// 初始化
    /// </summary>
    private DepthService()
    {
    }

    /// <summary>
    /// (全部)转换深度行情
    /// </summary>
    /// <param name="bid"></param>
    /// <param name="orderbook"></param>
    /// <returns></returns>
    public Dictionary<E_WebsockerChannel, Depth> ConvertDepth(long market, string symbol, (List<BaseOrderBook> bid, List<BaseOrderBook> ask) orderbook)
    {
        Dictionary<E_WebsockerChannel, Depth> depths = new Dictionary<E_WebsockerChannel, Depth>();
        depths.Add(E_WebsockerChannel.books10, new Depth());
        depths.Add(E_WebsockerChannel.books50, new Depth());
        depths.Add(E_WebsockerChannel.books200, new Depth());
        foreach (var item in depths)
        {
            item.Value.market = market;
            item.Value.symbol = symbol;
            item.Value.timestamp = DateTimeOffset.UtcNow;
            switch (item.Key)
            {
                case E_WebsockerChannel.books10:
                    item.Value.bid = new decimal[orderbook.bid.Count < 10 ? orderbook.bid.Count : 10, 2];
                    item.Value.ask = new decimal[orderbook.ask.Count < 10 ? orderbook.ask.Count : 10, 2];
                    break;
                case E_WebsockerChannel.books50:
                    item.Value.bid = new decimal[orderbook.bid.Count < 50 ? orderbook.bid.Count : 50, 2];
                    item.Value.ask = new decimal[orderbook.ask.Count < 50 ? orderbook.ask.Count : 50, 2];
                    break;
                case E_WebsockerChannel.books200:
                    item.Value.bid = new decimal[orderbook.bid.Count < 200 ? orderbook.bid.Count : 200, 2];
                    item.Value.ask = new decimal[orderbook.ask.Count < 200 ? orderbook.ask.Count : 200, 2];
                    break;
                default:
                    break;
            }
            decimal total_bid = 0;
            decimal total_ask = 0;
            for (int i = 0; i < item.Value.bid.GetLength(0); i++)
            {
                item.Value.bid[i, 0] = orderbook.bid[i].price;
                item.Value.bid[i, 1] = orderbook.bid[i].amount;
                total_bid += orderbook.bid[i].amount * orderbook.bid[i].price;
            }
            item.Value.total_bid = total_bid;
            for (int i = 0; i < item.Value.ask.GetLength(0); i++)
            {
                item.Value.ask[i, 0] = orderbook.ask[i].price;
                item.Value.ask[i, 1] = orderbook.ask[i].amount;
                total_ask += orderbook.ask[i].amount * orderbook.ask[i].price;
            }
            item.Value.total_ask = total_ask;
        }
        return depths;
    }

    /// <summary>
    /// (部分)转换深度行情
    /// </summary>
    /// <param name="bid"></param>
    /// <param name="orderbook"></param>
    /// <returns></returns>
    public Dictionary<E_WebsockerChannel, Depth> ConvertDepth(long market, string symbol, (List<(int index, BaseOrderBook orderbook)> bid, List<(int index, BaseOrderBook orderbook)> ask) orderbook)
    {
        Dictionary<E_WebsockerChannel, Depth> depths = new Dictionary<E_WebsockerChannel, Depth>();
        depths.Add(E_WebsockerChannel.books10_inc, new Depth());
        depths.Add(E_WebsockerChannel.books50_inc, new Depth());
        depths.Add(E_WebsockerChannel.books200_inc, new Depth());
        foreach (var item in depths)
        {
            item.Value.market = market;
            item.Value.symbol = symbol;
            item.Value.timestamp = DateTimeOffset.UtcNow;
            switch (item.Key)
            {
                case E_WebsockerChannel.books10_inc:
                    item.Value.bid = new decimal[orderbook.bid.Count(P => Math.Abs(P.index) <= 10), 2];
                    item.Value.ask = new decimal[orderbook.ask.Count(P => Math.Abs(P.index) <= 10), 2];
                    break;
                case E_WebsockerChannel.books50_inc:
                    item.Value.bid = new decimal[orderbook.bid.Count(P => Math.Abs(P.index) <= 50), 2];
                    item.Value.ask = new decimal[orderbook.ask.Count(P => Math.Abs(P.index) <= 50), 2];
                    break;
                case E_WebsockerChannel.books200_inc:
                    item.Value.bid = new decimal[orderbook.bid.Count(P => Math.Abs(P.index) <= 200), 2];
                    item.Value.ask = new decimal[orderbook.ask.Count(P => Math.Abs(P.index) <= 200), 2];
                    break;
                default:
                    break;
            }
            for (int i = 0; i < item.Value.bid.GetLength(0); i++)
            {
                item.Value.bid[i, 0] = orderbook.bid[i].orderbook.price;
                item.Value.bid[i, 1] = orderbook.bid[i].orderbook.amount;
            }
            for (int i = 0; i < item.Value.ask.GetLength(0); i++)
            {
                item.Value.ask[i, 0] = orderbook.ask[i].orderbook.price;
                item.Value.ask[i, 1] = orderbook.ask[i].orderbook.amount;
            }
        }
        return depths;
    }

    /// <summary>
    /// (全部)深度行情保存到redis并且推送到MQ
    /// </summary>
    /// <param name="depth"></param>
    public void Push(Dictionary<E_WebsockerChannel, Depth> depths)
    {
        ResWebsocker<Depth> resWebsocker = new ResWebsocker<Depth>();
        resWebsocker.success = true;
        resWebsocker.op = E_WebsockerOp.subscribe_date;
        foreach (var item in depths)
        {
            FactoryService.instance.constant.redis.HashSet(FactoryService.instance.GetRedisDepth(item.Value.market), item.Key.ToString(), JsonConvert.SerializeObject(item.Value));
            resWebsocker.channel = item.Key;
            resWebsocker.data = item.Value;
            FactoryService.instance.constant.MqPublish(FactoryService.instance.GetMqSubscribe(item.Key, item.Value.market), JsonConvert.SerializeObject(resWebsocker));
        }
    }

    /// <summary>
    /// (部分)深度行情保存到redis并且推送到MQ
    /// </summary>
    /// <param name="depth"></param>
    public void PushDiff(Dictionary<E_WebsockerChannel, Depth> depths)
    {
        ResWebsocker<Depth> resWebsocker = new ResWebsocker<Depth>();
        resWebsocker.success = true;
        resWebsocker.op = E_WebsockerOp.subscribe_date;
        foreach (var item in depths)
        {
            resWebsocker.channel = item.Key;
            resWebsocker.data = item.Value;
            FactoryService.instance.constant.MqPublish(FactoryService.instance.GetMqSubscribe(item.Key, item.Value.market), JsonConvert.SerializeObject(resWebsocker));
        }
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
    public (List<(int, BaseOrderBook)> bid, List<(int, BaseOrderBook)> ask) DiffOrderBook((List<BaseOrderBook> bid, List<BaseOrderBook> ask) book_old, (List<BaseOrderBook> bid, List<BaseOrderBook> ask) book_new)
    {
        List<(int index, BaseOrderBook orderbook)> bid_diff = new List<(int index, BaseOrderBook orderbook)>();
        List<(int index, BaseOrderBook orderbook)> ask_diff = new List<(int index, BaseOrderBook orderbook)>();
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
        bid_diff = bid_diff.OrderBy(P => P.index).ToList();
        ask_diff = ask_diff.OrderBy(P => P.index).ToList();
        return (bid_diff, ask_diff);
    }

    /// <summary>
    /// (部分)差异
    /// </summary>
    /// <param name="book_old"></param>
    /// <param name="book_new"></param>
    /// <returns></returns>
    public List<(int index, BaseOrderBook orderbook)> DiffOrderBook(List<BaseOrderBook> book_old, List<BaseOrderBook> book_new)
    {
        List<(int index, BaseOrderBook orderbook)> diff = new List<(int index, BaseOrderBook orderbook)>();
        for (int i = 0; i < book_old.Count; i++)
        {
            BaseOrderBook item = book_old[i];
            BaseOrderBook? book = book_new.FirstOrDefault(x => x.price == item.price);
            if (book == null)
            {
                diff.Add((-i - 1, new BaseOrderBook()
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
                diff.Add((book_new.IndexOf(book) + 1, new BaseOrderBook()
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
        List<BaseOrderBook> add = book_new.Where(P => !diff.Select(T => T.orderbook.price).Contains(P.price)).ToList();
        foreach (var item in add)
        {
            diff.Add((book_new.IndexOf(item) + 1, new BaseOrderBook()
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