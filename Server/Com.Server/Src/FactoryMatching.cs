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

            KlineService.instance.Init(constant, systemTime);

            // DateTimeOffset max = klindService.GetRedisMaxMinuteKline("btc/usdt", E_KlineType.min1);
        }

        public void DBtoRedis()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset end = now.AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond - 1);
            KlineService.instance.DBtoRedised(new List<string>() { "btc/usdt" }, end);
            KlineService.instance.DBtoRedising(new List<string>() { "btc/usdt" }, end);
        }



    }
}