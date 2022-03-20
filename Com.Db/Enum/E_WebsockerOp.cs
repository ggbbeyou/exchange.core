using System;

namespace Com.Db.Enum;

/// <summary>
/// 请求操作动作
/// </summary>
public enum E_WebsockerOp
{

    /// <summary>
    /// 登录
    /// </summary>
    login = 1,
    /// <summary>
    /// 登出
    /// </summary>
    Logout = 2,
    /// <summary>
    /// 订阅
    /// </summary>
    subscribe = 3,
    /// <summary>
    /// 取消订阅
    /// </summary>
    unsubscribe = 4,
    /// <summary>
    /// 订阅数据
    /// </summary>
    subscribe_date = 5,


}