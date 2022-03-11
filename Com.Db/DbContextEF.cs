using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Com.Db;

/// <summary>
/// DB上下文
/// </summary>
public class DbContextEF : DbContext
{
    /// <summary>
    /// 交易对基础信息
    /// </summary>
    /// <value></value>
    public DbSet<MarketInfo> MarketInfo { get; set; } = null!;
    /// <summary>
    /// K线
    /// </summary>
    /// <value></value>
    public DbSet<Kline> Kline { get; set; } = null!;
    /// <summary>
    /// 成交单
    /// </summary>
    /// <value></value>
    public DbSet<Deal> Deal { get; set; } = null!;
    /// <summary>
    /// 订单表
    /// </summary>
    /// <value></value>
    public DbSet<Orders> Order { get; set; } = null!;

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
        modelBuilder.Entity<MarketInfo>(o =>
        {
            o.HasKey(p => p.market);
            o.Property(P => P.market).IsRequired().HasColumnType("bigint").HasMaxLength(50).HasComment("交易对");
            o.Property(P => P.symbol).HasColumnType("nvarchar").HasMaxLength(20).HasComment("交易对名称");
            o.ToTable(nameof(MarketInfo));
        });
        modelBuilder.Entity<Kline>(o =>
        {
            o.HasKey(p => p.id);
            o.HasIndex(P => new { P.market, P.type, P.time_start, P.time_end });
            o.HasIndex(P => new { P.market, P.type, P.time_start });
            o.Property(P => P.id).IsRequired().ValueGeneratedNever().HasColumnType("bigint").HasComment("K线ID");
            o.Property(P => P.market).IsRequired().HasColumnType("bigint").HasMaxLength(50).HasComment("交易对");
            o.Property(P => P.symbol).HasColumnType("nvarchar").HasMaxLength(20).HasComment("交易对名称");
            o.Property(P => P.type).IsRequired().HasColumnType("tinyint").HasComment("K线类型");
            o.Property(P => P.amount).IsRequired().HasColumnType("decimal").HasPrecision(28, 16).HasComment("成交量");
            o.Property(P => P.count).IsRequired().HasColumnType("bigint").HasComment("成交笔数");
            o.Property(P => P.total).IsRequired().HasColumnType("decimal").HasPrecision(28, 16).HasComment("成交总额");
            o.Property(P => P.open).IsRequired().HasColumnType("decimal").HasPrecision(28, 16).HasComment("开盘价");
            o.Property(P => P.close).IsRequired().HasColumnType("decimal").HasPrecision(28, 16).HasComment("收盘价");
            o.Property(P => P.low).IsRequired().HasColumnType("decimal").HasPrecision(28, 16).HasComment("最低价");
            o.Property(P => P.high).IsRequired().HasColumnType("decimal").HasPrecision(28, 16).HasComment("最高价");
            o.Property(P => P.time_start).IsRequired().HasColumnType("datetimeoffset").HasComment("变更开始时间");
            o.Property(P => P.time_end).IsRequired().HasColumnType("datetimeoffset").HasComment("变更开始时间");
            o.Property(P => P.time).IsRequired().HasColumnType("datetimeoffset").HasComment("更新时间");
            o.ToTable(nameof(Kline));
        });
        modelBuilder.Entity<Orders>(o =>
        {
            o.HasKey(p => p.order_id);
            o.HasIndex(P => new { P.market, P.state });
            o.HasIndex(P => new { P.market, P.uid });
            o.HasIndex(P => new { P.create_time });
            o.Property(P => P.order_id).IsRequired().ValueGeneratedNever().HasColumnType("bigint").HasComment("订单ID");
            o.Property(P => P.market).IsRequired().HasColumnType("bigint").HasMaxLength(50).HasComment("交易对");
            o.Property(P => P.symbol).HasColumnType("nvarchar").HasMaxLength(20).HasComment("交易对名称");
            o.Property(P => P.client_id).HasColumnType("nvarchar").HasMaxLength(50).HasComment("客户自定义订单id");
            o.Property(P => P.uid).IsRequired().HasColumnType("bigint").HasComment("用户ID");
            o.Property(P => P.price).IsRequired().HasColumnType("decimal").HasPrecision(28, 16).HasComment("成交价");
            o.Property(P => P.amount).IsRequired().HasColumnType("decimal").HasPrecision(28, 16).HasComment("成交量");
            o.Property(P => P.total).IsRequired().HasColumnType("decimal").HasPrecision(28, 16).HasComment("成交总额");
            o.Property(P => P.create_time).IsRequired().HasColumnType("datetimeoffset").HasComment("挂单时间");
            o.Property(P => P.amount_unsold).IsRequired().HasColumnType("decimal").HasPrecision(28, 16).HasComment("未成交量");
            o.Property(P => P.amount_done).IsRequired().HasColumnType("decimal").HasPrecision(28, 16).HasComment("已成交挂单量");
            o.Property(P => P.deal_last_time).HasColumnType("datetimeoffset").HasComment("最后成交时间");
            o.Property(P => P.side).IsRequired().HasColumnType("tinyint").HasComment("交易方向");
            o.Property(P => P.state).IsRequired().HasColumnType("tinyint").HasComment("成交状态");
            o.Property(P => P.type).IsRequired().HasColumnType("tinyint").HasComment("订单类型");
            o.Property(P => P.data).HasColumnType("nvarchar").HasMaxLength(200).HasComment("附加数据");
            o.Property(P => P.remarks).HasColumnType("nvarchar").HasMaxLength(200).HasComment("备注");
            o.ToTable(nameof(Order));
        });
        modelBuilder.Entity<Deal>(o =>
        {
            o.HasKey(p => p.trade_id);
            o.HasIndex(P => new { P.market, P.time });
            o.Property(P => P.trade_id).IsRequired().ValueGeneratedNever().HasColumnType("bigint").HasComment("成交订单ID");
            o.Property(P => P.market).IsRequired().HasColumnType("bigint").HasMaxLength(50).HasComment("交易对");
            o.Property(P => P.symbol).HasColumnType("nvarchar").HasMaxLength(20).HasComment("交易对名称");
            o.Property(P => P.price).IsRequired().HasColumnType("decimal").HasPrecision(28, 16).HasComment("成交价");
            o.Property(P => P.amount).IsRequired().HasColumnType("decimal").HasPrecision(28, 16).HasComment("成交量");
            o.Property(P => P.total).IsRequired().HasColumnType("decimal").HasPrecision(28, 16).HasComment("成交总额");
            o.Property(P => P.trigger_side).IsRequired().HasColumnType("tinyint").HasComment("成交触发方向");
            o.Property(P => P.bid_id).IsRequired().HasColumnType("bigint").HasComment("买单id");
            o.Property(P => P.ask_id).IsRequired().HasColumnType("bigint").HasComment("卖单id");
            o.Property(P => P.time).IsRequired().HasColumnType("datetimeoffset").HasMaxLength(50).HasComment("成交时间");
            o.ToTable(nameof(Deal));
        });
        base.OnModelCreating(modelBuilder);
    }
}

