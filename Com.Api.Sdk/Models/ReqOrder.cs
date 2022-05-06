
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
     
    public E_OrderSide side { get; set; }
    /// <summary>
    /// 订单类型
    /// </summary>
    /// <value></value>
    [JsonConverter(typeof(StringEnumConverter))]
     
    public E_OrderType type { get; set; }
    /// <summary>
    /// 交易模式,现货(cash)
    /// </summary>
    /// <value></value>
    //[JsonConverter(typeof(StringEnumConverter))]
     
    public E_TradeModel trade_model { get; set; }
    /// <summary>
    /// 挂单价:限价单(有效),其它无效
    /// </summary>
    /// <value></value>
     
    public decimal? price { get; set; }
    /// <summary>
    /// 挂单量:限价单/市场卖价(有效),其它无效
    /// </summary>
    /// <value></value>
     
    public decimal? amount { get; set; }
    /// <summary>
    /// 挂单额:市价买单(有效),其它无效
    /// </summary>
    /// <value></value>
     
    public decimal? total { get; set; }
    /// <summary>
    /// 触发挂单价格
    /// </summary>
    /// <value></value>   
     
    public decimal trigger_hanging_price { get; set; }
    /// <summary>
    /// 触发撤单价格
    /// </summary>
    /// <value></value>
     
    public decimal trigger_cancel_price { get; set; }

}
