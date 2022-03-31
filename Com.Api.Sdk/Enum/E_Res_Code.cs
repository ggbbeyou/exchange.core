using System;

namespace Com.Api.Sdk.Enum;

/// <summary>
/// 请求操作动作
/// </summary>
public enum E_Res_Code
{
    /// <summary>
    /// 未定义
    /// </summary>
    unknown = 0,
    /// <summary>
    /// 成功
    /// </summary>
    ok = 1,
    /// <summary>
    /// 失败
    /// </summary>
    fail = 2,

    /// <summary>
    /// 无权限
    /// </summary>
    no_permission = 3,
    /// <summary>
    /// 未找到该用户
    /// </summary>
    no_user = 4,
    /// <summary>
    /// 未找到交易对
    /// </summary>
    no_symbol = 5,
    /// <summary>
    /// 字段内容格式出错
    /// </summary>
    field_error = 6,
    /// <summary>
    /// 资金不足
    /// </summary>
    low_capital = 7,


}