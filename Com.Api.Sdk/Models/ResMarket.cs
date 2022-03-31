using System;
using System.ComponentModel.DataAnnotations.Schema;
using Com.Api.Sdk;
using Newtonsoft.Json;

namespace Com.Db;

/// <summary>
/// 交易对基础信息
/// </summary>
public class ResMarket
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
    /// 基础币种名
    /// </summary>
    /// <value></value>
    public string coin_name_base { get; set; } = null!;
    /// <summary>
    /// 报价币种名
    /// </summary>
    /// <value></value>
    public string coin_name_quote { get; set; } = null!;
    /// <summary>
    /// 分隔符
    /// </summary>
    /// <value></value>
    public string separator { get; set; } = "/";
    /// <summary>
    /// 状态 true:正在运行,false:停止
    /// </summary>
    /// <value></value>
    public bool status { get; set; }
    /// <summary>
    /// 价格小数位数
    /// </summary>
    /// <value></value>
    public int price_places { get; set; }
    /// <summary>
    /// 交易量最小整数倍数
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal amount_multiple { get; set; }
    /// <summary>
    /// 市价买手续费
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal fee_market_buy { get; set; }
    /// <summary>
    /// 市价卖手续费
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal fee_market_sell { get; set; }
    /// <summary>
    /// 限价买手续费
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal fee_limit_buy { get; set; }
    /// <summary>
    /// 限价卖手续费
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal fee_limit_sell { get; set; }
}