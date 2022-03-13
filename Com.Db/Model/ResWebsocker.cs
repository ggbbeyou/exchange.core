using System;
using Com.Db.Enum;

namespace Com.Db.Model;

/// <summary>
/// 订阅响应
/// </summary>
public class ResWebsocker
{
    /// <summary>
    /// 是否订阅成功
    /// </summary>
    /// <value></value>
    public bool success { get; set; } = true;
    // <summary>
    /// 操作，subscribe unsubscribe 
    /// </summary>
    /// <value></value>
    public string op { get; set; } = "subscribe";
    // <summary>
    /// 频道     
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
