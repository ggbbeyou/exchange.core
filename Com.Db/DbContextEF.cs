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
    /// 构造函数
    /// </summary>
    /// <param name="options"></param>
    public DbContextEF(DbContextOptions<DbContextEF> options) : base(options)
    {

    }



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

    public IRouteTail RouteTail { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Deal>(o =>
        {
            o.HasKey(p => p.trade_id);
            o.Property(p => p.trade_id).IsRequired().HasMaxLength(40).HasComment("交易ID");
            // o.Property(p => p.Payer).IsRequired().HasMaxLength(50).HasComment("付款用户名");
            // o.Property(p => p.Money).HasComment("付款金额分");
            // o.Property(p => p.CreateTime).HasComment("创建时间");
            // o.Property(p => p.IsDelete).HasComment("是否已删除");
            // o.HasQueryFilter(p => p.IsDelete == false);
            o.ToTable(nameof(Deal));
        });
    }
}

