using System;

namespace Com.Db;

/// <summary>
/// K线
/// </summary>
public class BaseMarketInfo
{
    public long id { get; set; }
    /// <summary>
    /// 交易对
    /// </summary>
    /// <value></value>
    public long market { get; set; } 
    /// <summary>
    /// 最后的成交价
    /// </summary>
    /// <value></value>
    public decimal last_price { get; set; }  
}