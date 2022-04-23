using Com.Api.Sdk.Enum;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Com.Api.Sdk.Models;

/// <summary>
/// 交易记录
/// </summary>
public class ResDeal
{
    /// <summary>
    /// 交易对
    /// </summary>
    /// <value></value>
    public string symbol { get; set; } = null!;

    /// <summary>
    /// 成交价
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal price { get; set; }
    /// <summary>
    /// 成交量
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal amount { get; set; }
    /// <summary>
    /// 成交触发方向(吃单方向)
    /// </summary>
    /// <value></value>
    //[JsonConverter(typeof(StringEnumConverter))]
    //[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public E_OrderSide trigger_side { get; set; }
    /// <summary>
    /// 成交时间
    /// </summary>
    /// <value></value>
    public DateTimeOffset time { get; set; }

}