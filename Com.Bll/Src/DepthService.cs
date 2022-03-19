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
    /// 更新Depth
    /// </summary>
    /// <param name="market"></param>
    /// <param name="symbol"></param>
    /// <param name="side"></param>
    /// <param name="price"></param>
    /// <param name="amount">正为增加,负为减少</param>
    /// <param name="amount">正为增加,负为减少</param>
    /// <param name="create_time">挂单时间</param>
    /// <returns></returns>
    public (BaseOrderBook depth, string json)? UpdateOrderBook(long market, string symbol, E_OrderSide side, decimal price, decimal amount, int count, DateTimeOffset create_time)
    {
        string key = FactoryService.instance.GetRedisDepth(market, side);
        StackExchange.Redis.RedisValue[] redisValues = FactoryService.instance.constant.redis.SortedSetRangeByScore(key, start: (double)price, stop: (double)price, take: 1);
        if (redisValues.Count() == 0)
        {
            BaseOrderBook orderBook = new BaseOrderBook();
            orderBook.market = market;
            orderBook.symbol = symbol;
            orderBook.price = price;
            orderBook.amount = amount;
            orderBook.count = count;
            orderBook.direction = side;
            orderBook.last_time = create_time;
            string json = JsonConvert.SerializeObject(orderBook);
            FactoryService.instance.constant.redis.SortedSetAdd(key, JsonConvert.SerializeObject(orderBook), (double)price);
            return (orderBook, json);
        }
        else
        {
            BaseOrderBook? temp = JsonConvert.DeserializeObject<BaseOrderBook>(redisValues.First());
            if (temp != null)
            {
                temp.amount += amount;
                temp.count += count;
                temp.last_time = create_time;
                string json = JsonConvert.SerializeObject(temp);
                FactoryService.instance.constant.redis.SortedSetAdd(key, json, (double)price, When.Exists);
                return (temp, json);
            }
        }
        return null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="depth"></param>
    public void Push(List<(BaseOrderBook depth, string json)> depth)
    {

    }



}