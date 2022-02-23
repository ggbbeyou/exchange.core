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
public class RouteDeal : AbstractSimpleShardingModKeyStringVirtualTableRoute<Deal>
{

    public RouteDeal() : base(2, 3)
    {
    }

    public override void Configure(EntityMetadataTableBuilder<Deal> builder)
    {
        builder.ShardingProperty(o => o.trade_id);
        builder.AutoCreateTable(false);
        builder.TableSeparator("_");
    }

    // public override DateTime GetBeginTime()
    // {
    //     return DateTimeOffset.UtcNow.DateTime;
    // }

    // public override void Configure(EntityMetadataTableBuilder<Deal> builder)
    // {
    //     builder.ShardingProperty(o => o.time);
    // }

    // public override bool AutoCreateTableByTime()
    // {
    //     return true;
    // }
}