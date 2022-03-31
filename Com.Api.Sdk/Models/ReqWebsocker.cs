using System;
using Com.Api.Sdk.Enum;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Com.Api.Sdk.Models;

/// <summary>
/// 订阅请求
/// </summary>
public class ReqWebsocker
{
    // <summary>
    /// 操作
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(StringEnumConverter))]
    public E_WebsockerOp op { get; set; }
    /// <summary>
    /// 请求订阅的频道列表
    /// </summary>
    /// <returns></returns>
    public List<ReqChannel> args { get; set; } = new List<ReqChannel>();
}

/// <summary>
/// 频道信息
/// </summary>
public class ReqChannel
{
    // <summary>
    /// 频道
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(StringEnumConverter))]
    public E_WebsockerChannel channel { get; set; }
    /// <summary>
    /// 数据 交易对或其它数据
    /// </summary>
    /// <value></value>
    public string data { get; set; } = null!;
}
