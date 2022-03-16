using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Com.Db;

/// <summary>
/// 币的基础信息
/// </summary>
public class Coin
{
    /// <summary>
    /// 币id
    /// </summary>
    /// <value></value>
    public long coin_id { get; set; }
    /// <summary>
    /// 币名称
    /// </summary>
    /// <value></value>
    public string coin_name { get; set; } = null!;
    /// <summary>
    /// 价格小数位数
    /// </summary>
    /// <value></value>
    public decimal price_places { get; set; }
    /// <summary>
    /// 量小数位数
    /// </summary>
    /// <value></value>
    public decimal amount_places { get; set; }
    /// <summary>
    /// 合约地址
    /// </summary>
    /// <value></value>
    public string? contract { get; set; }
}