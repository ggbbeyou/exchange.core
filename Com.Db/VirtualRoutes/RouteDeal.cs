using System;
using Com.Model;
using Com.Model.Enum;
using ShardingCore.Core.EntityMetadatas;
using ShardingCore.VirtualRoutes.Mods;
using ShardingCore.VirtualRoutes.Months;

namespace Com.Db;

/// <summary>
/// 成交单 路由
/// </summary>
public class RouteDeal : AbstractSimpleShardingMonthKeyDateTimeOffsetVirtualTableRoute<Deal>
{

    public RouteDeal()
    {
    }

    public override bool AutoCreateTableByTime()
    {
        return true;
    }

    public override void Configure(EntityMetadataTableBuilder<Deal> builder)
    {
        builder.ShardingProperty(o => o.time);
        builder.AutoCreateTable(false);
        builder.TableSeparator("_");
    }

    public override DateTimeOffset GetBeginTime()
    {
        return new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }
}