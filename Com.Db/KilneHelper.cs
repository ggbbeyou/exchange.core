using Com.Model;
using Com.Model.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace Com.Db;
public class KilneHelper
{

    /// <summary>
    /// 常用接口
    /// </summary>
    private DbContextEF context;

    public KilneHelper(IConfiguration config)
    {
        string? dbConnection = config.GetConnectionString("Mysql");
        var options = new DbContextOptionsBuilder<DbContextEF>().UseMySQL(dbConnection).Options;
        var factory = new PooledDbContextFactory<DbContextEF>(options);
        context = factory.CreateDbContext();
    }

    public List<BaseKline> GetKlines(string market, E_KlineType klineType, DateTimeOffset start, DateTimeOffset end)
    {
        List<BaseKline> result = new List<BaseKline>();
        // var kline = context.Kline.Where(x => x.market == market && x.KlineType == (int)klineType && x.Time >= start && x.Time <= end).OrderBy(x => x.Time).ToList();


        return result;
    }

}
