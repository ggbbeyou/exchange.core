using System;
using Com.Model;
using Com.Model.Enum;
using ShardingCore.Core.EntityMetadatas;
using ShardingCore.VirtualRoutes.Months;

namespace Com.Db;

/// <summary>
/// 成交单 路由
/// </summary>
public class RouteKline : AbstractSimpleShardingMonthKeyDateTimeVirtualTableRoute<Kline>
{
    public override DateTime GetBeginTime()
    {
        return new DateTime(2022, 1, 1);
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