using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Com.Api.Sdk.Enum;
using Com.Api.Sdk.Models;
using Com.Bll.Util;
using Com.Db;
using Google_Authenticator_netcore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Com.Bll;

/// <summary>
/// Service:公共服务
/// </summary>
public class ServiceCommon
{
    /// <summary>
    /// 公共类
    /// </summary>
    /// <returns></returns>
    private Common common = new Common();

    /// <summary>
    /// 初始化
    /// </summary>
    public ServiceCommon()
    {
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
    public string Create2FA(string issuer, long user_id)
    {
        TwoFactorAuthenticator tfa = new TwoFactorAuthenticator();
        string tempKey = common.CreateRandomCode(40);
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                Users? user = db.Users.FirstOrDefault(P => P.user_id == user_id);
                if (user != null)
                {
                    SetupCode setupInfo = tfa.GenerateSetupCode(issuer, user.user_name, tempKey, 300, 300);
                    user.verify_google = true;
                    user.google_key = tempKey;
                    user.google_private_key = setupInfo.ManualEntryKey;
                    db.SaveChanges();
                    return setupInfo.ManualEntryKey;
                }
            }
        }
        return "";
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
    /// <param name="email"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public bool SendEmail(string email, string content)
    {
        return true;
    }

}