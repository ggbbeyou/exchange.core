using System;
using Com.Model.Enum;

namespace Com.Model;

/// <summary>
/// 请求操作动作
/// </summary>
public class Req<T>
{
    /// <summary>
    /// 操作   
    /// </summary>
    /// <value></value>
    public E_Op op { get; set; }
    /// <summary>
    /// 交易对
    /// </summary>
    /// <value></value>
    public string market { get; set; } = null!;
    /// <summary>
    /// 数据
    /// </summary>
    /// <value></value>
    public List<T> data { get; set; } = null!;
}