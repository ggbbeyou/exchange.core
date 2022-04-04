using System;

namespace Com.Api.Sdk.Enum;

/// <summary>
/// 用户类型(api接口权限)
/// </summary>
public enum E_UserType
{
    /// <summary>
    /// 普通账户(支持:挂单/撤单/充币/提现/划转)
    /// </summary>
    general = 0,
    /// <summary>
    /// 结算账户(支持:无,禁止:挂单/撤单/充币/提现/划转)
    /// </summary>
    settlement = 2,
    /// <summary>
    /// 作市账户(支持:挂单/撤单,禁止:充币/提现/划转)
    /// </summary>
    market = 3,
}