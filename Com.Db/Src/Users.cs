using System;
using System.ComponentModel.DataAnnotations.Schema;
using Com.Api.Sdk.Enum;
using Com.Api.Sdk.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Com.Db;

/// <summary>
/// 用户基础信息
/// </summary>
public class Users : ResUser
{
    /// <summary>
    /// 用户id
    /// </summary>
    /// <value></value>
    public long user_id { get; set; }
    /// <summary>
    /// 用户密码
    /// </summary>
    /// <value></value>
    [JsonIgnore]
    public string password { get; set; } = null!;
    /// <summary>
    /// 禁用,true:禁用,false:启用
    /// </summary>
    /// <value></value>
    public bool disabled { get; set; }
    /// <summary>
    /// 是否交易 true:交易,false:非交易
    /// </summary>
    /// <value></value>
    public bool transaction { get; set; }
    /// <summary>
    /// 是否提现 true:提现,false:非提现
    /// </summary>
    /// <value></value>
    public bool withdrawal { get; set; }
    /// <summary>
    /// 用户类型
    /// </summary>
    /// <value></value>
    //[JsonConverter(typeof(StringEnumConverter))]

    public E_UserType user_type { get; set; }
    /// <summary>
    /// 推荐人id
    /// </summary>
    /// <value></value>
    public string? recommend { get; set; }
    /// <summary>
    /// google验证码
    /// </summary>
    /// <value></value>
    [JsonIgnore]
    public string? google_key { get; set; }
    /// <summary>
    /// sha公钥
    /// </summary>
    /// <value></value>
    public string public_key { get; set; } = null!;
    /// <summary>
    /// sha私钥
    /// </summary>
    /// <value></value>
    [JsonIgnore]
    public string private_key { get; set; } = null!;
}