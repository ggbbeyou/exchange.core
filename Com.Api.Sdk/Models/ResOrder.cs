using Com.Api.Sdk.Enum;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Com.Api.Sdk.Models;

/// <summary>
/// 下单响应模型
/// </summary>
public class ResOrder : ReqOrder
{
    /// <summary>
    /// 用户ID
    /// </summary>
    /// <value></value>
    public long uid { get; set; }
    /// <summary>
    /// 交易对
    /// </summary>
    /// <value></value>
    public long market { get; set; }
    /// <summary>
    /// 订单id
    /// </summary>
    /// <value></value>
    public long order_id { get; set; }
    /// <summary>
    /// 订单状态
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(StringEnumConverter))]
    public E_OrderState state { get; set; }
    /// <summary>
    /// 挂单时间
    /// </summary>
    /// <value></value>
    public DateTimeOffset create_time { get; set; }

}
