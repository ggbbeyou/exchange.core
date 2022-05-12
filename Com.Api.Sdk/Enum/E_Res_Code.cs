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
    unknown,
    /// <summary>
    /// 成功
    /// </summary>
    ok,
    /// <summary>
    /// 失败
    /// </summary>
    fail,
    /// <summary>
    /// 数据库操作出错
    /// </summary>
    db_error,
    /// <summary>
    /// 网络错误
    /// </summary>
    network_error,
    /// <summary>
    /// 无权限
    /// </summary>
    permission_no,
    /// <summary>
    /// 未找到交易对
    /// </summary>
    symbol_not_found,
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
    api_key_not_found,
    /// <summary>
    /// 未找到api_sign参数
    /// </summary>
    api_sign_not_found,
    /// <summary>
    /// 未找到api_timestamp参数
    /// </summary>
    api_timestamp_not_found,
    /// <summary>
    /// 签名错误
    /// </summary>
    signature_error,
    /// <summary>
    /// 不是白名单用户
    /// </summary>
    white_ip_not,
    /// <summary>
    /// 未找到文件
    /// </summary>
    file_not_found,
    /// <summary>
    /// 申请失败
    /// </summary>
    apply_fail,
    /// <summary>
    /// 密码不合规则
    /// </summary>
    password_irregularity,
    /// <summary>
    /// 验证码错误
    /// </summary>
    verification_error,
    /// <summary>
    /// 禁用验证,没有权限或已验证时
    /// </summary>
    verification_disable,
    /// <summary>
    /// 名称重复
    /// </summary>
    name_repeat,
    /// <summary>
    /// 不能小于0
    /// </summary>
    cannot_less_0,

    //////用户相关错误码/////////////////////////////////////////////////////////////////////

    /// <summary>
    /// 未找到该用户
    /// </summary>
    user_not_found,
    /// <summary>
    /// 未找到该用户vip
    /// </summary>
    vip_not_found,
    /// <summary>
    /// 未找到该Email
    /// </summary>
    email_not_found,
    /// <summary>
    /// Email已存在
    /// </summary>
    email_repeat,
    /// <summary>
    /// Email地址不合规则
    /// </summary>
    email_irregularity,
    /// <summary>
    /// 登录账号或密码错误
    /// </summary>
    name_password_error,
    /// <summary>
    /// 用户禁用
    /// </summary>
    user_disable,

    //////订单相关错误码/////////////////////////////////////////////////////////////////////

    /// <summary>
    /// 系统禁止挂单
    /// </summary>
    system_disable_place_order,
    /// <summary>
    /// 用户禁止挂单
    /// </summary>
    user_disable_place_order,
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
    /// 交易模式错误
    /// </summary>
    trade_model_error,

    //////资金相关错误码/////////////////////////////////////////////////////////////////////

    /// <summary>
    /// 未找到钱包
    /// </summary>
    wallet_not_found,
    /// <summary>
    /// 未找到币种
    /// </summary>
    coin_not_found,
    /// <summary>
    /// 量不能低于0
    /// </summary>
    volume_cannot_lass_0,
    /// <summary>
    /// 资金不足
    /// </summary>
    available_not_enough,

}