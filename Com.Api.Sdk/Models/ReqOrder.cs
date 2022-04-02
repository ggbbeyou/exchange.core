
using Com.Api.Sdk.Enum;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Com.Api.Sdk.Models;

/// <summary>
/// 下单请求模型
/// </summary>
public class ReqOrder
{
    /// <summary>
    /// 客户自定义订单id
    /// </summary>
    /// <value></value>
    public string? client_id { get; set; } = null;
    /// <summary>
    /// 交易对名称
    /// </summary>
    /// <value></value>
    public string symbol { get; set; } = null!;
    /// <summary>
    /// 交易方向
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(StringEnumConverter))]
    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public E_OrderSide side { get; set; }
    /// <summary>
    /// 订单类型
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(StringEnumConverter))]
    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public E_OrderType type { get; set; }
    /// <summary>
    /// 挂单价(限价单必填,市价单无效,db:市价为成交均价)
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal? price { get; set; }
    /// <summary>
    /// 限价单:挂单量,市价单:交易额,db:量
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal? amount { get; set; }
    /// <summary>
    /// 触发挂单价格
    /// </summary>
    /// <value></value>   
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal trigger_hanging_price { get; set; }
    /// <summary>
    /// 触发撤单价格
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(JsonConverterDecimal))]
    public decimal trigger_cancel_price { get; set; }
    /// <summary>
    /// 附加数据
    /// </summary>
    /// <value></value>
    public string? data { get; set; }

}
