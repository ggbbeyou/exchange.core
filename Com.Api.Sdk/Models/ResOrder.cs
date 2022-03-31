


using Com.Api.Sdk.Enum;
using Newtonsoft.Json;

namespace Com.Api.Sdk.Models;

/// <summary>
/// 下单响应模型
/// </summary>
public class ResOrder : ReqOrder
{
    /// <summary>
    /// 订单id
    /// </summary>
    /// <value></value>
    public long order_id { get; set; }
    /// <summary>
    /// 订单状态
    /// </summary>
    /// <value></value>
    public E_OrderState state { get; set; }

    /// <summary>
    /// 未成交 买:总额,卖:交易量
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal amount_unsold { get; set; }
    /// <summary>
    /// 已成交量 买:总额,卖:交易量
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal amount_done { get; set; }
    /// <summary>
    /// 挂单时间
    /// </summary>
    /// <value></value>
    public DateTimeOffset create_time { get; set; }
    /// <summary>
    /// 最后成交时间或撤单时间
    /// </summary>
    /// <value></value>
    public DateTimeOffset? deal_last_time { get; set; }
}
