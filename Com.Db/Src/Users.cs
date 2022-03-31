using System;
using System.ComponentModel.DataAnnotations.Schema;
using Com.Api.Sdk.Models;

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
    /// 禁用
    /// </summary>
    /// <value></value>
    public bool disabled { get; set; } 
    /// <summary>
    /// sha私钥
    /// </summary>
    /// <value></value>
    public string private_key { get; set; } = null!;
}