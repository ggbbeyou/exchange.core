using System;

namespace Com.Model.Enum;

/// <summary>
/// 订单买卖方向
/// </summary>
public enum E_Op
{
    /// <summary>
    /// 启动交易对撮合
    /// </summary>
    start = 1,
    /// <summary>
    /// 停止交易对撮合
    /// </summary>
    stop = 2,
    /// <summary>
    /// 挂单
    /// </summary>
    Place = 3,
    /// <summary>
    /// 根据订单id撤单
    /// </summary>
    cancel_by_id = 4,
    /// <summary>
    /// 根据用户id撤单
    /// </summary>
    cancel_by_uid = 5,
    /// <summary>
    /// 根据客户自定义id撤单
    /// </summary>
    cancel_by_clientid = 6,
    /// <summary>
    /// 该交易对全部撤单
    /// </summary>
    cancel_by_all = 7,

}