using System;

namespace Com.Api.Sdk.Enum;

/// <summary>
/// 审核状态
/// </summary>
public enum E_Verify
{
    /// <summary>
    /// 未审核
    /// </summary>
    verify_not = 0,
    /// <summary>
    /// 审核申请中
    /// </summary>
    verify_apply = 1,
    /// <summary>
    /// 审核通过
    /// </summary>
    verify_ok = 2,
    /// <summary>
    /// 审核未通过
    /// </summary>
    verify_no = 3,

}