using System.Linq.Expressions;
using Com.Common;
using Com.Db;
using Com.Model;
using Com.Model.Enum;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace Com.Bll;
public class DealHelper
{
    /// <summary>
    /// 常用接口
    /// </summary>
    public FactoryConstant constant = null!;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="constant"></param>
    public DealHelper(FactoryConstant constant)
    {
        this.constant = constant;
    }

    /// <summary>
    /// 获取交易记录
    /// </summary>
    /// <param name="market"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public List<Deal> GetDeals(string market, DateTimeOffset? start, DateTimeOffset? end)
    {
        GetKlinesByDeal(market, E_KlineType.min1, start, end);
        a(market, E_KlineType.min1, start.Value);


        Expression<Func<Deal, bool>> predicate = P => P.market == market;
        if (start != null)
        {
            predicate = predicate.And(P => start <= P.time);
        }
        if (end != null)
        {
            predicate = predicate.And(P => P.time <= end);
        }
        return this.constant.db.Deal.Where(predicate).OrderBy(P => P.time).ToList();
    }

    public List<Kline>? GetKlinesByDeal(string market, E_KlineType klineType, DateTimeOffset? start, DateTimeOffset? end)
    {
        try
        {

            Expression<Func<Deal, bool>> predicate = P => P.market == market;
            if (start != null)
            {
                predicate = predicate.And(P => start <= P.time);
            }
            if (end != null)
            {
                predicate = predicate.And(P => P.time <= end);
            }
            var deals = this.constant.db.Deal.Where(predicate).ToList();

        }
        catch (System.Exception)
        {

            throw;
        }
        return null;
    }



    public async Task a(string market, E_KlineType klineType, DateTimeOffset end)
    {
        try
        {
            /*

              var ids = new[] {"200", "300"};
                        var dateOfMonths = new[] {202111, 202110};
                        var group = await (from u in _virtualDbContext.Set<SysUserSalary>()
                                .Where(o => ids.Contains(o.UserId) && dateOfMonths.Contains(o.DateOfMonth))
                            group u by new
                            {
                                UId = u.UserId
                            }
                            into g
                            select new
                            {
                                GroupUserId = g.Key.UId,
                                Count = g.Count(),
                                TotalSalary = g.Sum(o => o.Salary),
                                AvgSalary = g.Average(o => o.Salary),
                                AvgSalaryDecimal = g.Average(o => o.SalaryDecimal),
                                MinSalary = g.Min(o => o.Salary),
                                MaxSalary = g.Max(o => o.Salary)
                            }).ToListAsync();
            -----
            著作权归xuejiaming所有。
            链接: https://xuejmnet.github.io/sharding-core-doc/query/group-by-query/#



            */
            var eee = this.constant.db.Deal_202202.ToList();
            var sql2 = from kline in this.constant.db.Deal_202202.OrderBy(P => P.time)
                       where kline.market == market && end >= kline.time && end > kline.time

                       group kline by new
                       {
                           kline.market,
                           kline.time
                       }
                into g
                       select new
                       {
                           //    g.Key.market,
                           //    g.Key.time,
                           //    count = g.Count()


                           market = g.Key.market,
                           amount = g.Sum(P => P.amount),
                           count = g.Count(),
                           total = g.Sum(P => P.total),
                           //    open = g.First().open,
                           //    close = g.Last().close,
                           //    low = g.Min(P => P.low),
                           //    high = g.Max(P => P.high),
                           //    type = klineType,
                           //    time_start = g.First().time_start,
                           //    time_end = g.Last().time_end,
                           time = g.Key.time,
                       };


            var bbbbb = sql2.ToList();

        }
        catch (System.Exception ex)
        {

            throw;
        }





        var sql1 = from kline in this.constant.db.Set<Kline>()
                   where kline.market == market && kline.type == klineType && end >= kline.time && end > kline.time
                   //    orderby kline.time_end.Minute
                   //   group kline by lambda into g
                   group kline by kline.open into g
                   //    group kline by kline.open into g
                   select new
                   {
                       Minute = g.Key,
                       //   amount = g.Sum(P => P.amount),
                   };
        //    select new Kline
        //    {
        //        //   open = g.Key,
        //        market = market,
        //        amount = g.Sum(P => P.amount),
        //        count = g.Count(),
        //        total = g.Sum(P => P.total),
        //        open = g.First().open,
        //        close = g.Last().close,
        //        low = g.Min(P => P.low),
        //        high = g.Max(P => P.high),
        //        type = klineType,
        //        time_start = g.First().time_start,
        //        time_end = g.Last().time_end,
        //        time = DateTimeOffset.UtcNow,
        //    };
        try
        {
            var aaa = sql1.ToList();

        }
        catch (System.Exception ex)
        {

            throw;
        }

    }

}