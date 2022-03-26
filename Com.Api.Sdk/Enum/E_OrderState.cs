using System;

namespace Com.Api.Sdk.Enum;

/// <summary>
/// 订单成交状态
/// </summary>
public enum E_OrderState
{
    /// <summary>
    /// 未成交
    /// </summary>
    unsold = 0,
    /// <summary>
    /// 部分成交
    /// </summary>
    partial = 1,
    /// <summary>
    /// 完全成交
    /// </summary>
    completed = 2,
    /// <summary>
    /// 撤单或部分撤单
    /// </summary>
    cancel = 3,
    /// <summary>
    /// 暂不进撮合订单(触发单)
    /// </summary>
    not_atch = 4,
}