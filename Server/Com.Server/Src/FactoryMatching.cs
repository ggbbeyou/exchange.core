using Com.Bll;
using Com.Common;
using Com.Model;
using Com.Model.Enum;

namespace Com.Server;

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
    public Dictionary<string, Core> service = new Dictionary<string, Core>();

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
    /// 交易记录数据从DB同步到Redis 至少保存最近3个月记录
    /// </summary>
    public Res<BaseMarketInfo> DealDbToRedis(BaseMarketInfo markets)
    {
        Res<BaseMarketInfo> res = new Res<BaseMarketInfo>();
        DealService.instance.DeleteDeal(markets.market, DateTimeOffset.UtcNow.AddMonths(-3));
        DealService.instance.DealDbToRedis(markets.market, new TimeSpan(-30, 0, 0, 0));
        return res;
    }

    /// <summary>
    /// K线数据从DB同步到Redis
    /// </summary>
    public Res<BaseMarketInfo> KlindDBtoRedis(BaseMarketInfo markets)
    {
        Res<BaseMarketInfo> res = new Res<BaseMarketInfo>();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset end = now.AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond - 1);
        KlineService.instance.DBtoRedised(markets.market, end);
        KlineService.instance.DBtoRedising(markets.market);
        return res;
    }

    /// <summary>
    /// 启动服务
    /// </summary>
    /// <param name="markets"></param>
    public Res<BaseMarketInfo> StartService(BaseMarketInfo markets)
    {
        Res<BaseMarketInfo> res = new Res<BaseMarketInfo>();
        if (!this.service.ContainsKey(markets.market))
        {
            this.service.Add(markets.market, new Core(markets.market, this.constant));
        }
        this.service[markets.market].Start();
        return res;
    }

    /// <summary>
    /// 关闭服务
    /// </summary>
    /// <param name="markets"></param>
    public Res<BaseMarketInfo> StopService(BaseMarketInfo markets)
    {
        Res<BaseMarketInfo> res = new Res<BaseMarketInfo>();
        if (!this.service.ContainsKey(markets.market))
        {
            res.message = "未找到该服务";
            res.code = E_Res_Code.fail;
            return res;
        }
        this.service[markets.market].Stop();
        return res;
    }


}
