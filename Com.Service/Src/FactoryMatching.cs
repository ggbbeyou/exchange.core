using Com.Api.Sdk.Enum;
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
    /// Service:交易记录
    /// </summary>
    public ServiceDeal deal_service = new ServiceDeal();
    /// <summary>
    /// Service:K线
    /// </summary>
    public ServiceKline kline_service = new ServiceKline();
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
    /// 服务:获取服务状态
    /// </summary>
    /// <param name="marketInfo"></param>
    /// <returns></returns>
    public Market ServiceGetStatus(Market info)
    {
        if (this.service.ContainsKey(info.market))
        {
            if (this.service[info.market].run)
            {
                info.status = 3;
            }
            else
            {
                info.status = 4;
            }
        }
        else
        {
            info.status = 4;
        }
        this.service[info.market].run = true;
        return info;
    }

    /// <summary>
    /// 服务:清除所有缓存
    /// </summary>
    /// <param name="marketInfo"></param>
    /// <returns></returns>
    public Market ServiceClearCache(Market info)
    {
        FactoryService.instance.constant.i_model.ExchangeDelete(FactoryService.instance.GetMqSubscribe(E_WebsockerChannel.min1, info.market), false);
        FactoryService.instance.constant.i_model.ExchangeDelete(FactoryService.instance.GetMqSubscribe(E_WebsockerChannel.min5, info.market), false);
        FactoryService.instance.constant.i_model.ExchangeDelete(FactoryService.instance.GetMqSubscribe(E_WebsockerChannel.min15, info.market), false);
        FactoryService.instance.constant.i_model.ExchangeDelete(FactoryService.instance.GetMqSubscribe(E_WebsockerChannel.min30, info.market), false);
        FactoryService.instance.constant.i_model.ExchangeDelete(FactoryService.instance.GetMqSubscribe(E_WebsockerChannel.hour1, info.market), false);
        FactoryService.instance.constant.i_model.ExchangeDelete(FactoryService.instance.GetMqSubscribe(E_WebsockerChannel.hour6, info.market), false);
        FactoryService.instance.constant.i_model.ExchangeDelete(FactoryService.instance.GetMqSubscribe(E_WebsockerChannel.hour12, info.market), false);
        FactoryService.instance.constant.i_model.ExchangeDelete(FactoryService.instance.GetMqSubscribe(E_WebsockerChannel.day1, info.market), false);
        FactoryService.instance.constant.i_model.ExchangeDelete(FactoryService.instance.GetMqSubscribe(E_WebsockerChannel.week1, info.market), false);
        FactoryService.instance.constant.i_model.ExchangeDelete(FactoryService.instance.GetMqSubscribe(E_WebsockerChannel.month1, info.market), false);
        FactoryService.instance.constant.i_model.ExchangeDelete(FactoryService.instance.GetMqSubscribe(E_WebsockerChannel.tickers, info.market), false);
        FactoryService.instance.constant.i_model.ExchangeDelete(FactoryService.instance.GetMqSubscribe(E_WebsockerChannel.trades, info.market), false);
        FactoryService.instance.constant.i_model.ExchangeDelete(FactoryService.instance.GetMqSubscribe(E_WebsockerChannel.books10, info.market), false);
        FactoryService.instance.constant.i_model.ExchangeDelete(FactoryService.instance.GetMqSubscribe(E_WebsockerChannel.books50, info.market), false);
        FactoryService.instance.constant.i_model.ExchangeDelete(FactoryService.instance.GetMqSubscribe(E_WebsockerChannel.books200, info.market), false);
        FactoryService.instance.constant.i_model.ExchangeDelete(FactoryService.instance.GetMqSubscribe(E_WebsockerChannel.books10_inc, info.market), false);
        FactoryService.instance.constant.i_model.ExchangeDelete(FactoryService.instance.GetMqSubscribe(E_WebsockerChannel.books50_inc, info.market), false);
        FactoryService.instance.constant.i_model.ExchangeDelete(FactoryService.instance.GetMqSubscribe(E_WebsockerChannel.books200_inc, info.market), false);
        FactoryService.instance.constant.i_model.QueueDelete(FactoryService.instance.GetMqOrderPlace(info.market), false, false);
        FactoryService.instance.constant.i_model.QueueDelete(FactoryService.instance.GetMqOrderCancel(info.market), false, false);
        FactoryService.instance.constant.i_model.QueueDelete(FactoryService.instance.GetMqOrderDeal(info.market), false, false);
        //交易记录数据从DB同步到Redis 至少保存最近3个月记录
        long delete = this.deal_service.DeleteDeal(info.market, DateTimeOffset.UtcNow.AddMonths(-2));
        ServiceDepth.instance.DeleteRedisDepth(info.market);
        kline_service.DeleteRedisKline(info.market);
        return info;
    }

    /// <summary>
    /// 服务:预热缓存
    /// </summary>
    public Market ServiceWarmCache(Market info)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        now = now.AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond);
        this.deal_service.DealDbToRedis(info.market, now.AddMonths(-2));
        DateTimeOffset end = now.AddMilliseconds(-1);
        this.kline_service.DBtoRedised(info.market, info.symbol, end);
        this.kline_service.DBtoRedising(info.market, info.symbol);

        return info;
    }

    /// <summary>
    /// 服务:启动服务
    /// </summary>
    /// <param name="info"></param>
    public Market ServiceStart(Market info)
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
    public Market ServiceStop(Market info)
    {
        if (this.service.ContainsKey(info.market))
        {
            this.service[info.market].run = false;
        }
        return info;
    }


}
