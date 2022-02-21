using System;
using Com.Model.Enum;

namespace Com.Db;

/// <summary>
/// K线
/// </summary>
public class Kline
{
    /// <summary>
    /// 主键
    /// </summary>
    /// <value></value>
    public long id { get; set; }
    /// <summary>
    /// 交易对
    /// </summary>
    /// <value></value>
    public string market { get; set; } = null!;
    /// <summary>
    /// 成交量
    /// </summary>
    /// <value></value>
    public decimal amount { get; set; }
    /// <summary>
    /// 成交笔数
    /// </summary>
    /// <value></value>
    public decimal count { get; set; }
    /// <summary>
    /// 成交总金额
    /// </summary>
    /// <value></value>
    public decimal total { get; set; }
    /// <summary>
    /// 开盘价
    /// </summary>
    /// <value></value>
    public decimal open { get; set; }
    /// <summary>
    /// 收盘价（当K线为最晚的一根时，是最新成交价）
    /// </summary>
    /// <value></value>
    public decimal close { get; set; }
    /// <summary>
    /// 最低价
    /// </summary>
    /// <value></value>
    public decimal low { get; set; }
    /// <summary>
    /// 最高价
    /// </summary>
    /// <value></value>
    public decimal high { get; set; }
    /// <summary>
    /// K线类型
    /// </summary>
    /// <value></value>
    public E_KlineType type { get; set; }
    /// <summary>
    /// 变更开始时间
    /// </summary>
    /// <value></value>
    public DateTimeOffset time_start { get; set; }
    /// <summary>
    /// 变更结束时间
    /// </summary>
    /// <value></value>
    public DateTimeOffset time_end { get; set; }
    /// <summary>
    /// 更新时间
    /// </summary>
    /// <value></value>
    public DateTimeOffset time { get; set; }
}