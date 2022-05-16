using System;
using Com.Api.Sdk.Enum;

namespace Com.Service.Models;

/// <summary>
/// 处理进程
/// </summary>
public class Processing
{
    /// <summary>
    /// 序号
    /// </summary>
    /// <value></value>
    public long no { get; set; }
    /// <summary>
    /// 撮合进程
    /// </summary>
    /// <value></value>
    public bool match { get; set; }
    /// <summary>
    /// 资产变更
    /// </summary>
    /// <value></value>
    public bool asset { get; set; }
    /// <summary>
    /// 资产流水(手续费)
    /// </summary>
    /// <value></value>
    public bool running_fee { get; set; }
    /// <summary>
    /// 资产流水(交易)
    /// </summary>
    /// <value></value>
    public bool running_trade { get; set; }
    /// <summary>
    /// 成交记录添加
    /// </summary>
    /// <value></value>
    public bool deal { get; set; }
    /// <summary>
    /// 订单更新
    /// </summary>
    /// <value></value>
    public bool order { get; set; }
    /// <summary>
    /// 订单取消
    /// </summary>
    /// <value></value>
    public bool order_cancel { get; set; }
    /// <summary>
    /// 订单完成或撤单解冻多余资金
    /// </summary>
    /// <value></value>
    public bool order_complete_thaw_buy { get; set; }
    /// <summary>
    /// 订单完成或撤单解冻多余资金
    /// </summary>
    /// <value></value>
    public bool order_complete_thaw_sell { get; set; }
    /// <summary>
    /// 推送订单更新
    /// </summary>
    /// <value></value>
    public bool push_order { get; set; }
    /// <summary>
    /// 推送订单取消
    /// </summary>
    /// <value></value>
    public bool push_order_cancel { get; set; }
    /// <summary>
    /// 保存K线并推送K线
    /// </summary>
    /// <value></value>
    public bool push_kline { get; set; }
    /// <summary>
    /// 推送交易记录
    /// </summary>
    /// <value></value>
    public bool push_deal { get; set; }
    /// <summary>
    /// 推送聚合行情
    /// </summary>
    /// <value></value>
    public bool push_ticker { get; set; }
}