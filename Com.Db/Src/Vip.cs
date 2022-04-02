using System;
using System.ComponentModel.DataAnnotations.Schema;
using Com.Api.Sdk;
using Newtonsoft.Json;

namespace Com.Db;

/// <summary>
/// 用户等级
/// </summary>
public class Vip
{
    /// <summary>
    /// id
    /// </summary>
    /// <value></value>
    public long id { get; set; }
    /// <summary>
    /// 等级名称
    /// </summary>
    /// <value></value>
    public string name { get; set; } = null!;
    /// <summary>
    /// vip等级要求成交量总额
    /// </summary>
    /// <value></value>
    public decimal volume_used { get; set; }
    /// <summary>
    /// 挂单手续费
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal fee_maker { get; set; }
    /// <summary>
    /// 吃单手续费
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal fee_taker { get; set; }

}