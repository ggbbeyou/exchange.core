using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Com.Db;

/// <summary>
/// 交易对基础信息
/// </summary>
public class MarketInfo
{
    /// <summary>
    /// 交易对
    /// </summary>
    /// <value></value>
    public long market { get; set; }
    /// <summary>
    /// 交易对名称
    /// </summary>
    /// <value></value>
    public string symbol { get; set; } = null!;
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
    /// 交易量整数倍数
    /// </summary>
    /// <value></value>
    public int amount_multiple { get; set; }
    /// <summary>
    /// 手续费
    /// </summary>
    /// <value></value>
    public decimal fee { get; set; }
    /// <summary>
    /// 最后的成交价
    /// </summary>
    /// <value></value>
    [NotMapped]
    public decimal last_price { get; set; }
}