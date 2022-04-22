

using Com.Api.Sdk.Enum;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Com.Api.Sdk.Models;

/// <summary>
/// 请求操作动作
/// </summary>
public class KeyList<T, K>
{
    /// <summary>
    /// key
    /// </summary>
    /// <value></value>
    public T key { get; set; } = default(T)!;
    /// <summary>
    /// list
    /// </summary>
    /// <value></value>
    public List<K> data { get; set; } = null!;

}
