using Com.Bll;
using Com.Model;
using Com.Model.Enum;
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
    /// 常用接口
    /// </summary>
    public FactoryConstant constant = null!;
    /// <summary>
    /// 服务
    /// </summary>
    /// <typeparam name="string">交易对</typeparam>
    /// <typeparam name="Core">服务</typeparam>
    /// <returns></returns>
    public Dictionary<string, MatchModel> service = new Dictionary<string, MatchModel>();

    /// <summary>
    /// 私有构造方法
    /// </summary>
    private FactoryMatching()
    {
    }

    /// <summary>
    /// 初始化方法
    /// </summary>
    /// <param name="constant"></param>
    public void Init(FactoryConstant constant)
    {
        this.constant = constant;
    }

    /// <summary>
    /// 初始化服务
    /// </summary>
    public BaseMarketInfo ServiceInit(BaseMarketInfo info)
    {
        //交易记录数据从DB同步到Redis 至少保存最近3个月记录
        long delete = FactoryService.instance.deal_service.DeleteDeal(info.market, DateTimeOffset.UtcNow.AddMonths(-3));
        FactoryService.instance.deal_service.DealDbToRedis(info.market, new TimeSpan(-30, 0, 0, 0));
        // K线数据从DB同步到Redis
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset end = now.AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond - 1);
        FactoryService.instance.kline_service.DBtoRedised(info.market, end);
        FactoryService.instance.kline_service.DBtoRedising(info.market);
        return info;
    }

    /// <summary>
    /// 启动服务
    /// </summary>
    /// <param name="info"></param>
    public BaseMarketInfo ServiceStart(BaseMarketInfo info)
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
    /// 关闭服务
    /// </summary>
    /// <param name="info"></param>
    public BaseMarketInfo ServiceStop(BaseMarketInfo info)
    {
        if (!this.service.ContainsKey(info.market))
        {
            throw new Exception("未找到该服务");
        }
        this.service[info.market].run = false;
        return info;
    }


}
