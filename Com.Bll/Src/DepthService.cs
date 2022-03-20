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
    /// 数据库
    /// </summary>
    public DbContextEF db = null!;

    /// <summary>
    /// 初始化
    /// </summary>
    public DepthService()
    {
        var scope = FactoryService.instance.constant.provider.CreateScope();
        this.db = scope.ServiceProvider.GetService<DbContextEF>()!;
    }



    /// <summary>
    /// 转换深度行情
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
        // depths.Add(E_WebsockerChannel.books10_inc, new Depth());
        // depths.Add(E_WebsockerChannel.books50_inc, new Depth());
        // depths.Add(E_WebsockerChannel.books200_inc, new Depth());
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
                case E_WebsockerChannel.books10_inc:

                    break;
                case E_WebsockerChannel.books50_inc:

                    break;
                case E_WebsockerChannel.books200_inc:

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
    /// 深度行情保存到redis并且推送到MQ
    /// </summary>
    /// <param name="depth"></param>
    public void Push(Dictionary<E_WebsockerChannel, Depth> depths)
    {
        foreach (var item in depths)
        {
            FactoryService.instance.constant.redis.HashSet(FactoryService.instance.GetRedisDepth(item.Value.market), item.Key.ToString(), JsonConvert.SerializeObject(item.Value));
            FactoryService.instance.constant.MqPublish(FactoryService.instance.GetMqSubscribe(item.Key, item.Value.market), JsonConvert.SerializeObject(item.Value));
        }
    }

}