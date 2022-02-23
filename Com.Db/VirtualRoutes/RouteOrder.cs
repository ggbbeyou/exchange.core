using System;
using Com.Model;
using Com.Model.Enum;
using ShardingCore.Core.EntityMetadatas;
using ShardingCore.VirtualRoutes.Months;

namespace Com.Db;

/// <summary>
/// 订单 路由
/// </summary>
public class RouteOrder : AbstractSimpleShardingMonthKeyDateTimeVirtualTableRoute<Order>
{
    public override DateTime GetBeginTime()
    {
        return DateTimeOffset.UtcNow.DateTime;
    }

    public override void Configure(EntityMetadataTableBuilder<Order> builder)
    {
        builder.ShardingProperty(o => o.create_time);
    }

    public override bool AutoCreateTableByTime()
    {
        return true;
    }
}