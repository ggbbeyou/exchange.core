using System;
using Com.Db.Enum;

namespace Com.Db.Model;

/// <summary>
/// 订阅响应
/// </summary>
public class ResWebsocker
{
    // <summary>
    /// 操作，subscribe unsubscribe 
    /// </summary>
    /// <value></value>
    public string op { get; set; } = "subscribe";
    /// <summary>
    /// 响应订阅的频道列表
    /// </summary>
    /// <returns></returns>
    public ResChannel args { get; set; } = new ResChannel();
}

/// <summary>
/// 频道信息
/// </summary>
public class ResChannel
{
    /// <summary>
    /// 是否订阅成功
    /// </summary>
    /// <value></value>
    public bool success { get; set; } = true;
    // <summary>
    /// 频道  
    /// account，
    /// orders:symbol,
    /// trades:symbol,
    /// books50-l2-tbt:symbol,
    /// tickers:symbol
    /// 
    /// </summary>
    /// <value></value>
    public string channel { get; set; } = null!;
    /// <summary>
    /// 数据
    /// </summary>
    /// <value></value>
    public string? data { get; set; }
    /// <summary>
    /// 消息
    /// </summary>
    /// <value></value>
    public string message { get; set; } = null!;
}
