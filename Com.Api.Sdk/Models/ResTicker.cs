using System;
using Com.Api.Sdk.Enum;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Com.Api.Sdk.Models;

/// <summary>
/// 聚合行情
/// </summary>
public class ResTicker
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
    /// 24小时价格变化
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal price_change { get; set; }
    /// <summary>
    /// 24小时价格变化百分比
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal price_change_percent { get; set; }
    /// <summary>
    /// 24小时内开盘价
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal open { get; set; }
    /// <summary>
    /// 24小时内最高价
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal high { get; set; }
    /// <summary>
    /// 24小时内最低价
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal low { get; set; }
    /// <summary>
    /// 24小时内收盘价
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal close { get; set; }
    /// <summary>
    /// 24小时内收盘量
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal close_amount { get; set; }
    /// <summary>
    /// 24小时内最后一笔成交时间
    /// </summary>
    /// <value></value>
    public DateTimeOffset close_time { get; set; }
    /// <summary>
    /// 24小时内交易量
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal volume { get; set; }
    /// <summary>
    /// 24小时内交易额
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal volume_currency { get; set; }
    /// <summary>
    /// 24小时内交易笔数
    /// </summary>
    /// <value></value>
    public int count { get; set; }
    /// <summary>
    /// 记录时间
    /// </summary>
    /// <value></value>
    public DateTimeOffset time { get; set; }

}