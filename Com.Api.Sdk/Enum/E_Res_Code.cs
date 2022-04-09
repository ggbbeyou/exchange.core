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
    /// 数据库操作出错
    /// </summary>
    db_error,
    /// <summary>
    /// 服务操作出错
    /// </summary>
    service_error,
    /// <summary>
    /// 无权限
    /// </summary>
    no_permission = 1002,

    /// <summary>
    /// 未找到交易对
    /// </summary>
    symbol_not_found = 1004,
    /// <summary>
    /// 字段长度过长
    /// </summary>
    length_too_long,
    /// <summary>
    /// 请求超时
    /// </summary>
    request_overtime,
    /// <summary>
    /// 未找到api_key参数
    /// </summary>
    not_found_api_key,
    /// <summary>
    /// 未找到api_sign参数
    /// </summary>
    not_found_api_sign,
    /// <summary>
    /// 未找到api_timestamp参数
    /// </summary>
    not_found_api_timestamp,
    /// <summary>
    /// 签名错误
    /// </summary>
    signature_error,
    /// <summary>
    /// 不是白名单用户
    /// </summary>
    not_white_ip,
    //////用户相关错误码/////////////////////////////////////////////////////////////////////



    /// <summary>
    /// 未找到该用户
    /// </summary>
    user_not_found,
    /// <summary>
    /// 未找到该用户vip
    /// </summary>
    vip_not_found,


    //////订单相关错误码/////////////////////////////////////////////////////////////////////

    /// <summary>
    /// 系统禁止挂单
    /// </summary>
    system_prohibit_place_order,
    /// <summary>
    /// 用户禁止挂单
    /// </summary>
    user_prohibit_place_order,
    /// <summary>
    /// 成交价格不能低于0
    /// </summary>
    trans_price_cannot_lower_0,
    /// <summary>
    /// 成交价格不能低于0
    /// </summary>
    amount_cannot_lower_0,
    /// <summary>
    /// 限价单交易价错误
    /// </summary>
    limit_price_error,
    /// <summary>
    /// 限价单交易量错误
    /// </summary>
    limit_amount_error,

    /// <summary>
    /// 市价单交易额错误
    /// </summary>
    market_total_error,
    /// <summary>
    /// 市价单交易量错误
    /// </summary>
    market_amount_error,
    /// <summary>
    /// 交易模式
    /// </summary>
    trade_model_error,


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






}