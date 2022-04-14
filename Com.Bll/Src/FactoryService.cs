using System.Text;
using Com.Db;
using Com.Api.Sdk.Enum;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
    /// 系统初始化时间  初始化  注:2017-1-1 此时是一年第一天，一年第一月，一年第一个星期日(星期日是一个星期开始的第一天)
    /// </summary>   
    public DateTimeOffset system_init = new DateTimeOffset(2017, 1, 1, 0, 0, 0, TimeSpan.Zero);

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

    }

    /// <summary>
    /// redis(zset)键 已生成交易记录 交易时间=>deal:btc/usdt 
    /// </summary>
    /// <param name="market"></param>
    /// <returns></returns>
    public string GetRedisDeal(long market)
    {
        return string.Format("deal:{0}", market);
    }

    /// <summary>
    /// redis hast 深度行情 depth:{market}
    /// </summary>
    /// <param name="market">交易对</param>
    /// <returns></returns>
    public string GetRedisDepth(long market)
    {
        return string.Format("depth:{0}", market);
    }

    /// <summary>
    /// redis hast 深度行情 depth
    /// </summary>
    /// <returns></returns>
    public string GetRedisTicker()
    {
        return string.Format("ticker");
    }

    /// <summary>
    /// redis(zset)键 已生成K线 K线开始时间=>kline:btc/usdt:main1
    /// </summary>
    /// <param name="market"></param>
    /// <returns></returns>
    public string GetRedisKline(long market, E_KlineType type)
    {
        return string.Format("kline:{0}:{1}", market, type);
    }

    /// <summary>
    /// redis(hash)键 正在生成K线 K线类型=>klineing:btc/usdt
    /// </summary>
    /// <param name="market"></param>
    /// <returns></returns>
    public string GetRedisKlineing(long market)
    {
        return string.Format("klineing:{0}", market);
    }

    /// <summary>
    /// 处理进程
    /// </summary>
    /// <returns></returns>
    public string GetRedisProcess()
    {
        return string.Format("process");
    }

    /// <summary>
    /// 验证码
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public string GetRedisVerificationCode(long id)
    {
        return string.Format("verification_code:{0}", id);
    }

    /// <summary>
    /// 用户api账户信息(注意,当修改时记得删除redis数据)
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public string GetRedisApiKey()
    {
        return string.Format("api_key");
    }

    /// <summary>
    /// MQ:发送历史成交记录
    /// </summary>
    /// <param name="market"></param>
    /// <returns></returns>
    public string GetMqOrderDeal(long market)
    {
        return string.Format("deal_{0}", market);
    }

    /// <summary>
    /// MQ:挂单队列
    /// </summary>
    /// <param name="market"></param>
    /// <returns></returns>
    public string GetMqOrderPlace(long market)
    {
        return string.Format("order_place_{0}", market);
    }

    /// <summary>
    /// MQ:订阅
    /// </summary>
    /// <param name="channel">订阅频道</param>
    /// <param name="data">不需要登录:交易对id,需要登录:用户id</param>
    /// <returns></returns>
    public string GetMqSubscribe(E_WebsockerChannel channel, long data)
    {
        return string.Format("{0}_{1}", channel, data);
    }

}