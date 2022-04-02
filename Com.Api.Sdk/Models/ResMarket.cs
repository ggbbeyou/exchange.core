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
    /// 最小交易额
    /// </summary>
    /// <value></value>
    public decimal trade_min { get; set; }
    /// <summary>
    /// 排序
    /// </summary>
    /// <value></value>
    public float sort { get; set; }
}