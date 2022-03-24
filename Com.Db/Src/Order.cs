using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Com.Api.Sdk.Enum;
using Com.Api.Sdk.Models;

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
    /// 订单总额(市价买单必填,其它都无效)
    /// </summary>
    /// <value></value>
    public decimal? total { get; set; }
    /// <summary>
    /// 手续费率
    /// </summary>
    /// <value></value>
    public decimal fee_rate { get; set; }
    /// <summary>
    /// 备注
    /// </summary>
    /// <value></value>
    public string? remarks { get; set; }
}