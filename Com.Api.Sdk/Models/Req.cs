using System;

namespace Com.Api.Sdk.Models;

/// <summary>
/// 请求操作动作
/// </summary>
public class Req<T>
{
    /// <summary>
    /// 数据
    /// </summary>
    /// <value></value>
    public T data { get; set; } = default!;
}