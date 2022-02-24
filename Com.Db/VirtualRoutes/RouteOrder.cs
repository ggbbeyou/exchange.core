using System;
using Com.Model;
using Com.Model.Enum;
using ShardingCore.Core.EntityMetadatas;
using ShardingCore.VirtualRoutes.Months;

namespace Com.Db;

/// <summary>
/// 订单 路由
/// </summary>
public class RouteOrder : AbstractSimpleShardingMonthKeyDateTimeOffsetVirtualTableRoute<Order>
{

    public RouteOrder()
    {
    }

    public override bool AutoCreateTableByTime()
    {
        return true;
    }

    public override void Configure(EntityMetadataTableBuilder<Order> builder)
    {
        builder.ShardingProperty(o => o.create_time);
        builder.AutoCreateTable(false);
        builder.TableSeparator("_");
    }

    public override DateTimeOffset GetBeginTime()
    {
        return DateTimeOffset.Now;
    }
}