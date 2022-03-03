using Com.Common;
using Com.Db;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Com.Bll;

/*


*/


/// <summary>
/// 交易记录
/// </summary>
public class DealService
{
    /// <summary>
    /// 单例类的实例
    /// </summary>
    /// <returns></returns>
    public static readonly DealService instance = new DealService();
    /// <summary>
    /// 常用接口
    /// </summary>
    private FactoryConstant constant = null!;
    /// <summary>
    /// redis(zset)键 已生成交易记录 deal:btc/usdt
    /// </summary>
    /// <value></value>
    public string redis_key_deal = "deal:{0}";
    /// <summary>
    /// 交易记录Db类
    /// </summary>
    public DealHelper dealHelper = null!;

    private DealService()
    {

    }


    /// <summary>
    ///
    /// </summary>
    /// <param name="configuration">配置接口</param>
    /// <param name="environment">环境接口</param>
    /// <param name="logger">日志接口</param>
    public void Init(FactoryConstant constant)
    {
        this.constant = constant;
        this.dealHelper = new DealHelper(constant);
    }

    /// <summary>
    /// 同步交易记录
    /// </summary>
    /// <param name="market"></param>
    /// <param name="span">最少同步多少时间数据</param>
    /// <returns></returns>
    public bool DealDbToRedis(List<string> markets, TimeSpan span)
    {
        foreach (var market in markets)
        {
            DateTimeOffset start = DateTimeOffset.UtcNow.Add(span);
            Deal? deal = GetRedisLastDeal(market);
            if (deal != null)
            {
                start = deal.time;
            }
            List<Deal> deals = dealHelper.GetDeals(market, start, null);
            if (deals.Count() > 0)
            {
                SortedSetEntry[] entries = new SortedSetEntry[deals.Count()];
                for (int i = 0; i < deals.Count(); i++)
                {
                    entries[i] = new SortedSetEntry(JsonConvert.SerializeObject(deals[i]), deals[i].time.ToUnixTimeMilliseconds());
                }
                this.constant.redis.SortedSetAdd(string.Format(this.redis_key_deal, market), entries);
            }
        }
        return true;
    }

    /// <summary>
    /// 从redis获取最后一条交易记录
    /// </summary>
    /// <param name="market">交易对</param>
    /// <returns></returns>
    public Deal? GetRedisLastDeal(string market)
    {
        RedisValue[] redisvalue = this.constant.redis.SortedSetRangeByRank(string.Format(this.redis_key_deal, market), 0, 1, StackExchange.Redis.Order.Descending);
        if (redisvalue.Length > 0)
        {
            return JsonConvert.DeserializeObject<Deal>(redisvalue[0]);
        }
        return null;
    }

    /// <summary>
    /// 删除redis中的交易记录
    /// </summary>
    /// <param name="markets"></param>
    /// <param name="start">start之前记录全部清除</param>
    public void DeleteDeal(List<string> markets, DateTimeOffset start)
    {
        foreach (var market in markets)
        {
            this.constant.redis.SortedSetRemoveRangeByScore(string.Format(this.redis_key_deal, market), 0, start.ToUnixTimeMilliseconds());
        }

    }





}