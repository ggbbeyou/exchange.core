using System;
using Com.Model;
using Com.Model.Enum;
using ShardingCore.Core.EntityMetadatas;
using ShardingCore.VirtualRoutes.Months;

namespace Com.Db;

/// <summary>
/// K线 路由
/// </summary>
public class RouteKline : AbstractSimpleShardingMonthKeyDateTimeVirtualTableRoute<Kline>
{
    public override DateTime GetBeginTime()
    {
        return DateTimeOffset.UtcNow.DateTime;
    }

    public override void Configure(EntityMetadataTableBuilder<Kline> builder)
    {
        builder.ShardingProperty(o => o.time);
    }

    public override bool AutoCreateTableByTime()
    {
        return true;
    }
}