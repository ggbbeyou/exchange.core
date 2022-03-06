using Com.Bll;
using Com.Common;
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
        DealService.instance.Init(constant);
        KlineService.instance.Init(constant);
    }

    /// <summary>
    /// 初始化服务
    /// </summary>
    public Res<BaseMarketInfo> ServiceInit(BaseMarketInfo market)
    {
        Res<BaseMarketInfo> res = new Res<BaseMarketInfo>();
        //交易记录数据从DB同步到Redis 至少保存最近3个月记录
        DealService.instance.DeleteDeal(market.market, DateTimeOffset.UtcNow.AddMonths(-3));
        DealService.instance.DealDbToRedis(market.market, new TimeSpan(-30, 0, 0, 0));
        // K线数据从DB同步到Redis
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset end = now.AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond - 1);
        KlineService.instance.DBtoRedised(market.market, end);
        KlineService.instance.DBtoRedising(market.market);
        return res;
    }

    /// <summary>
    /// 启动服务
    /// </summary>
    /// <param name="market"></param>
    public Res<BaseMarketInfo> ServiceStart(BaseMarketInfo market)
    {
        Res<BaseMarketInfo> res = new Res<BaseMarketInfo>();
        if (!this.service.ContainsKey(market.market))
        {
            // this.service.Add(market.market, new Core(market, new MatchCore(market.market)));
        }
        // this.service[market.market].Start();
        return res;
    }

    /// <summary>
    /// 关闭服务
    /// </summary>
    /// <param name="market"></param>
    public Res<BaseMarketInfo> ServiceStop(BaseMarketInfo market)
    {
        Res<BaseMarketInfo> res = new Res<BaseMarketInfo>();
        if (!this.service.ContainsKey(market.market))
        {
            res.message = "未找到该服务";
            res.code = E_Res_Code.fail;
            return res;
        }
        // this.service[market.market].Stop();
        return res;
    }


}
