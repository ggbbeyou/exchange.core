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
        // DateTimeOffset s = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
        // DateTimeOffset now = DateTimeOffset.UtcNow;
        // var aaab = (now - s).TotalSeconds;


        // DateTimeOffset bbb = DateTimeOffset.FromUnixTimeSeconds((long)aaab);

        // System.Data.Objects.SqlClient.SqlFunctions.DateDiff(start, end);
        // SqlFunctions sqlFunctions = new SqlFunctions(this.constant);
        var a = from deal in this.constant.db.Deal.Where(P => start <= P.time && P.time < end)
                group deal by (deal.market, deal.timestamp) into g
                select new BaseKline
                {
                    market = g.Key.market,
                    time_start = DateTimeOffset.FromUnixTimeSeconds(g.Key.timestamp),
                    count = g.Count(),
                };






        return result;
    }

}
