using Com.Db;
using Com.Model.Enum;
using Newtonsoft.Json;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace Com.Bll;

/// <summary>
/// 服务工厂
/// </summary>
public class FactoryService
{
    /// <summary>
    /// 单例类的实例
    /// </summary>
    /// <returns></returns>
    public static readonly FactoryService instance = new FactoryService();
    /// <summary>
    /// 常用接口
    /// </summary>
    public FactoryConstant constant = null!;
    /// <summary>
    /// DB:交易记录
    /// </summary>
    public DealDb deal_db = null!;
    /// <summary>
    /// DB:K线
    /// </summary>
    public KilneDb kilne_db = null!;
    /// <summary>
    /// Service:订单
    /// </summary>
    public OrderService order_service = null!;
    /// <summary>
    /// Service:交易记录
    /// </summary>
    public DealService deal_service = null!;
    /// <summary>
    /// Service:K线
    /// </summary>
    public KlineService kline_service = null!;
    /// <summary>
    /// 系统初始化时间  初始化  注:2017-1-1 此时是一年第一天，一年第一月，一年第一个星期日(星期日是一个星期开始的第一天)
    /// </summary>   
    public DateTimeOffset system_init = new DateTimeOffset(2017, 1, 1, 0, 0, 0, TimeSpan.Zero);
    /// <summary>
    /// MQ基本属性
    /// </summary>
    /// <returns></returns>
    public IBasicProperties props = null!;


    /// <summary>
    /// private构造方法
    /// </summary>
    private FactoryService()
    {

    }

    /// <summary>
    /// 初始化方法
    /// </summary>
    /// <param name="constant"></param>
    public void Init(FactoryConstant constant)
    {
        this.constant = constant;
        this.props = constant.i_model.CreateBasicProperties();
        this.props.DeliveryMode = 2;
        this.deal_db = new DealDb();
        this.kilne_db = new KilneDb();
        this.order_service = new OrderService();
        this.deal_service = new DealService();
        this.kline_service = new KlineService();
    }

    /// <summary>
    /// redis(zset)键 已生成交易记录 交易时间=>deal:btc/usdt 
    /// </summary>
    /// <param name="market"></param>
    /// <returns></returns>
    public string GetRedisDeal(string market)
    {
        return string.Format("deal:{0}", market);
    }

    /// <summary>
    /// redis(zset)键 已生成K线 K线开始时间=>kline:btc/usdt:main1
    /// </summary>
    /// <param name="market"></param>
    /// <returns></returns>
    public string GetRedisKline(string market, E_KlineType type)
    {
        return string.Format("kline:{0}:{1}", market, type.ToString());
    }

    /// <summary>
    /// redis(hash)键 正在生成K线 K线类型=>klineing:btc/usdt
    /// </summary>
    /// <param name="market"></param>
    /// <returns></returns>
    public string GetRedisKlineing(string market)
    {
        return string.Format("klineing:{0}", market);
    }

    /// <summary>
    /// MQ:下单队列
    /// </summary>
    /// <param name="market"></param>
    /// <returns></returns>
    public string GetMqOrderPlace(string market)
    {
        return string.Format("order_place_{0}", market);
    }

    /// <summary>
    /// MQ:订阅深度
    /// </summary>
    /// <param name="market"></param>
    /// <returns></returns>
    public string GetMqSubscribeDepth(string market)
    {
        return string.Format("subscribe_depth_{0}", market);
    }

    /// <summary>
    /// MQ:订阅K线
    /// </summary>
    /// <param name="market"></param>
    /// <returns></returns>
    public string GetMqSubscribeKline(string market)
    {
        return string.Format("subscribe_kline_{0}", market);
    }

}