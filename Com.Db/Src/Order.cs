using System;
using System.ComponentModel.DataAnnotations.Schema;
using Com.Api.Sdk;
using Com.Api.Sdk.Enum;
using Com.Api.Sdk.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Com.Db;

/// <summary>
/// 订单模型
/// 注:此表数据量超大,请使用数据库表分区功能
/// </summary>

public class Orders : ResOrder
{
    /// <summary>
    /// 用户名
    /// </summary>
    /// <value></value>
    public string user_name { get; set; } = null!;
    /// <summary>
    /// 已成交均价
    /// </summary>
    /// <value></value>

    public decimal deal_price { get; set; }
    /// <summary>
    /// 已成交量
    /// </summary>
    /// <value></value>

    public decimal deal_amount { get; set; }
    /// <summary>
    /// 已成交额
    /// </summary>
    /// <value></value>

    public decimal deal_total { get; set; }
    /// <summary>
    /// 未成交 买:交易额,卖:交易量
    /// </summary>
    /// <value></value>

    public decimal unsold { get; set; }
    /// <summary>
    /// 订单完成解冻金额
    /// </summary>
    /// <value></value>

    public decimal complete_thaw { get; set; }
    /// <summary>
    /// 挂单手续费
    /// </summary>
    /// <value></value>

    public decimal fee_maker { get; set; }
    /// <summary>
    /// 吃单手续费
    /// </summary>
    /// <value></value>

    public decimal fee_taker { get; set; }

    /// <summary>
    /// 最后成交时间或撤单时间
    /// </summary>
    /// <value></value>
    public DateTimeOffset? deal_last_time { get; set; }
    /// <summary>
    /// 备注
    /// </summary>
    /// <value></value>    
    public string? remarks { get; set; }

}