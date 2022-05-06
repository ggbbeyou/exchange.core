using System;

using Com.Api.Sdk.Enum;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Com.Api.Sdk.Models;

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
    /// <summary>
    /// 操作
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(StringEnumConverter))]
    //[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public E_WebsockerOp op { get; set; }
    /// <summary>
    /// 频道     
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(StringEnumConverter))]
    //[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public E_WebsockerChannel channel { get; set; }
    /// <summary>
    /// 数据
    /// </summary>
    /// <value></value>
    public T data { get; set; } = default(T)!;
    /// <summary>
    /// 消息
    /// </summary>
    /// <value></value>
    public string msg { get; set; } = null!;

}
