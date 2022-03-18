using System;
using Com.Db.Enum;

namespace Com.Db.Model;

/// <summary>
/// 订阅请求
/// </summary>
public class ReqWebsocker
{
    // <summary>
    /// 操作:login,Logout,subscribe unsubscribe 
    /// </summary>
    /// <value></value>
    public E_WebsockerOp op { get; set; }
    /// <summary>
    /// 请求订阅的频道列表
    /// </summary>
    /// <returns></returns>
    public List<ReqChannel> args { get; set; } = new List<ReqChannel>();
}

/// <summary>
/// 频道信息
/// </summary>
public class ReqChannel
{
    // <summary>
    /// 频道  
    /// account，
    /// orders:symbol,
    /// trades:symbol,
    /// books50-l2-tbt:symbol,
    /// tickers:symbol
    /// order:uid
    /// 
    /// </summary>
    /// <value></value>
    public E_WebsockerChannel channel { get; set; }
    /// <summary>
    /// 数据 交易对或其它数据
    /// </summary>
    /// <value></value>
    public string data { get; set; } = null!;
}
