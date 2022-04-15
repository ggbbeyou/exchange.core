using System.ComponentModel.DataAnnotations.Schema;

namespace Com.Api.Sdk.Models;

/// <summary>
/// 用户信息
/// </summary>
public class ResUser
{   
    /// <summary>
    /// 用户名(系统生成内部账号)
    /// </summary>
    /// <value></value>
    public string user_name { get; set; } = null!;
    /// <summary>
    /// 邮箱
    /// </summary>
    /// <value></value>
    public string email { get; set; } = null!;
    /// <summary>
    /// 用户手机号码
    /// </summary>
    /// <value></value>
    public string? phone { get; set; }
    
    /// <summary>
    /// 是否验证邮箱 true:验证,false:未验证
    /// </summary>
    /// <value></value>
    public bool verify_email { get; set; }
    /// <summary>
    /// 是否验证手机 true:验证,false:未验证
    /// </summary>
    /// <value></value>
    public bool verify_phone { get; set; }
    /// <summary>
    /// 是否验证谷歌验证器 true:验证,false:未验证
    /// </summary>
    /// <value></value>
    public bool verify_google { get; set; }
    /// <summary>
    /// 用户等级
    /// </summary>
    /// <value></value>
    public long vip { get; set; } 
    /// <summary>
    /// 令牌
    /// </summary>
    /// <value></value>
    [NotMapped]
    public string token { get; set; } = null!;
}