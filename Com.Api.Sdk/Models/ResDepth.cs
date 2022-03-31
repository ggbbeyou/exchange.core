namespace Com.Api.Sdk.Models;

/// <summary>
/// orderbook
/// </summary>
public class ResDepth
{
    /// <summary>
    /// 交易对
    /// </summary>
    /// <value></value>
    public string symbol { get; set; } = null!;
    /// <summary>
    /// 全部实时深度行情买盘，价格从高到低  0:price,1:size
    /// </summary>
    /// <returns></returns>
    public List<List<decimal>> bid { get; set; } = null!;
    /// <summary>
    /// 全部实时深度行情买盘，价格从高到低  0:price,1:size
    /// </summary>
    /// <returns></returns>
    public List<List<decimal>> ask { get; set; } = null!;
    /// <summary>
    /// 总额,可作为推送校检深度
    /// </summary>
    /// <value></value>
    public decimal total_bid { get; set; }
    /// <summary>
    /// 总额,可作为推送校检深度
    /// </summary>
    /// <value></value>
    public decimal total_ask { get; set; }
    /// <summary>
    /// orderbook时间
    /// </summary>
    /// <value></value>
    public DateTimeOffset timestamp { get; set; }
}