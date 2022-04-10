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
    /// 用户密码
    /// </summary>
    /// <value></value>
    public string password { get; set; } = null!;
    /// <summary>
    /// 禁用,true:禁用,false:启用
    /// </summary>
    /// <value></value>
    public bool disabled { get; set; }
    /// <summary>
    /// 用户类型
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(StringEnumConverter))]
    //[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public E_UserType user_type { get; set; }
    /// <summary>
    /// google验证码
    /// </summary>
    /// <value></value>
    public string? google_key { get; set; }
    /// <summary>
    /// google验证器密钥
    /// </summary>
    /// <value></value>
    public string? google_private_key { get; set; }
    /// <summary>
    /// sha私钥
    /// </summary>
    /// <value></value>
    public string private_key { get; set; } = null!;
}