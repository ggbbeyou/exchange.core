using System.Security.Cryptography;
using System.Text;

namespace Com.Common;

public class Encryption
{
    /// <summary>
    /// MD5 加密字符串
    /// </summary>
    /// <param name="str">源字符串</param>
    /// <returns>加密后字符串</returns>
    public static string MD5Encrypt(string str)
    {
        MD5 md5 = MD5.Create();
        byte[] hs = md5.ComputeHash(Encoding.UTF8.GetBytes(str));
        StringBuilder stb = new StringBuilder();
        foreach (byte b in hs)
        {
            stb.Append(b.ToString("x2"));
        }
        return stb.ToString();
    }


}
