using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Com.Api.Sdk.Enum;
using Com.Api.Sdk.Models;
using Com.Bll;
using Com.Db;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Com.Api.Controllers;

/// <summary>
/// 账户
/// </summary>
[Route("[controller]")]
[AllowAnonymous]
[ApiController]
public class AccountController : ControllerBase
{
    /// <summary>
    /// 日志接口
    /// </summary>
    private readonly ILogger<AccountController> logger;
    /// <summary>
    /// 用户服务
    /// </summary>
    /// <returns></returns>
    private ServiceUser service_user = new ServiceUser();
    /// <summary>
    /// service:公共服务
    /// </summary>
    private ServiceCommon service_common = new ServiceCommon();

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="logger"></param>
    public AccountController(ILogger<AccountController> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// 注册账号
    /// </summary>
    /// <param name="email">邮箱地址</param>
    /// <param name="password">密码</param>
    /// <param name="code">邮箱验证码</param>
    /// <param name="recommend">推荐人id</param>
    /// <returns></returns>
    [HttpPost]
    [Route("register")]
    public Res<long> Register(string email, string password, string code, string? recommend)
    {
        string ip = "";
        if (Request.Headers.TryGetValue("X-Real-IP", out var ip_addr))
        {
            ip = ip_addr;
        }
        return service_user.Register(email, password, code, recommend, ip);
    }

    /// <summary>
    /// 登录
    /// </summary>
    /// <param name="email">emal)</param>
    /// <param name="password">密码</param>
    /// <param name="app">终端</param>
    /// <returns></returns>
    [HttpPost]
    [Route("login")]
    public Res<ResUser> Login(string email, string password, string app)
    {
        string ip = "";
        if (Request.Headers.TryGetValue("X-Real-IP", out var ip_addr))
        {
            ip = ip_addr;
        }
        return service_user.Login(email, password, app, ip);
    }

    /// <summary>
    /// 注册时发送Email验证码
    /// </summary>
    /// <param name="email">邮件地址</param>
    /// <returns></returns>
    [HttpPost]
    [Route("SendEmailCodeByRegister")]
    public Res<bool> SendEmailCodeByRegister(string email)
    {
        Res<bool> res = new Res<bool>();
        res.success = false;
        res.code = E_Res_Code.fail;
        email = email.Trim().ToLower();
        if (!Regex.IsMatch(email, @"^([a-zA-Z0-9_-])+@([a-zA-Z0-9_-])+((\.[a-zA-Z0-9_-]{2,3}){1,2})$"))
        {
            res.code = E_Res_Code.email_format_error;
            res.message = "邮箱格式错误";
            return res;
        }
        string code = "123456";
        string content = $"Exchange 注册验证码:{code}";
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                if (db.Users.Any(P => P.email == email))
                {
                    res.success = false;
                    res.code = E_Res_Code.email_repeat;
                    res.message = "邮箱地址已存在";
                    res.data = false;
                    return res;
                }
                else
                {
                    if (service_common.SendEmail(email, content))
                    {
                        FactoryService.instance.constant.redis.StringSet(FactoryService.instance.GetRedisVerificationCode(email), code, TimeSpan.FromMinutes(10));
                        res.success = true;
                        res.code = E_Res_Code.ok;
                        return res;
                    }
                }
            }
        }

        return res;
    }

}