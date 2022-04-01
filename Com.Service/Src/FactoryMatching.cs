using System.Diagnostics;
using Com.Api.Sdk.Enum;
using Com.Bll;
using Com.Db;
using Com.Service.Match;
using Com.Service.Models;
using Microsoft.Extensions.Logging;

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
    /// Service:订单
    /// </summary>
    public ServiceOrder order_service = new ServiceOrder();
    /// <summary>
    /// 服务
    /// </summary>
    /// <typeparam name="string">交易对</typeparam>
    /// <typeparam name="Core">服务</typeparam>
    /// <returns></returns>
    public Dictionary<long, MatchModel> service = new Dictionary<long, MatchModel>();
    /// <summary>
    /// 互斥锁
    /// </summary>
    /// <returns></returns>
    private Mutex mutex = new Mutex(false);
    /// <summary>
    /// 秒表
    /// </summary>
    /// <returns></returns>
    public Stopwatch stopwatch = new Stopwatch();

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
            info.status = this.service[info.market].run;
        }
        else
        {
            info.status = false;
        }
        return info;
    }

    /// <summary>
    /// 服务:启动服务
    /// </summary>
    /// <param name="info"></param>
    public Market ServiceStart(Market info)
    {
        FactoryService.instance.constant.logger.LogInformation($"服务准备启动:{info.market}");
        this.mutex.WaitOne();
        if (!this.service.ContainsKey(info.market))
        {
            MatchModel mm = new MatchModel(info);
            mm.match_core = new MatchCore(mm);
            mm.mq = new MQ(mm);
            mm.core = new Core(mm);
            this.service.Add(info.market, mm);
        }
        MatchModel model = this.service[info.market];
        if (model.run == false)
        {
            ServiceClearCache(info);
            ServiceWarmCache(info);
            model.run = true;
            (string queue_name, string consume_tag) order_cancel = this.service[info.market].mq.OrderCancel();
            if (!model.mq_queues.Contains(order_cancel.queue_name))
            {
                model.mq_queues.Add(order_cancel.queue_name);
            }
            if (!model.mq_consumer.Contains(order_cancel.consume_tag))
            {
                model.mq_consumer.Add(order_cancel.consume_tag);
            }
            (string queue_name, string consume_tag) order_receive = this.service[info.market].mq.OrderReceive();
            if (!model.mq_queues.Contains(order_receive.queue_name))
            {
                model.mq_queues.Add(order_receive.queue_name);
            }
            if (!model.mq_consumer.Contains(order_receive.consume_tag))
            {
                model.mq_consumer.Add(order_receive.consume_tag);
            }
        }
        info.status = model.run;
        this.mutex.ReleaseMutex();
        FactoryService.instance.constant.logger.LogInformation(model.eventId, $"服务启动成功:{info.market}");
        return info;
    }

    /// <summary>
    /// 服务:关闭服务
    /// </summary>
    /// <param name="info"></param>
    public Market ServiceStop(Market info)
    {
        FactoryService.instance.constant.logger.LogInformation($"服务准备关闭:{info.market}");
        this.mutex.WaitOne();
        if (this.service.ContainsKey(info.market))
        {
            MatchModel mm = this.service[info.market];
            mm.run = false;
            ServiceClearCache(info);
            this.service.Remove(info.market);
            info.status = false;
        }
        else
        {
            info.status = false;
        }
        this.mutex.ReleaseMutex();
        FactoryService.instance.constant.logger.LogInformation($"服务关闭成功:{info.market}");
        return info;
    }

    /// <summary>
    /// 服务:清除所有缓存
    /// </summary>
    /// <param name="marketInfo"></param>
    /// <returns></returns>
    private bool ServiceClearCache(Market info)
    {
        if (this.service.ContainsKey(info.market))
        {
            MatchModel mm = this.service[info.market];
            foreach (var item in mm.mq_consumer)
            {
                FactoryService.instance.constant.MqDeleteConsumer(item);
            }
            mm.mq_consumer.Clear();
            foreach (var item in mm.mq_queues)
            {
                FactoryService.instance.constant.MqDeletePurge(item);
            }
            mm.mq_queues.Clear();
            mm.mq.DepthChange(new List<Orders>(), new List<Deal>(), mm.match_core.CancelOrder());
        }
        //交易记录数据从DB同步到Redis 至少保存最近3个月记录
        FactoryService.instance.constant.stopwatch.Restart();
        long delete = this.deal_service.DeleteDeal(info.market, DateTimeOffset.UtcNow.AddMonths(-2));
        ServiceDepth.instance.DeleteRedisDepth(info.market);
        kline_service.DeleteRedisKline(info.market);
        FactoryService.instance.constant.stopwatch.Stop();
        FactoryService.instance.constant.logger.LogTrace($"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};{info.symbol}:redis=>清除所有缓存");
        return true;
    }

    /// <summary>
    /// 服务:预热缓存
    /// </summary>
    private bool ServiceWarmCache(Market info)
    {
        FactoryService.instance.constant.stopwatch.Restart();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        now = now.AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond);
        this.deal_service.DealDbToRedis(info.market, now.AddMonths(-2));
        DateTimeOffset end = now.AddMilliseconds(-1);
        this.kline_service.DBtoRedised(info.market, info.symbol, end);
        this.kline_service.DBtoRedising(info.market, info.symbol);
        order_service.PushOrderToMqRedis(info.market);
        FactoryService.instance.constant.stopwatch.Stop();
        FactoryService.instance.constant.logger.LogTrace($"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};{info.symbol}:redis=>预热缓存");
        return true;
    }




}
