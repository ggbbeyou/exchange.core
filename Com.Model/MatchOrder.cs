using System;
using Com.Model.Enum;

namespace Com.Model;

/// <summary>
/// 订单表
/// </summary>
public class MatchOrder : BaseOrder
{

    /// <summary>
    /// 触发撤单价格
    /// </summary>
    /// <value></value>
    public decimal trigger_cancel_price { get; set; }


}