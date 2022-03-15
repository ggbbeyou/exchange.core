using Com.Db;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Com.Bll;

/// <summary>
/// Service:交易记录
/// </summary>
public class DealService
{
    /// <summary>
    /// 同步交易记录
    /// </summary>
    /// <param name="markets">交易对</param>
    /// <param name="span">最少同步多少时间数据</param>
    /// <returns></returns>
    public bool DealDbToRedis(long market, TimeSpan span)
    {
        DateTimeOffset start = DateTimeOffset.UtcNow.Add(span);
        Deal? deal = GetRedisLastDeal(market);
        if (deal != null)
        {
            start = deal.time;
        }
        List<Deal> deals = FactoryService.instance.deal_db.GetDeals(market, start, null);
        if (deals.Count() > 0)
        {
            SortedSetEntry[] entries = new SortedSetEntry[deals.Count()];
            for (int i = 0; i < deals.Count(); i++)
            {
                entries[i] = new SortedSetEntry(JsonConvert.SerializeObject(deals[i]), deals[i].time.ToUnixTimeMilliseconds());
            }
            FactoryService.instance.constant.redis.SortedSetAdd(FactoryService.instance.GetRedisDeal(market), entries);
        }
        return true;
    }

    /// <summary>
    /// 从redis获取最后一条交易记录
    /// </summary>
    /// <param name="market">交易对</param>
    /// <returns></returns>
    public Deal? GetRedisLastDeal(long market)
    {
        RedisValue[] redisvalue = FactoryService.instance.constant.redis.SortedSetRangeByRank(FactoryService.instance.GetRedisDeal(market), 0, 1, StackExchange.Redis.Order.Descending);
        if (redisvalue.Length > 0)
        {
            return JsonConvert.DeserializeObject<Deal>(redisvalue[0]);
        }
        return null;
    }

    /// <summary>
    /// 删除redis中的交易记录
    /// </summary>
    /// <param name="markets">交易对</param>
    /// <param name="end">start之前记录全部清除</param>
    public long DeleteDeal(long market, DateTimeOffset end)
    {
        return FactoryService.instance.constant.redis.SortedSetRemoveRangeByScore(FactoryService.instance.GetRedisDeal(market), 0, end.ToUnixTimeMilliseconds());
    }

}