using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Com.Api.Sdk.Enum;
using Com.Api.Sdk.Models;
using Com.Bll.Util;
using Com.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Com.Bll;

/// <summary>
/// Service:用户
/// </summary>
public class ServiceUser
{
    /// <summary>
    /// 公共类
    /// </summary>
    /// <returns></returns>
    private Common common = new Common();

    /// <summary>
    /// 初始化
    /// </summary>
    public ServiceUser()
    {
    }

    #region 辅助方法

    /// <summary>
    /// 登录
    /// </summary>
    /// <param name="account">账号</param>
    /// <param name="password">密码</param>
    /// <param name="no">验证码编号</param>
    /// <param name="code">验证码</param>
    /// <param name="app">登录终端</param>
    /// <param name="ip">登录ip</param>
    /// <returns></returns>
    public Res<ResUser> Login(string account, string password, long no, string code, string app, string ip)
    {
        Res<ResUser> res = new Res<ResUser>();
        res.success = false;
        res.code = E_Res_Code.fail;
        if (!VerificationCode(no, code))
        {
            res.message = "验证码错误";
            return res;
        }
        FactoryService.instance.constant.redis.KeyDelete(FactoryService.instance.GetRedisVerificationCode(no));
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                var user = db.Users.FirstOrDefault(P => P.disabled == false && (P.user_name == account || P.phone == account || P.email == account) && P.password == password);
                if (user == null)
                {
                    res.message = "账户名或密码错误";
                    return res;
                }
                var token = GenerateToken(user, DateTime.UtcNow.AddMonths(12), app);
                res.data = new ResUser
                {
                    user_id = user.user_id,
                    user_name = user.user_name,
                    transaction = user.transaction,
                    withdrawal = user.withdrawal,
                    phone = user.phone,
                    email = user.email,
                    vip = user.vip,
                    public_key = user.public_key,
                    token = token
                };
                res.success = true;
                res.code = E_Res_Code.ok;
                return res;
            }
        }
    }

    public void Register(string user_name, string password, string phone, string email, string app, string ip)
    {

    }

    public void logout()
    {

    }

    /// <summary>
    /// 生成token
    /// </summary>
    /// <param name="user"></param>
    /// <param name="timeout"></param>
    /// <param name="app"></param>
    /// <returns></returns>
    public string GenerateToken(Users user, DateTime timeout, string app)
    {
        var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier,user.user_id.ToString()),
                    new Claim(ClaimTypes.Name,user.user_name),
                    new Claim(ClaimTypes.Rsa, user.public_key),
                    new Claim("app", app),
                };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(user.public_key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: claims,
            expires: timeout,
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// 获取验证码
    /// </summary>
    /// <returns></returns>
    public (long, string) GetVerificationCode()
    {
        long no = FactoryService.instance.constant.worker.NextId();
        string verify = common.CreateRandomCode(4);
        byte[] b = common.CreateImage(verify);
        FactoryService.instance.constant.redis.StringSet(FactoryService.instance.GetRedisVerificationCode(no), verify, TimeSpan.FromMinutes(5));
        return (no, Convert.ToBase64String(b));
    }

    /// <summary>
    /// 校验验证码
    /// </summary>
    /// <param name="no">编号</param>
    /// <param name="code">验证码</param>
    /// <returns></returns>
    public bool VerificationCode(long no, string code)
    {
        string verify = FactoryService.instance.constant.redis.StringGet(FactoryService.instance.GetRedisVerificationCode(no));
        if (verify != null && verify.ToLower() == code.ToLower())
        {
            return true;
        }
        return false;
    }
    #endregion

    /// <summary>
    /// 
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    public Users? GetUser(long uid)
    {
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                return db.Users.AsNoTracking().SingleOrDefault(P => P.user_id == uid);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    public Vip? GetVip(long id)
    {
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                return db.Vip.AsNoTracking().SingleOrDefault(P => P.id == id);
            }
        }
    }





}