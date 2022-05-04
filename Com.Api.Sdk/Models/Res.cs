using System;
using Com.Api.Sdk.Enum;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Com.Api.Sdk.Models;

/// <summary>
/// 响应操作动作
/// </summary>
public class Res<T>
{
    /// <summary>
    /// 返回编号
    /// </summary>
    /// <value></value>
    //[JsonConverter(typeof(StringEnumConverter))]
    // //[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public E_Res_Code code { get; set; } = E_Res_Code.ok;
    /// <summary>
    /// 响应消息
    /// </summary>
    /// <value></value>
    public string? msg { get; set; } = null;
    /// <summary>
    /// 数据
    /// </summary>
    /// <value></value>
    public T data { get; set; } = default!;
}