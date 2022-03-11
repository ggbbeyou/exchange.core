

using Com.Db.Enum;

namespace Com.Db.Model;

/// <summary>
/// 请求操作动作
/// </summary>
public class CallRequest<T> : Req<T>
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
    public long market { get; set; }
    /// <summary>
    /// 数据
    /// </summary>
    /// <value></value>
    public T data { get; set; } = default!;
}
