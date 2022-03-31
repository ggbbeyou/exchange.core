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
    /// 交易记录:成交价,成交量,触发方向(1:挂单,2:吃单),成交时间(时间戳)
    /// </summary>
    /// <returns></returns>
    public List<decimal[]> deal { get; set; } = new List<decimal[]>();

}