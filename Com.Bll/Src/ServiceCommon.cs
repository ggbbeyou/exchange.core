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

    // /// <summary>
    // /// 获取图形验证码
    // /// </summary>
    // /// <returns></returns>
    // public (long, string) GetVerificationCode()
    // {
    //     long no = FactoryService.instance.constant.worker.NextId();
    //     string verify = common.CreateRandomCode(4);
    //     byte[] b = common.CreateImage(verify);
    //     FactoryService.instance.constant.redis.StringSet(FactoryService.instance.GetRedisVerificationCode(no), verify, TimeSpan.FromMinutes(5));
    //     return (no, Convert.ToBase64String(b));
    // }

    /// <summary>
    /// 校验图形验证码
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
    /// 生成token
    /// </summary>
    /// <param name="no">登录唯一码</param>
    /// <param name="user">用户信息</param>
    /// <param name="timeout"></param>
    /// <param name="app"></param>
    /// <returns></returns>
    public string GenerateToken(long no, Users user, string app)
    {
        var claims = new[]
            {
                new Claim("no",no.ToString()),
                new Claim("user_id",user.user_id.ToString()),
                new Claim("user_name",user.user_name),
                new Claim("app", app),
                new Claim("public_key", user.public_key),
            };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(FactoryService.instance.constant.config["Jwt:SecretKey"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: FactoryService.instance.constant.config["Jwt:Issuer"],// 签发者
            audience: FactoryService.instance.constant.config["Jwt:Audience"],// 接收者
            expires: DateTime.Now.AddMinutes(double.Parse(FactoryService.instance.constant.config["Jwt:Expires"])),// 过期时间
            claims: claims,// payload
            signingCredentials: creds);// 令牌
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// 获取登录信息
    /// </summary>
    /// <param name="user_id"></param>
    /// <param name="no"></param>
    /// <param name="user_name"></param>
    /// <param name="app"></param>
    /// <param name="claims_principal"></param>
    /// <returns></returns>
    public (long user_id, long no, string user_name, string app, string public_key) GetLoginUser(System.Security.Claims.ClaimsPrincipal claims_principal)
    {
        string user_name = "", app = "", public_key = "";
        long user_id = 0, no = 0;
        Claim? claim = claims_principal.Claims.FirstOrDefault(P => P.Type == "no");
        if (claim != null)
        {
            no = long.Parse(claim.Value);
        }
        claim = claims_principal.Claims.FirstOrDefault(P => P.Type == "user_id");
        if (claim != null)
        {
            user_id = long.Parse(claim.Value);
        }
        claim = claims_principal.Claims.FirstOrDefault(P => P.Type == "user_name");
        if (claim != null)
        {
            user_name = claim.Value;
        }
        claim = claims_principal.Claims.FirstOrDefault(P => P.Type == "app");
        if (claim != null)
        {
            app = claim.Value;
        }
        claim = claims_principal.Claims.FirstOrDefault(P => P.Type == "public_key");
        if (claim != null)
        {
            public_key = claim.Value;
        }
        return (user_id, no, user_name, app, public_key);
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