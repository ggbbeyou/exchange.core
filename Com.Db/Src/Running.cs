using System;
using System.ComponentModel.DataAnnotations.Schema;
using Com.Api.Sdk;
using Com.Api.Sdk.Enum;
using Com.Api.Sdk.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Com.Db;

/// <summary>
/// 计账钱包流水
/// 注:此表数据量超大,请使用数据库表分区功能
/// </summary>
public class Running : ResRunning
{
    /// <summary>
    /// 关联id
    /// </summary>
    /// <value></value>
    public long relation_id { get; set; }
    /// <summary>
    /// 币id
    /// </summary>
    /// <value></value>
    public long coin_id { get; set; }
    /// <summary>
    /// 来源 钱包id
    /// </summary>
    /// <value></value>
    public long wallet_from { get; set; }
    /// <summary>
    /// 目的 钱包id
    /// </summary>
    /// <value></value>
    public long wallet_to { get; set; }
    /// <summary>
    /// 来源 用户id
    /// </summary>
    /// <value></value>
    public long uid_from { get; set; }
    /// <summary>
    /// 目的 用户id
    /// </summary>
    /// <value></value>
    public long uid_to { get; set; }
    /// <summary>
    /// 操作人 0:系统
    /// </summary>
    /// <value></value>
    public long operation_uid { get; set; }


}