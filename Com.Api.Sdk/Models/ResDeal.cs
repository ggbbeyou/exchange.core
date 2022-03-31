using Newtonsoft.Json;

namespace Com.Api.Sdk.Models;

/// <summary>
/// 交易记录
/// </summary>
[JsonConverter(typeof(JsonConverterDecimal))]
public class ResDeal
{
    /// <summary>
    /// 交易对
    /// </summary>
    /// <value></value>
    public string symbol { get; set; } = null!;
    /// <summary>
    /// 交易记录  0:成交价,1:成交量,2:触发方向(1:挂单,2:吃单),3:成交时间(时间戳)
    /// </summary>
    /// <returns></returns>
    public List<decimal[]> deal { get; set; } = new List<decimal[]>();

}