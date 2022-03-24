using System;

namespace Com.Api.Sdk.Enum;

/// <summary>
/// 请求操作动作
/// </summary>
public enum E_WebsockerChannel
{
    /// <summary>
    /// 无
    /// </summary>
    none = 0,
    /// <summary>
    /// 1分钟
    /// </summary>
    min1 = 1,
    /// <summary>
    /// 5分钟
    /// </summary>
    min5 = 2,
    /// <summary>
    /// 15分钟
    /// </summary>
    min15 = 3,
    /// <summary>
    /// 30分钟
    /// </summary>
    min30 = 4,
    /// <summary>
    /// 1小时
    /// </summary>
    hour1 = 5,
    /// <summary>
    /// 6小时
    /// </summary>
    hour6 = 6,
    /// <summary>
    /// 12小时
    /// </summary>
    hour12 = 7,
    /// <summary>
    /// 1天
    /// </summary>
    day1 = 8,
    /// <summary>
    /// 1周
    /// </summary>
    week1 = 9,
    /// <summary>
    /// 1月
    /// </summary>
    month1 = 10,
    /// <summary>
    /// 聚合行情
    /// </summary>
    tickers = 11,
    /// <summary>
    /// 成交记录
    /// </summary>
    trades = 12,
    /// <summary>
    /// 盘口10档(全部)
    /// </summary>
    books10 = 13,
    /// <summary>
    /// 盘口50档(全部)
    /// </summary>
    books50 = 14,
    /// <summary>
    /// 盘口200档(全部)
    /// </summary>
    books200 = 15,
    /// <summary>
    /// 盘口10档(增量)
    /// </summary>
    books10_inc = 16,
    /// <summary>
    /// 盘口50档(增量)
    /// </summary>
    books50_inc = 17,
    /// <summary>
    /// 盘口200档(增量)
    /// </summary>
    books200_inc = 18,
    /// <summary>
    /// 资金
    /// </summary>
    assets = 19,
    /// <summary>
    /// 订单
    /// </summary>
    orders = 20,
}