using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Com.Api.Sdk;
using Com.Api.Sdk.Enum;
using Com.Api.Sdk.Models;

namespace Com.Db;

/// <summary>
/// 成交单
/// 注:此表数据量超大,请使用数据库表分区功能
/// </summary>
public class Deal : ResDeal
{
    /// <summary>
    /// 成交id
    /// </summary>
    /// <value></value>
    public long trade_id { get; set; }
    /// <summary>
    /// 交易对
    /// </summary>
    /// <value></value>
    public long market { get; set; }
    /// <summary>
    /// 成交总额
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal total { get; set; }
    /// <summary>
    /// 买单id
    /// </summary>
    /// <value></value>
    public long bid_id { get; set; }
    /// <summary>
    /// 卖单id
    /// </summary>
    /// <value></value>
    public long ask_id { get; set; }
    /// <summary>
    /// 买单用户id
    /// </summary>
    /// <value></value>
    public long bid_uid { get; set; }
    /// <summary>
    /// 卖单用户id
    /// </summary>
    /// <value></value>
    public long ask_uid { get; set; }
    /// <summary>
    /// 买单用户名
    /// </summary>
    /// <value></value>
    public string bid_name { get; set; } = null!;
    /// <summary>
    /// 卖单用户名
    /// </summary>
    /// <value></value>
    public string ask_name { get; set; } = null!;
    /// <summary>
    /// 买单未成交量
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal bid_amount_unsold { get; set; }
    /// <summary>
    /// 卖单未成交量
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal ask_amount_unsold { get; set; }
    /// <summary>
    /// 买单已成交量
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal bid_amount_done { get; set; }
    /// <summary>
    /// 卖单已成交量
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal ask_amount_done { get; set; }
    /// <summary>
    /// 买手续费率
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal fee_rate_buy { get; set; }
    /// <summary>
    /// 卖手续费率
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal fee_rate_sell { get; set; }
    /// <summary>
    /// 买手续费
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal fee_buy { get; set; }
    /// <summary>
    /// 卖手续费
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal fee_sell { get; set; }
    /// <summary>
    /// 买手续费币种
    /// </summary>
    /// <value></value>
    public long fee_coin_buy { get; set; }
    /// <summary>
    /// 卖手续费币种
    /// </summary>
    /// <value></value>
    public long fee_coin_sell { get; set; }


}