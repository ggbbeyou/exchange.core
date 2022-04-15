using System.Linq.Expressions;
using Com.Db;
using Com.Api.Sdk.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using Com.Api.Sdk.Models;

namespace Com.Bll;

/// <summary>
/// Service:交易记录
/// </summary>
public class ServiceDeal
{
    /// <summary>
    /// 初始化
    /// </summary>
    public ServiceDeal()
    {
    }

    /// <summary>
    /// 获取交易记录
    /// </summary>
    /// <param name="market">交易对</param>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    /// <returns></returns>
    public List<Deal> GetDeals(long market, DateTimeOffset? start, DateTimeOffset? end)
    {
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                return db.Deal.Where(P => P.market == market).WhereIf(start != null, P => start <= P.time).WhereIf(end != null, P => P.time <= end).OrderBy(P => P.time).AsNoTracking().ToList();
            }
        }
    }

    /// <summary>
    /// 添加交易记录
    /// </summary>
    /// <param name="deals"></param>
    public bool AddDeal(List<Deal> deals)
    {
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                try
                {
                    db.Deal.AddRange(deals);
                    db.SaveChanges();
                }
                catch (System.Exception ex)
                {
                    FactoryService.instance.constant.logger.LogError(ex, "添加交易记录出错");
                    return false;
                }
                return true;
            }
        }
    }


    /// <summary>
    /// 交易记录转换成一分钟K线
    /// </summary>
    /// <param name="market">交易对</param>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    /// <returns></returns>
    public List<Kline>? GetKlinesMin1ByDeal(long market, DateTimeOffset? start, DateTimeOffset? end)
    {
        try
        {
            using (var scope = FactoryService.instance.constant.provider.CreateScope())
            {
                using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
                {
                    var sql = from deal in db.Deal.Where(P => P.market == market).WhereIf(start != null, P => start <= P.time).WhereIf(end != null, P => P.time <= end).OrderBy(P => P.time)
                              group deal by EF.Functions.DateDiffMinute(FactoryService.instance.system_init, deal.time) into g
                              select new Kline
                              {
                                  market = market,
                                  amount = g.Sum(P => P.amount),
                                  count = g.Count(),
                                  total = g.Sum(P => P.total),
                                  open = g.OrderBy(P => P.time).First().price,
                                  close = g.OrderBy(P => P.time).Last().price,
                                  low = g.Min(P => P.price),
                                  high = g.Max(P => P.price),
                                  type = E_KlineType.min1,
                                  time_start = FactoryService.instance.system_init.AddMinutes(g.Key),
                                  time_end = FactoryService.instance.system_init.AddMinutes(g.Key + 1).AddMilliseconds(-1),
                                  time = DateTimeOffset.UtcNow,
                              };
                    return sql.AsNoTracking().ToList();
                }
            }
        }
        catch (Exception ex)
        {
            FactoryService.instance.constant.logger.LogError(ex, "交易记录转换成一分钟K线失败");
        }
        return null;
    }

    /// <summary>
    /// 交易记录转换成K线
    /// </summary>
    /// <param name="market">交易对</param>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    /// <returns></returns>
    public Kline? GetKlinesByDeal(long market, E_KlineType type, DateTimeOffset start, DateTimeOffset? end)
    {
        try
        {
            using (var scope = FactoryService.instance.constant.provider.CreateScope())
            {
                using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
                {
                    var sql = from deal in db.Deal.Where(P => P.market == market && start <= P.time).WhereIf(end != null, P => P.time <= end)
                              group deal by new { deal.market, deal.symbol } into g
                              select new Kline
                              {
                                  market = g.Key.market,
                                  symbol = g.Key.symbol,
                                  amount = g.Sum(P => P.amount),
                                  count = g.Count(),
                                  total = g.Sum(P => P.total),
                                  open = g.OrderBy(P => P.time).First().price,
                                  close = g.OrderBy(P => P.time).Last().price,
                                  low = g.Min(P => P.price),
                                  high = g.Max(P => P.price),
                                  type = type,
                                  time_start = start,
                                  time_end = g.OrderBy(P => P.time).Last().time,
                                  time = DateTimeOffset.UtcNow,
                              };
                    return sql.AsNoTracking().FirstOrDefault();
                }
            }
        }
        catch (Exception ex)
        {
            FactoryService.instance.constant.logger.LogError(ex, "交易记录转换成一分钟K线失败");
        }
        return null;
    }

    /// <summary>
    /// 获取最近24小时聚合行情
    /// </summary>
    /// <param name="market"></param>
    /// <returns></returns>
    public ResTicker? Get24HoursTicker(long market)
    {
        try
        {
            using (var scope = FactoryService.instance.constant.provider.CreateScope())
            {
                using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
                {
                    var sql = from deal in db.Deal
                              where deal.market == market && deal.time >= DateTimeOffset.UtcNow.AddDays(-1)
                              group deal by new { deal.market, deal.symbol } into g
                              select new ResTicker
                              {
                                  market = g.Key.market,
                                  symbol = g.Key.symbol,
                                  price_change = g.Max(P => P.price) - g.Min(P => P.price),
                                  price_change_percent = (g.Max(P => P.price) - g.Min(P => P.price)) / g.Min(P => P.price),
                                  open = g.OrderBy(P => P.time).First().price,
                                  close = g.OrderBy(P => P.time).Last().price,
                                  low = g.Min(P => P.price),
                                  high = g.Max(P => P.price),
                                  close_amount = g.OrderBy(P => P.time).Last().amount,
                                  close_time = g.OrderBy(P => P.time).Last().time,
                                  volume = g.Sum(P => P.amount),
                                  volume_currency = g.Sum(P => P.total),
                                  count = g.Count(),
                                  time = DateTimeOffset.UtcNow,
                              };
                    return sql.AsNoTracking().FirstOrDefault();
                }
            }
        }
        catch (Exception ex)
        {
            FactoryService.instance.constant.logger.LogError(ex, "交易记录转换成一分钟K线失败");
        }
        return null;
    }

    /// <summary>
    /// 深度行情保存到redis并且推送到MQ
    /// </summary>
    /// <param name="depth"></param>
    public bool PushTicker(ResTicker? ticker)
    {
        try
        {
            if (ticker != null)
            {
                ResWebsocker<ResTicker> resWebsocker = new ResWebsocker<ResTicker>();
                resWebsocker.success = true;
                resWebsocker.op = E_WebsockerOp.subscribe_event;
                resWebsocker.channel = E_WebsockerChannel.tickers;
                resWebsocker.data = ticker;
                FactoryService.instance.constant.redis.HashSet(FactoryService.instance.GetRedisTicker(), ticker.market, JsonConvert.SerializeObject(ticker));
                FactoryService.instance.constant.MqPublish(FactoryService.instance.GetMqSubscribe(E_WebsockerChannel.tickers, ticker.market), JsonConvert.SerializeObject(resWebsocker));
            }
        }
        catch (System.Exception ex)
        {
            FactoryService.instance.constant.logger.LogError(ex, "深度行情保存到redis并且推送到MQ失败");
            return false;
        }
        return true;
    }


    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////




    /// <summary>
    /// 同步交易记录
    /// </summary>
    /// <param name="markets">交易对</param>
    /// <param name="start">start之后所有记录</param>
    /// <returns></returns>
    public bool DealDbToRedis(long market, DateTimeOffset start)
    {
        Deal? deal = GetRedisLastDeal(market);
        if (deal != null)
        {
            start = deal.time;
        }
        List<Deal> deals = GetDeals(market, start, null);
        if (deals.Count() > 0)
        {
            SortedSetEntry[] entries = new SortedSetEntry[deals.Count()];
            for (int i = 0; i < deals.Count(); i++)
            {
                entries[i] = new SortedSetEntry(JsonConvert.SerializeObject(deals[i]), deals[i].time.ToUnixTimeMilliseconds());
            }
            FactoryService.instance.constant.redis.SortedSetAdd(FactoryService.instance.GetRedisDeal(market), entries);
        }
        return true;
    }

    /// <summary>
    /// 从redis获取最后一条交易记录
    /// </summary>
    /// <param name="market">交易对</param>
    /// <returns></returns>
    public Deal? GetRedisLastDeal(long market)
    {
        RedisValue[] redisvalue = FactoryService.instance.constant.redis.SortedSetRangeByRank(FactoryService.instance.GetRedisDeal(market), 0, 1, StackExchange.Redis.Order.Descending);
        if (redisvalue.Length > 0)
        {
            return JsonConvert.DeserializeObject<Deal>(redisvalue[0]);
        }
        return null;
    }

    /// <summary>
    /// 删除redis中的交易记录
    /// </summary>
    /// <param name="markets">交易对</param>
    /// <param name="end">end之前记录全部清除</param>
    public long DeleteDeal(long market, DateTimeOffset end)
    {
        return FactoryService.instance.constant.redis.SortedSetRemoveRangeByScore(FactoryService.instance.GetRedisDeal(market), 0, end.ToUnixTimeMilliseconds());
    }

    /// <summary>
    /// 转换
    /// </summary>
    /// <param name="deal"></param>
    /// <returns></returns>
    public ResDeal Convert(Deal deal)
    {
        return new ResDeal
        {
            symbol = deal.symbol,
            price = deal.price,
            amount = deal.amount,
            trigger_side = deal.trigger_side,
            time = deal.time,
        };
    }

    /// <summary>
    /// 订单查询
    /// </summary>
    /// <param name="market">交易对</param>
    /// <param name="uid">用户id</param>
    /// <param name="state">订单状态</param>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    /// <param name="ids">订单id</param>
    /// <returns></returns>
    public Res<List<ResDeal>> GetDealByuid(long uid, int skip, int take, DateTimeOffset? start = null, DateTimeOffset? end = null)
    {
        Res<List<ResDeal>> res = new Res<List<ResDeal>>();
        res.data = new List<ResDeal>();
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                res.data = db.Deal.Where(P => P.ask_uid == uid || P.bid_uid == uid).WhereIf(start == null, P => P.time >= start).WhereIf(start == null, P => P.time <= end).OrderByDescending(P => P.time).Skip(skip).Take(take).ToList().ConvertAll(P => (ResDeal)P);
                return res;
            }
        }
    }


}