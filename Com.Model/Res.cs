using System;
using Com.Model.Enum;

namespace Com.Model;

/// <summary>
/// 响应操作动作
/// </summary>
public class Res<T>
{
    /// <summary>
    /// 操作   
    /// </summary>
    /// <value></value>
    public E_Op op { get; set; }
    /// <summary>
    /// 是否成功
    /// </summary>
    /// <value></value>
    public bool success { get; set; }
    /// <summary>
    /// 返回编号
    /// </summary>
    /// <value></value>
    public E_Res_Code code { get; set; }
    /// <summary>
    /// 交易对
    /// </summary>
    /// <value></value>
    public string market { get; set; } = null!;
    /// <summary>
    /// 响应消息
    /// </summary>
    /// <value></value>
    public string message { get; set; } = null!;
    /// <summary>
    /// 数据
    /// </summary>
    /// <value></value>
    public List<T> data { get; set; } = null!;
}