
using Com.Db.Enum;

namespace Com.Bll.ApiModel;

/// <summary>
/// 下单
/// </summary>
public class PlaceOrder
{
    /// <summary>
    /// 客户自定义订单id
    /// </summary>
    /// <value></value>
    public string? client_id { get; set; } = null;
    /// <summary>
    /// 挂单价
    /// </summary>
    /// <value></value>
    public decimal? price { get; set; }
    /// <summary>
    /// 挂单量
    /// </summary>
    /// <value></value>
    public decimal amount { get; set; }
    /// <summary>
    /// 交易方向
    /// </summary>
    /// <value></value>
    public E_OrderSide side { get; set; }
    /// <summary>
    /// 订单类型
    /// </summary>
    /// <value></value>
    public E_OrderType type { get; set; }

}
