using System;

namespace Com.Api.Sdk.Enum;

/// <summary>
/// 钱包类型
/// </summary>
public enum E_WalletType
{
    /// <summary>
    /// (用户)主钱包
    /// </summary>
    main = 0,
    /// <summary>
    /// (结算账号)收手续费钱包
    /// </summary>
    fee = 1,
    /// <summary>
    /// (结算账号)充值钱包
    /// </summary>
    recharge = 2,
    /// <summary>
    /// (结算账号)取款续费钱包
    /// </summary>
    withdraw = 3,
}