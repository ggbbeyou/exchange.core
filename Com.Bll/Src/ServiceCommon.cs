using System.IdentityModel.Tokens.Jwt;
using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using Com.Api.Sdk.Enum;
using Com.Api.Sdk.Models;
using Com.Bll.Util;
using Com.Db;
using Google_Authenticator_netcore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace Com.Bll;

/// <summary>
/// Service:公共服务
/// </summary>
public class ServiceCommon
{
    /// <summary>
    /// 日志接口
    /// </summary>
    private readonly ILogger logger;
  
    /// <summary>
    /// 初始化
    /// </summary>
    public ServiceCommon(ILogger? logger = null)
    {
        this.logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// 生成验证码
    /// </summary>
    /// <param name="n">位数</param>
    /// <returns>验证码字符串</returns>
    public string CreateRandomCode(int n)
    {
        //产生验证码的字符集(去除I 1 l L，O 0等易混字符)
        string charSet = "2,3,4,5,6,8,9,A,B,C,D,E,F,G,H,J,K,M,N,P,R,S,U,W,X,Y";
        string[] CharArray = charSet.Split(',');
        string randomCode = "";
        int temp = -1;
        Random rand = new Random();
        for (int i = 0; i < n; i++)
        {
            if (temp != -1)
            {
                rand = new Random(i * temp * ((int)DateTime.Now.Ticks));
            }
            int t = rand.Next(CharArray.Length - 1);
            if (temp == t)
            {
                return CreateRandomCode(n);
            }
            temp = t;
            randomCode += CharArray[t];
        }
        return randomCode;
    }

    /// <summary>
    /// 压缩字符
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public byte[] Compression(string json)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        using (var compressedStream = new MemoryStream())
        using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
        {
            zipStream.Write(bytes, 0, bytes.Length);
            zipStream.Close();
            bytes = compressedStream.ToArray();
            return bytes;
        }
    }


    /// <summary>
    /// 校验验证码
    /// </summary>
    /// <param name="no">编号</param>
    /// <param name="code">验证码</param>
    /// <returns></returns>
    public bool VerificationCode(string no, string code)
    {
        string verify = FactoryService.instance.constant.redis.StringGet(FactoryService.instance.GetRedisVerificationCode(no));
        if (verify != null && verify.ToLower() == code.ToLower())
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 给用户创建google验证器
    /// </summary>
    /// <param name="issuer">签发者</param>
    /// <param name="user_id">用户id</param>
    /// <returns></returns>
    public string? CreateGoogle2FA(string issuer, long user_id)
    {
        TwoFactorAuthenticator tfa = new TwoFactorAuthenticator();
        string tempKey = CreateRandomCode(40);
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                Users? user = db.Users.FirstOrDefault(P => P.user_id == user_id);
                if (user != null && user.disabled == false && user.verify_google == false)
                {
                    SetupCode setupInfo = tfa.GenerateSetupCode(issuer, user.email, tempKey, 300, 300);
                    user.google_key = tempKey;
                    if (db.SaveChanges() > 0)
                    {
                        return setupInfo.ManualEntryKey;
                    }
                }
            }
        }
        return null;
    }

    /// <summary>
    /// 验证google验证器
    /// </summary>
    /// <param name="google_key">google key</param>
    /// <param name="_2FA">google验证码</param>
    /// <returns></returns>
    public bool Verification2FA(string google_key, string _2FA)
    {
        return new TwoFactorAuthenticator().ValidateTwoFactorPIN(google_key, _2FA);
    }

    /// <summary>
    /// 发送邮件
    /// </summary>
    /// <param name="email">邮箱地址</param>
    /// <param name="content">内容</param>
    /// <returns></returns>
    public bool SendEmail(string email, string content)
    {
        return true;
    }

    /// <summary>
    /// 发送手机短信
    /// </summary>
    /// <param name="phone">手机号码</param>
    /// <param name="content">内容</param>
    /// <returns></returns>
    public bool SendPhone(string phone, string content)
    {
        return true;
    }

}