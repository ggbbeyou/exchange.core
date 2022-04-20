using System;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Com.Api.Sdk.Models;

/// <summary>
/// Api用户
/// </summary>
public class ResUsersApi
{
    /// <summary>
    /// id
    /// </summary>
    /// <value></value>    
    public long id { get; set; }
    /// <summary>
    /// 名称
    /// </summary>
    /// <value></value>
    public string? name { get; set; }
    /// <summary>
    /// 账户key
    /// </summary>
    /// <value></value>
    public string api_key { get; set; } = null!;

    /// <summary>
    /// 是否交易,true:交易,false:非交易
    /// </summary>
    /// <value></value>
    public bool transaction { get; set; }
    /// <summary>
    /// 是否提现,true:提现,false:非提现
    /// </summary>
    /// <value></value>
    public bool withdrawal { get; set; }
    /// <summary>
    /// IP白名单
    /// </summary>
    /// <value></value>
    public string? white_list_ip { get; set; }
    /// <summary>
    /// 创建时间
    /// </summary>
    /// <value></value>
    public DateTimeOffset create_time { get; set; }

}