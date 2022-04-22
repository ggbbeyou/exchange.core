using System.Text;
using System.Security.Cryptography;

namespace Com.Bll.Util;

public class Encryption
{
    #region md5

    /// <summary>
    /// MD5 加密字符串,不可逆
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

    #endregion

    #region sha256

    /// <summary>
    /// SHA256加密,不可逆
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    public static string SHA256Encrypt(string content)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        byte[] hash = SHA256.Create().ComputeHash(bytes);
        HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes("com.bll.util.sha256"));
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < hash.Length; i++)
        {
            builder.Append(hash[i].ToString("x2"));
        }
        return builder.ToString();
    }

    /// <summary>
    /// HmacSHA256加密,不可逆
    /// </summary>
    /// <param name="secret">密钥</param>
    /// <param name="content">内容</param>
    /// <returns></returns>
    public static string HmacSHA256Encrypt(string secret, string message)
    {
        using (var hmacsha256 = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
        {
            byte[] hashmessage = hmacsha256.ComputeHash(Encoding.UTF8.GetBytes(message));
            return Convert.ToBase64String(hashmessage);
        }
    }

    #endregion

    #region RSA

    /// <summary>
    ///  生成rsa公钥和私钥
    /// </summary>
    /// <returns></returns>
    public static (string public_key, string private_key) GetRsaKey()
    {
        using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
        {
            string publicKey = rsa.ToXmlString(false); // 公钥
            string privateKey = rsa.ToXmlString(true); // 私钥
            return (publicKey, privateKey);
        }
    }

    /// <summary>
    /// rsa 加密
    /// </summary>
    /// <param name="public_key">公钥</param>
    /// <param name="content">需要加密字符</param>
    /// <returns></returns>
    public static string RSAEncrypt(string public_key, string content)
    {
        using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
        {
            rsa.FromXmlString(public_key);
            byte[] encryptedData = rsa.Encrypt(Encoding.UTF8.GetBytes(content), false);
            return Convert.ToBase64String(encryptedData);
        }
    }

    /// <summary>
    /// rsa 解密
    /// </summary>
    /// <param name="private_key">私钥</param>
    /// <param name="content">需要解密字符</param>
    /// <returns></returns>
    public static string RSADecrypt(string private_key, string content)
    {

        using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
        {
            rsa.FromXmlString(private_key);
            byte[] decryptedData = rsa.Decrypt(Convert.FromBase64String(content), false);
            return Encoding.UTF8.GetString(decryptedData);
        }
    }


    #endregion

}
