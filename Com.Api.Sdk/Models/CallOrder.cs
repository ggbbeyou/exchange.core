

using Com.Api.Sdk.Enum;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Com.Api.Sdk.Models;

/// <summary>
/// 请求操作动作
/// </summary>
public class CallOrder
{
    /// <summary>
    /// 交易对
    /// </summary>
    /// <value></value>
    public string symbol { get; set; } = null!;
    /// <summary>
    /// 2:按交易对和用户全部撤单,3:按用户和订单id撤单,4:按用户和用户订单id撤单
    /// </summary>
    /// <value></value>
    public int type { get; set; }
    /// <summary>
    /// 挂单数据
    /// </summary>
    /// <value></value>
    public List<ReqOrder> orders { get; set; } = null!;

}
