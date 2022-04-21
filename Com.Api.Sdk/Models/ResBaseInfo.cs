using Com.Api.Sdk.Enum;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Com.Api.Sdk.Models;

/// <summary>
/// 基本信息
/// </summary>
public class ResBaseInfo
{
    /// <summary>
    /// 网站:名称
    /// </summary>
    /// <value></value>
    public string website_name { get; set; } = null!;
    /// <summary>
    /// 网站:icon
    /// </summary>
    /// <value></value>
    public string website_icon { get; set; } = null!;
    /// <summary>
    ///  网站:系统时间
    /// </summary>
    /// <value></value>
    public DateTimeOffset website_time { get; set; }
    /// <summary>
    ///  网站:文件服务器地址
    /// </summary>
    /// <value></value>
    public string website_serivcefile { get; set; } = null!;
    
}