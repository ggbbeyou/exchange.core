

using Com.Db.Enum;

namespace Com.Db.Model;

/// <summary>
/// web调用结果
/// </summary>
public class CallResponse<T> : Res<T>
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
}
