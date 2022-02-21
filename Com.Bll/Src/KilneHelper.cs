using Com.Common;
using Com.Db;
using Com.Model;
using Com.Model.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace Com.Bll;
public class KilneHelper
{

    /// <summary>
    /// 常用接口
    /// </summary>
    public FactoryConstant constant = null!;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="constant"></param>
    public KilneHelper(FactoryConstant constant)
    {
        this.constant = constant;
    }

    /// <summary>
    /// 从数据库统计K线
    /// </summary>
    /// <param name="market"></param>
    /// <param name="klineType"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public List<BaseKline> GetKlines(string market, E_KlineType klineType, DateTimeOffset start, DateTimeOffset end)
    {
        List<BaseKline> result = new List<BaseKline>();
        int minutes = 0;
        switch (klineType)
        {
            case E_KlineType.min1:
                minutes = 1;
                break;
            case E_KlineType.min5:
                minutes = 5;
                break;
            case E_KlineType.min15:
                minutes = 15;
                break;
            case E_KlineType.min30:
                minutes = 30;
                break;
            case E_KlineType.hour1:
                minutes = 60;
                break;
            case E_KlineType.hour4:
                minutes = 240;
                break;
            case E_KlineType.day1:
                minutes = 1440;
                break;
            default:
                break;
        }
        if (minutes > 0)
        {
            var sql = from deal in this.constant.db.Deal.Where(P => P.market == market && start <= P.time && P.time < end)
                      group deal by deal.timestamp / minutes into g
                      select new BaseKline
                      {
                          market = market,
                          amount = g.Sum(P => P.amount),
                          count = g.Count(),
                          total = g.Sum(P => P.price * P.amount),
                          open = g.First().price,
                          close = g.Last().price,
                          low = g.Min(P => P.price),
                          high = g.Max(P => P.price),
                          type = klineType,
                          time_start = DateTimeOffset.FromUnixTimeSeconds(g.Key * minutes),
                          time_end = DateTimeOffset.FromUnixTimeSeconds(g.Key * minutes).AddMinutes(minutes),
                          time = DateTimeOffset.UtcNow,
                      };
            result = sql.ToList();
        }
        else if (klineType == E_KlineType.week1)
        {

        }
        else if (klineType == E_KlineType.month1)
        {

        }
        return result;
    }

}
