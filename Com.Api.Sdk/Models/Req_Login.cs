

using Com.Api.Sdk.Enum;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Com.Api.Sdk.Models;

/// <summary>
/// 登录模型
/// </summary>
public class Req_Login
{
    /// <summary>
    /// api用户key
    /// </summary>
    /// <value></value>
    public string api_key { get; set; } = null!;
    /// <summary>
    /// 请求时间戳
    /// </summary>
    /// <value></value>
    public long timestamp { get; set; }
    /// <summary>
    /// 签名
    /// </summary>
    /// <value></value>
    public string sign { get; set; } = null!;


}
