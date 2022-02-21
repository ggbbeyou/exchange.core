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

    public KilneHelper(FactoryConstant constant)
    {
        // string? dbConnection = config.GetConnectionString("Mysql");
        // var options = new DbContextOptionsBuilder<DbContextEF>().UseMySQL(dbConnection).Options;
        // var factory = new PooledDbContextFactory<DbContextEF>(options);
        // context = factory.CreateDbContext();
    }

    public List<BaseKline> GetKlines(string market, E_KlineType klineType, DateTimeOffset start, DateTimeOffset end)
    {
        List<BaseKline> result = new List<BaseKline>();
        
        var sql = from deal in this.constant.db.Deal.Where(P => P.market == market && start <= P.time && P.time < end)
                  group deal by deal.timestamp % 1 into g
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
                      time_start = DateTimeOffset.FromUnixTimeSeconds(g.Key),
                      time_end = DateTimeOffset.FromUnixTimeSeconds(g.Key).AddMinutes(1),
                      time = DateTimeOffset.UtcNow,
                  };
        result = sql.ToList();
        return result;
    }

}
