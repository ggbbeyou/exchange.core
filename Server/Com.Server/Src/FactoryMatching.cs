using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Com.Bll;
using Com.Common;
using Com.Model;
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
        public void DealDbToRedis()
        {
            List<BaseMarketInfo> marketInfos = GetMarkets();
            foreach (var markets in marketInfos)
            {
                DealService.instance.DeleteDeal(markets.market, DateTimeOffset.UtcNow.AddMonths(-3));
                DealService.instance.DealDbToRedis(markets.market, new TimeSpan(-30, 0, 0, 0));
            }
        }

        /// <summary>
        /// K线数据从DB同步到Redis
        /// </summary>
        public void KlindDBtoRedis()
        {
            List<BaseMarketInfo> marketInfos = GetMarkets();
            foreach (var markets in marketInfos)
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset end = now.AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond - 1);
                KlineService.instance.DBtoRedised(markets.market, end);
                KlineService.instance.DBtoRedising(markets.market);
            }
        }

        /// <summary>
        /// 获取交易对基本信息
        /// </summary>
        /// <returns></returns>
        public List<BaseMarketInfo> GetMarkets()
        {
            List<BaseMarketInfo> result = new List<BaseMarketInfo>();
            result.Add(new BaseMarketInfo { market = "btc/usdt" });
            result.Add(new BaseMarketInfo { market = "eth/usdt" });
            return result;
        }




    }
}