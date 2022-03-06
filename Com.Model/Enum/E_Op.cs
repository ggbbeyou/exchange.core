using System;

namespace Com.Model.Enum;

/// <summary>
/// 请求操作动作
/// </summary>
public enum E_Op
{
    /// <summary>
    /// 服务初始化  动作:预热缓存,如同步K线或成交记录到redis
    /// </summary>
    service_init = 0,
    /// <summary>
    /// 服务启动  动作:启动服务,如启动撮合成交后续动作
    /// </summary>
    service_start = 1,
    /// <summary>
    /// 服务停止
    /// </summary>
    service_stop = 2,
    /// <summary>
    /// 启动交易对撮合
    /// </summary>
    match_start = 3,
    /// <summary>
    /// 停止交易对撮合
    /// </summary>
    match_stop = 4,
    /// <summary>
    /// 挂单
    /// </summary>
    place = 5,
    /// <summary>
    /// 根据订单id撤单
    /// </summary>
    cancel_by_id = 6,
    /// <summary>
    /// 根据用户id撤单
    /// </summary>
    cancel_by_uid = 7,
    /// <summary>
    /// 根据客户自定义id撤单
    /// </summary>
    cancel_by_clientid = 8,
    /// <summary>
    /// 该交易对全部撤单
    /// </summary>
    cancel_by_all = 9,

}