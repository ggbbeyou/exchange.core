using System;
using Com.Api.Sdk.Enum;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Com.Api.Sdk.Models;

/// <summary>
/// K线
/// 注:此表数据量超大,请使用数据库表分区功能
/// </summary>
public class ResKline
{
    /// <summary>
    /// 交易对名称
    /// </summary>
    /// <value></value>
    public string symbol { get; set; } = null!;
    /// <summary>
    /// K线类型
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(StringEnumConverter))]
    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public E_KlineType type { get; set; }
    /// <summary>
    /// 成交量
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal amount { get; set; }
    /// <summary>
    /// 成交笔数
    /// </summary>
    /// <value></value>
    public long count { get; set; }
    /// <summary>
    /// 成交总金额
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal total { get; set; }
    /// <summary>
    /// 开盘价
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal open { get; set; }
    /// <summary>
    /// 收盘价（当K线为最晚的一根时，是最新成交价）
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal close { get; set; }
    /// <summary>
    /// 最低价
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal low { get; set; }
    /// <summary>
    /// 最高价
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal high { get; set; }
    /// <summary>
    /// K线开始时间
    /// </summary>
    /// <value></value>
    public DateTimeOffset time_start { get; set; }
    /// <summary>
    /// K线结束时间
    /// </summary>
    /// <value></value>
    public DateTimeOffset time_end { get; set; }

}