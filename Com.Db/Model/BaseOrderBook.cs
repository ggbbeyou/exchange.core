using System;
using Com.Db.Enum;

namespace Com.Db.Model;

/// <summary>
/// 盘口
/// </summary>
public class BaseOrderBook
{
    /// <summary>
    /// 交易对
    /// </summary>
    /// <value></value>
    public long market { get; set; }
    /// <summary>
    /// 交易对
    /// </summary>
    /// <value></value>
    public string symbol { get; set; } = null!;
    /// <summary>
    /// 挂单价
    /// </summary>
    /// <value></value>
    public decimal price { get; set; }
    /// <summary>
    /// 挂单总量
    /// </summary>
    /// <value></value>
    public decimal amount { get; set; }
    /// <summary>
    /// 挂单总金额
    /// </summary>
    /// <value></value>
    public decimal total
    {
        get
        {
            return price * amount;
        }
    }
    /// <summary>
    /// 挂单笔数量
    /// </summary>
    /// <value></value>
    public decimal count { get; set; }
    /// <summary>
    /// 变更时间
    /// </summary>
    /// <value></value>
    public DateTimeOffset last_time { get; set; }
    /// <summary>
    /// 交易方向
    /// </summary>
    /// <value></value>
    public E_OrderSide direction { get; set; }
}