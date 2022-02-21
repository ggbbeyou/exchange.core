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
    /// 构造函数
    /// </summary>
    public DbContextEF()
    {
        this.Database.SetCommandTimeout(1000 * 10);
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options"></param>
    public DbContextEF(DbContextOptions<DbContextEF> options) : base(options)
    {
    }

    /// <summary>
    /// 账户下面币种
    /// </summary>
    /// <value></value>
    // public DbSet<AccountCurrency> AccountCurrency { get; set; }

}

