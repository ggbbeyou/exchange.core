using System.ComponentModel.DataAnnotations.Schema;

namespace Com.Api.Sdk.Models;

/// <summary>
/// 用户信息
/// </summary>
public class ResUser
{
    /// <summary>
    /// 用户id
    /// </summary>
    /// <value></value>
    public long user_id { get; set; }
    /// <summary>
    /// 用户名
    /// </summary>
    /// <value></value>
    public string user_name { get; set; } = null!;
    /// <summary>
    /// 是否交易
    /// </summary>
    /// <value></value>
    public bool transaction { get; set; }
    /// <summary>
    /// 是否提现
    /// </summary>
    /// <value></value>
    public bool withdrawal { get; set; }
    /// <summary>
    /// 用户手机号码
    /// </summary>
    /// <value></value>
    public string? phone { get; set; }
    /// <summary>
    /// 邮箱
    /// </summary>
    /// <value></value>
    public string? email { get; set; }
    /// <summary>
    /// 用户等级
    /// </summary>
    /// <value></value>
    public long vip { get; set; }
    /// <summary>
    /// sha公钥
    /// </summary>
    /// <value></value>
    public string public_key { get; set; } = null!;
    /// <summary>
    /// 令牌
    /// </summary>
    /// <value></value>
    [NotMapped]
    public string token { get; set; } = null!;
}