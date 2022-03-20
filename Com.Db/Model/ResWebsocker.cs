using System;

using Com.Db.Enum;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Com.Db.Model;

/// <summary>
/// 订阅响应
/// </summary>
public class ResWebsocker<T>
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
    [JsonConverter(typeof(StringEnumConverter))]
    public E_WebsockerOp op { get; set; }
    // <summary>
    /// 频道     
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(StringEnumConverter))]
    public E_WebsockerChannel channel { get; set; }
    /// <summary>
    /// 数据
    /// </summary>
    /// <value></value>
    public T data { get; set; } = default!;
    /// <summary>
    /// 消息
    /// </summary>
    /// <value></value>
    public string message { get; set; } = null!;

}
