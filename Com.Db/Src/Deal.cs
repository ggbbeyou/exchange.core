using System;
using Com.Model;
using Com.Model.Enum;

namespace Com.Db;

/// <summary>
/// 成交单
/// </summary>
public class Deal : BaseDeal
{
    /// <summary>
    /// 时间戳(分钟)
    /// </summary>
    /// <value></value>
    public long timestamp { get; set; }
}