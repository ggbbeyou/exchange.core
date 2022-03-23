using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Com.Db;

/// <summary>
/// 用户等级
/// </summary>
public class Vip
{
    /// <summary>
    /// id
    /// </summary>
    /// <value></value>
    public long id { get; set; }
    /// <summary>
    /// 等级名称
    /// </summary>
    /// <value></value>
    public string name { get; set; } = null!;
    /// <summary>
    /// 市价手续费
    /// </summary>
    /// <value></value>
    public decimal rate_market { get; set; }
    /// <summary>
    /// 限价手续费
    /// </summary>
    /// <value></value>
    public decimal rate_fixed { get; set; }

}