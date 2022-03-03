using System;
using Com.Model.Enum;

namespace Com.Model;

/// <summary>
/// K线
/// </summary>
public class BaseMarketInfo
{
    public long id { get; set; }
    /// <summary>
    /// 交易对
    /// </summary>
    /// <value></value>
    public string market { get; set; } = null!;
    
    
  
}