using System;
using Com.Model;
using Com.Model.Enum;
using ShardingCore.Core.EntityMetadatas;
using ShardingCore.VirtualRoutes.Months;

namespace Com.Db;

/// <summary>
/// K线 路由
/// </summary>
public class RouteKline : AbstractSimpleShardingMonthKeyDateTimeOffsetVirtualTableRoute<Kline>
{

    public RouteKline()
    {
    }

    public override bool AutoCreateTableByTime()
    {
        return true;
    }

    public override void Configure(EntityMetadataTableBuilder<Kline> builder)
    {
        builder.ShardingProperty(o => o.time_start);
        builder.AutoCreateTable(false);
        builder.TableSeparator("_");
    }

    public override DateTimeOffset GetBeginTime()
    {
        return new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }
}