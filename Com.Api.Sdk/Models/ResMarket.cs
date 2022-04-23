using System;
using System.ComponentModel.DataAnnotations.Schema;
using Com.Api.Sdk;
using Com.Api.Sdk.Enum;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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
    /// 市场类型
    /// </summary>
    /// <value></value>
    //[JsonConverter(typeof(StringEnumConverter))]
    //[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public E_MarketType market_type { get; set; }
    /// <summary>
    /// 是否交易(true:可以交易,false:禁止交易)
    /// </summary>
    /// <value></value>
    public bool transaction { get; set; }
    /// <summary>
    /// 状态 true:正在运行,false:停止
    /// </summary>
    /// <value></value>
    public bool status { get; set; }
    /// <summary>
    /// 交易价小数位数
    /// </summary>
    /// <value></value>
    public int places_price { get; set; }
    /// <summary>
    /// 交易量小数位数
    /// </summary>
    /// <value></value>   
    public int places_amount { get; set; }
    /// <summary>
    /// 除了市价卖单外每一笔最小交易额
    /// </summary>
    /// <value></value>
    public decimal trade_min { get; set; }
    /// <summary>
    /// 市价卖单每一笔最小交易量
    /// </summary>
    /// <value></value>
    public decimal trade_min_market_sell { get; set; }
    /// <summary>
    /// 排序
    /// </summary>
    /// <value></value>
    public float sort { get; set; }
    /// <summary>
    /// 标签
    /// </summary>
    /// <value></value>
    public string? tag { get; set; }
}