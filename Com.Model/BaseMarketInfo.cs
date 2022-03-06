using System;
using Com.Model.Enum;

namespace Com.Model;

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
    public string market { get; set; } = null!;
    /// <summary>
    /// 最后的成交价
    /// </summary>
    /// <value></value>
    public decimal deal_last_time { get; set; }
    /// <summary>
    /// 服务地址
    /// </summary>
    /// <value></value>
    public string? service_url { get; set; }
    /// <summary>
    /// 撮合地址
    /// </summary>
    /// <value></value>
    public string? match_url { get; set; }


}