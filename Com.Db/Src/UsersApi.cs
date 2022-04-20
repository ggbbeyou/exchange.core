using System;
using System.ComponentModel.DataAnnotations.Schema;
using Com.Api.Sdk.Models;
using Newtonsoft.Json;

namespace Com.Db;

/// <summary>
/// Api用户
/// </summary>
public class UsersApi : ResUsersApi
{
    /// <summary>
    /// 用户id
    /// </summary>
    /// <value></value>
    [JsonIgnore]
    public long user_id { get; set; }
    /// <summary>
    /// 账户密钥
    /// </summary>
    /// <value></value>    
    [JsonIgnore]
    public string api_secret { get; set; } = null!;
    /// <summary>
    /// 最后登录IP地址
    /// </summary>
    /// <value></value>
    public string? last_login_ip { get; set; }
}