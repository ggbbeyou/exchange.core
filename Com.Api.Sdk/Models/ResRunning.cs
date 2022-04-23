using System;
using System.ComponentModel.DataAnnotations.Schema;
using Com.Api.Sdk;
using Com.Api.Sdk.Enum;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Com.Api.Sdk.Models;

/// <summary>
/// 计账钱包流水
/// 注:此表数据量超大,请使用数据库表分区功能
/// </summary>
public class ResRunning
{
    /// <summary>
    /// id
    /// </summary>
    /// <value></value>
    public long id { get; set; }
    /// <summary>
    /// 流水类型
    /// </summary>
    /// <value></value>
    public E_RunningType type { get; set; }
    /// <summary>
    /// 币名称
    /// </summary>
    /// <value></value>
    public string coin_name { get; set; } = null!;
    /// <summary>
    /// 来源 钱包类型
    /// </summary>
    /// <value></value>
    //[JsonConverter(typeof(StringEnumConverter))]
    //[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public E_WalletType wallet_type_from { get; set; }
    /// <summary>
    /// 目的 钱包类型
    /// </summary>
    /// <value></value>
    //[JsonConverter(typeof(StringEnumConverter))]
    //[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public E_WalletType wallet_type_to { get; set; }
    /// <summary>
    /// 来源 用户名
    /// </summary>
    /// <value></value>
    public string user_name_from { get; set; } = null!;
    /// <summary>
    /// 目的 用户名
    /// </summary>
    /// <value></value>
    public string user_name_to { get; set; } = null!;
    /// <summary>
    /// 量
    /// </summary>
    /// <value></value>
    //[JsonConverter(typeof(JsonConverterDecimal))]
    public decimal amount { get; set; }
    /// <summary>
    /// 时间
    /// </summary>
    /// <value></value>
    public DateTimeOffset time { get; set; }
    /// <summary>
    /// 备注
    /// </summary>
    /// <value></value>
    public string? remarks { get; set; }

}