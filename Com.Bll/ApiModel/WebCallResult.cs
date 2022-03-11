

namespace Com.Bll.ApiModel;

/// <summary>
/// web调用结果
/// </summary>
public class WebCallResult<T>
{
    /// <summary>
    /// 是否成功
    /// </summary>
    /// <value></value>
    public bool success { get; set; }
    /// <summary>
    /// 返回编码
    /// 0:成功
    /// </summary>
    /// <value></value>
    public int code { get; set; }
    /// <summary>
    /// 返回消息
    /// </summary>
    /// <value></value>
    public string? message { get; set; }
    /// <summary>
    /// 返回数据
    /// </summary>
    /// <value></value>
    public T data { get; set; } = default!;

}
