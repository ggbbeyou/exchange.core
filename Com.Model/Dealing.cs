using System;
using Com.Model.Enum;

namespace Com.Model;

/// <summary>
/// 成交单
/// </summary>
public class Dealing : BaseDeal
{
    /// <summary>
    /// 买订单
    /// </summary>
    /// <value></value>
    public MatchOrder bid { get; set; } = null!;
    /// <summary>
    /// 卖订单
    /// </summary>
    /// <value></value>
    public MatchOrder ask { get; set; } = null!;
}