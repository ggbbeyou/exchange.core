using Com.Api.Sdk.Enum;
using Newtonsoft.Json;

namespace Com.Api.Sdk.Models;

/// <summary>
/// 交易记录
/// </summary>
public class ResDeal
{
    /// <summary>
    /// 交易对
    /// </summary>
    /// <value></value>
    public string symbol { get; set; } = null!;
    /// <summary>
    /// 成交价
    /// </summary>
    /// <value></value>
    public decimal price { get; set; }
    /// <summary>
    /// 成交量
    /// </summary>
    /// <value></value>
    public decimal amount { get; set; }
    /// <summary>
    /// 成交触发方向
    /// </summary>
    /// <value></value>
    public E_OrderSide trigger_side { get; set; }
    /// <summary>
    /// 成交时间
    /// </summary>
    /// <value></value>
    public DateTimeOffset time { get; set; }



    /// <summary>
    /// 交易记录  0:成交价,1:成交量,2:触发方向(1:挂单,2:吃单),3:成交时间(时间戳)
    /// </summary>
    /// <returns></returns>
    public List<decimal[]> deal { get; set; } = new List<decimal[]>();

}