using Com.Bll;
using Com.Db;
using Com.Service.Match;
using Com.Service.Models;

namespace Com.Service;

/// <summary>
/// 工厂
/// </summary>
public class FactoryMatching
{
    /// <summary>
    /// 单例类的实例
    /// </summary>
    /// <returns></returns>
    public static readonly FactoryMatching instance = new FactoryMatching();   
    /// <summary>
    /// 服务
    /// </summary>
    /// <typeparam name="string">交易对</typeparam>
    /// <typeparam name="Core">服务</typeparam>
    /// <returns></returns>
    public Dictionary<long, MatchModel> service = new Dictionary<long, MatchModel>();

    /// <summary>
    /// 私有构造方法
    /// </summary>
    private FactoryMatching()
    {
    }    

    /// <summary>
    /// 服务:清除所有缓存
    /// </summary>
    /// <param name="marketInfo"></param>
    /// <returns></returns>
    public MarketInfo ServiceClearCache(MarketInfo info)
    {
        //交易记录数据从DB同步到Redis 至少保存最近3个月记录
        long delete = FactoryService.instance.deal_service.DeleteDeal(info.market, DateTimeOffset.UtcNow.AddMonths(-2));
        return info;
    }

    /// <summary>
    /// 服务:预热缓存
    /// </summary>
    public MarketInfo ServiceWarmCache(MarketInfo info)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        now = now.AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond);
        FactoryService.instance.deal_service.DealDbToRedis(info.market, now.AddMonths(-2));
        DateTimeOffset end = now.AddMilliseconds(-1);
        FactoryService.instance.kline_service.DBtoRedised(info.market, end);
        FactoryService.instance.kline_service.DBtoRedising(info.market);
        return info;
    }

    /// <summary>
    /// 服务:启动服务
    /// </summary>
    /// <param name="info"></param>
    public MarketInfo ServiceStart(MarketInfo info)
    {
        if (!this.service.ContainsKey(info.market))
        {
            MatchModel model = new MatchModel(info);
            model.match_core = new MatchCore(model);
            model.mq = new MQ(model);
            model.core = new Core(model);
            this.service.Add(info.market, model);
        }
        this.service[info.market].run = true;
        return info;
    }

    /// <summary>
    /// 服务:关闭服务
    /// </summary>
    /// <param name="info"></param>
    public MarketInfo ServiceStop(MarketInfo info)
    {
        if (this.service.ContainsKey(info.market))
        {
            this.service[info.market].run = false;
        }
        return info;
    }


}
