using System;

namespace Com.Api.Sdk.Enum;

/// <summary>
/// 请求操作动作
/// </summary>
public enum E_Op
{
    /// <summary>
    /// 服务初始化  动作:获取服务状态
    /// </summary>
    service_get_status = 0,
    /// <summary>
    /// 服务启动  动作:启动服务,如启动撮合成交后续动作
    /// </summary>
    service_start = 1,
    /// <summary>
    /// 服务停止
    /// </summary>
    service_stop = 2,
    /// <summary>
    /// 挂单
    /// </summary>
    place = 3,
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