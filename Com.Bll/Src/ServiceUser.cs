using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
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
    /// service:公共服务
    /// </summary>
    private ServiceCommon service_common = new ServiceCommon();

    /// <summary>
    /// 初始化
    /// </summary>
    public ServiceUser()
    {
    }

    /// <summary>
    /// 注册
    /// </summary>
    /// <param name="email">Email</param>
    /// <param name="password">密码</param>
    /// <param name="code">邮箱验证码</param>
    /// <param name="recommend">推荐人id</param>
    /// <param name="ip">ip地址</param>
    public Res<bool> Register(string email, string password, string code, string? recommend, string ip)
    {
        Res<bool> res = new Res<bool>();
        res.success = false;
        res.code = E_Res_Code.fail;
        res.data = false;
        if (!Regex.IsMatch(email, @"^([a-zA-Z0-9_-])+@([a-zA-Z0-9_-])+((\.[a-zA-Z0-9_-]{2,3}){1,2})$"))
        {
            res.code = E_Res_Code.email_format_error;
            res.message = "邮箱格式错误";
            return res;
        }
        if (!Regex.IsMatch(password, @"
                            (?=.*[0-9])                     #必须包含数字
                            (?=.*[a-zA-Z])                  #必须包含小写或大写字母
                            (?=([\x21-\x7e]+)[^a-zA-Z0-9])  #必须包含特殊符号
                            .{6,20}                         #至少6个字符,最多20个字符
                            ", RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace))
        {
            res.code = E_Res_Code.password_format_error;
            res.message = "密码必须包含数字、小写字母或大写字母、特殊符号,长度6-20位";
            return res;
        }
        if (!service_common.VerificationCode(email, code))
        {
            res.code = E_Res_Code.verification_error;
            res.message = "验证码错误";
            return res;
        }
        (string public_key, string private_key) key_res = Encryption.GetRsaKey();
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                if (db.Users.Any(P => P.email == email))
                {
                    res.code = E_Res_Code.email_repeat;
                    res.message = "邮箱已重复";
                    return res;
                }
                Vip? vip0 = db.Vip.SingleOrDefault(P => P.name == "vip0");
                string user_name = FactoryService.instance.constant.random.NextInt64(10_001_000, 99_999_999).ToString();
                while (db.Users.Any(P => P.user_name == user_name))
                {
                    user_name = FactoryService.instance.constant.random.NextInt64(10_001_000, 99_999_999).ToString();
                }
                Users settlement_btc_usdt = new Users()
                {
                    user_id = FactoryService.instance.constant.worker.NextId(),
                    user_name = user_name,
                    password = Encryption.SHA256Encrypt(password),
                    email = email,
                    phone = null,
                    verify_email = false,
                    verify_phone = false,
                    verify_google = false,
                    verify_realname = false,
                    disabled = false,
                    transaction = true,
                    withdrawal = false,
                    user_type = E_UserType.general,
                    vip = vip0?.id ?? 0,
                    google_key = null,
                    google_private_key = null,
                    public_key = key_res.public_key,
                    private_key = key_res.private_key,
                };
                db.Users.Add(settlement_btc_usdt);
                if (db.SaveChanges() > 0)
                {
                    res.success = true;
                    res.code = E_Res_Code.ok;
                    res.data = true;
                    return res;
                }
            }
        }
        return res;
    }

    /// <summary>
    /// 登录
    /// </summary>
    /// <param name="email">账号</param>
    /// <param name="password">密码</param>
    /// <param name="no">验证码编号</param>
    /// <param name="code">验证码</param>
    /// <param name="app">登录终端</param>
    /// <param name="ip">登录ip</param>
    /// <returns></returns>
    public Res<ResUser> Login(string email, string password, string app, string ip)
    {
        Res<ResUser> res = new Res<ResUser>();
        res.success = false;
        res.code = E_Res_Code.fail;

        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                var user = db.Users.FirstOrDefault(P => P.disabled == false && (P.phone == email || P.email == email) && P.password == password);
                if (user == null)
                {
                    res.message = "账户名或密码错误";
                    return res;
                }
                FactoryService.instance.constant.redis.KeyDelete(FactoryService.instance.GetRedisVerificationCode(email));
                var token = service_common.GenerateToken(FactoryService.instance.constant.worker.NextId(), user, app);
                res.data = user;
                res.data.token = token;
                res.success = true;
                res.code = E_Res_Code.ok;
                return res;
            }
        }
    }

    public void logout()
    {

    }

    /// <summary>
    /// 获取用户
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
    /// 获取vip
    /// </summary>
    /// <param name="id"></param>
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


    /// <summary>
    /// 获取user api用户
    /// </summary>
    /// <param name="api_key"></param>
    /// <returns></returns>
    public UsersApi? GetApi(string api_key)
    {
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                return db.UsersApi.AsNoTracking().SingleOrDefault(P => P.api_key == api_key);
            }
        }
    }

    /// <summary>
    /// 判断Api账户是否可以交易
    /// </summary>
    /// <param name="api_key"></param>
    /// <returns></returns>
    public (bool, Users?, UsersApi?) ApiUserTransaction(string api_key)
    {
        UsersApi? api = GetApi(api_key);
        if (api == null || !api.transaction)
        {
            return (false, null, null);
        }
        Users? users = GetUser(api.user_id);
        if (users == null || users.disabled || !users.transaction)
        {
            return (false, null, null);
        }
        return (true, users, api);
    }

    /// <summary>
    /// 判断Api账户是否可以交易
    /// </summary>
    /// <param name="api_key"></param>
    /// <returns></returns>
    public (bool, Users?, UsersApi?) ApiUserWithdraw(string api_key)
    {
        UsersApi? api = GetApi(api_key);
        if (api == null || !api.withdrawal)
        {
            return (false, null, null);
        }
        Users? users = GetUser(api.user_id);
        if (users == null || users.disabled || !users.withdrawal)
        {
            return (false, null, null);
        }
        return (true, users, api);
    }

    /// <summary>
    /// 判断Api账户是否可以交易
    /// </summary>
    /// <param name="uid">用户id</param>
    /// <returns></returns>
    public (bool, Users?) UserTransaction(long uid)
    {
        Users? users = GetUser(uid);
        if (users == null || users.disabled || !users.transaction)
        {
            return (false, null);
        }
        return (true, users);
    }

    /// <summary>
    /// 判断Api账户是否可以交易
    /// </summary>
    /// <param name="uid">用户id</param>
    /// <returns></returns>
    public (bool, Users?) UserWithdraw(long uid)
    {
        Users? users = GetUser(uid);
        if (users == null || users.disabled || !users.disabled)
        {
            return (false, null);
        }
        return (true, users);
    }


}