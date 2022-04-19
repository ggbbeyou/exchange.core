using System;
using System.ComponentModel.DataAnnotations.Schema;
using Com.Api.Sdk;
using Newtonsoft.Json;

namespace Com.Db;

/// <summary>
/// 币的基础信息
/// </summary>
public class Coin
{
    /// <summary>
    /// 币id
    /// </summary>
    /// <value></value>
    public long coin_id { get; set; }
    /// <summary>
    /// 币名称
    /// </summary>
    /// <value></value>
    public string coin_name { get; set; } = null!;
    /// <summary>
    /// 全名
    /// </summary>
    /// <value></value>
    public string full_name { get; set; } = null!;
    /// <summary>
    /// 图标地址
    /// </summary>
    /// <value></value>
    public string icon { get; set; } = null!;
    /// <summary>
    /// 合约地址
    /// </summary>
    /// <value></value>
    public string? contract { get; set; }
}