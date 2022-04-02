using System;
using System.ComponentModel.DataAnnotations.Schema;
using Com.Api.Sdk;
using Com.Api.Sdk.Enum;
using Com.Api.Sdk.Models;
using Newtonsoft.Json;

namespace Com.Db;

/// <summary>
/// 订单模型
/// 注:此表数据量超大,请使用数据库表分区功能
/// </summary>

public class Orders : ResOrder
{
    /// <summary>
    /// 交易对
    /// </summary>
    /// <value></value>
    public long market { get; set; }
    /// <summary>
    /// 用户ID
    /// </summary>
    /// <value></value>
    public long uid { get; set; }
    /// <summary>
    /// 用户名
    /// </summary>
    /// <value></value>
    public string user_name { get; set; } = null!;
    /// <summary>
    /// 订单总额(市价买单必填,其它都无效)
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal? total { get; set; }   
    /// <summary>
    /// 备注
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public string? remarks { get; set; }
}