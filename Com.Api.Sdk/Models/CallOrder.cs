

using Com.Api.Sdk.Enum;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Com.Api.Sdk.Models;

/// <summary>
/// 请求操作动作
/// </summary>
public class CallOrder
{
    public string symbol { get; set; } = null!;
    public List<ReqOrder> orders { get; set; } = null!;

}
