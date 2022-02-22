using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ShardingCore.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;
using ShardingCore.Sharding;
using ShardingCore.Sharding.Abstractions;

namespace Com.Db;

/// <summary>
/// DB上下文
/// </summary>
public class DbContextEF : AbstractShardingDbContext, IShardingTableDbContext
{
    /// <summary>
    /// 
    /// </summary>
    /// <value></value>
    public IRouteTail RouteTail { get; set; } = null!;

    /// <summary>
    /// K线
    /// </summary>
    /// <value></value>
    public DbSet<Kline> Kline { get; set; } = null!;
    /// <summary>
    /// K线
    /// </summary>
    /// <value></value>
    public DbSet<Deal> Deal { get; set; } = null!;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options"></param>
    public DbContextEF(DbContextOptions<DbContextEF> options) : base(options)
    {

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="modelBuilder"></param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Deal>(o =>
        {
            o.HasKey(p => p.trade_id);
            o.HasIndex(P => new { P.market, P.time });//.IsUnique();
            o.HasIndex(P => new { P.market, P.time, P.timestamp });
            o.Property(P => P.trade_id).IsRequired().HasColumnType("bigint").HasComment("成交订单ID");
            o.Property(P => P.market).IsRequired().HasColumnType("nvarchar").HasMaxLength(50).HasComment("交易对");
            o.Property(P => P.price).IsRequired().HasColumnType("decimal").HasPrecision(28, 16).HasComment("成交价");
            o.Property(P => P.amount).IsRequired().HasColumnType("amount").HasPrecision(28, 16).HasComment("成交量");
            o.Property(P => P.total).IsRequired().HasColumnType("decimal").HasPrecision(28, 16).HasComment("成交总额");
            o.Property(P => P.trigger_side).IsRequired().HasColumnType("tinyint").HasComment("成交触发方向");
            o.Property(P => P.bid_id).IsRequired().HasColumnType("bigint").HasComment("买单id");
            o.Property(P => P.ask_id).IsRequired().HasColumnType("bigint").HasComment("卖单id");
            o.Property(P => P.time).IsRequired().HasColumnType("datetimeoffset").HasComment("成交时间");
            o.ToTable(nameof(Deal));
        });
    }
}

