using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Com.Bll;
using Com.Common;
using Com.Model.Enum;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

// using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Com.Server
{
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


        DateTimeOffset systemTime = DateTimeOffset.Now;


        /// <summary>
        /// 私有构造方法
        /// </summary>
        private FactoryMatching()
        {
        }

        /// <summary>
        /// 初始化方法
        /// </summary>
        /// <param name="configuration">配置接口</param>
        /// <param name="logger">日志接口</param>
        public void Init(FactoryConstant constant)
        {
            this.constant = constant;
            this.systemTime = new DateTimeOffset(2017, 1, 1, 0, 0, 0, TimeSpan.Zero);
        }

        /// <summary>
        /// 交易记录数据从DB同步到Redis 至少保存最近3个月记录
        /// </summary>
        public void DealDbToRedis()
        {
            List<string> markets = new List<string>();
            markets.Add("btc/usdt");
            DealService.instance.Init(constant, systemTime);
            DealService.instance.DeleteDeal(markets, DateTimeOffset.UtcNow.AddMonths(-3));
            DealService.instance.DealDbToRedis(markets, new TimeSpan(-30, 0, 0, 0));
        }

        /// <summary>
        /// K线数据从DB同步到Redis
        /// </summary>
        public void KlindDBtoRedis()
        {
            List<string> markets = new List<string>();
            markets.Add("btc/usdt");
            KlineService.instance.Init(constant, systemTime);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset end = now.AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond - 1);
            KlineService.instance.DBtoRedised(markets, end);
            KlineService.instance.DBtoRedising(markets);
        }



    }
}