using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Com.Db;

/// <summary>
/// DB上下文
/// </summary>
public class DbContextEF : DbContext
{

    private readonly string? connectionString;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DbContextEF()
    {
        this.Database.SetCommandTimeout(1000 * 10);
    }

    public DbContextEF(string connectionString)
    {
        this.connectionString = connectionString;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options"></param>
    public DbContextEF(DbContextOptions<DbContextEF> options) : base(options)
    {

    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            optionsBuilder.UseMySQL(connectionString);
        }
    }

    /// <summary>
    /// K线
    /// </summary>
    /// <value></value>
    public DbSet<Kline> Kline { get; set; } = null!;

}

