namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// ip扩展类
/// </summary>
public static class IpExtension
{
    /// <summary>
    /// 获取ip地址
    /// </summary>
    /// <param name="request"></param>
    public static string GetIp(this HttpRequest request)
    {
        string ip = request.Host.Host;
        if (string.IsNullOrWhiteSpace(ip) || ip == "::1" || ip == "127.0.0.1" || ip == "localhost")
        {
            if (request.Headers.TryGetValue("X-Real-IP", out var ip_addr))
            {
                ip = ip_addr;
            }
        }
        if (string.IsNullOrWhiteSpace(ip) || ip == "::1" || ip == "127.0.0.1" || ip == "localhost")
        {
            if (request.Headers.TryGetValue("X-Forwarded-For", out var ip_addr))
            {
                ip = ip_addr;
            }
        }
        return ip;
    }
}