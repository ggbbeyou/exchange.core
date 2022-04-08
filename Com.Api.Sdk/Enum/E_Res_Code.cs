using System;

namespace Com.Api.Sdk.Enum;

/// <summary>
/// 请求操作动作
/// 1000-1999 常用错误码
/// 2000-2999 用户相关错误码
/// 3000-3999 订单相关错误码
/// 4000-4999 资金相关错误码
/// 5000-5999 暂定
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
    ok = 1000,
    /// <summary>
    /// 失败
    /// </summary>
    fail = 1001,
    /// <summary>
    /// 无权限
    /// </summary>
    no_permission = 1002,

    /// <summary>
    /// 未找到交易对
    /// </summary>
    symbol_not_found = 1004,



    //////用户相关错误码/////////////////////////////////////////////////////////////////////



    /// <summary>
    /// 未找到该用户
    /// </summary>
    user_not_found,


    //////订单相关错误码/////////////////////////////////////////////////////////////////////

    /// <summary>
    /// 系统禁止挂单
    /// </summary>
    system_prohibit_place_order,
    /// <summary>
    /// 成交价格不能低于0
    /// </summary>
    trans_price_cannot_lower_0,
    /// <summary>
    /// 成交价格不能低于0
    /// </summary>
    amount_cannot_lower_0,

    //////资金相关错误码/////////////////////////////////////////////////////////////////////

    /// <summary>
    /// 未找到钱包
    /// </summary>
    wallet_not_found,
    /// <summary>
    /// 资金不能低于0
    /// </summary>
    amount_cannot_lass_0,

    /// <summary>
    /// 资金不足
    /// </summary>
    available_not_enough,


    /// <summary>
    /// 字段内容格式出错
    /// </summary>
    field_error = 6,



}