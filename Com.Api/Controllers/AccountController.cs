using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Com.Api.Sdk.Enum;
using Com.Api.Sdk.Models;
using Com.Bll;
using Com.Bll.Util;
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
    /// db
    /// </summary>
    private readonly DbContextEF db;
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
    /// 公共类
    /// </summary>
    /// <returns></returns>
    private Common common = new Common();

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="logger">日志接口</param>
    /// <param name="db">db</param>
    public AccountController(ILogger<AccountController> logger, DbContextEF db)
    {
        this.logger = logger;
        this.db = db;
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
    public Res<bool> Register(string email, string password, string code, string? recommend)
    {
        return service_user.Register(email, password, code, recommend, Request.GetIp());
    }

    /// <summary>
    /// 登录
    /// </summary>
    /// <param name="email">email</param>
    /// <param name="password">密码</param>
    /// <param name="app">终端</param>
    /// <returns></returns>
    [HttpPost]
    [Route("login")]
    public Res<ResUser> Login(string email, string password, E_App app)
    {
        return service_user.Login(email, password, app, Request.GetIp());
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
        res.code = E_Res_Code.fail;
        res.data = false;
        email = email.Trim().ToLower();
        if (!Regex.IsMatch(email, @"^([a-zA-Z0-9_-])+@([a-zA-Z0-9_-])+((\.[a-zA-Z0-9_-]{2,3}){1,2})$"))
        {
            res.code = E_Res_Code.email_irregularity;
            res.msg = "邮箱格式错误";
            return res;
        }
        string code = common.CreateRandomCode(6);
#if (DEBUG)
        code = "123456";
#endif
        string content = $"Exchange 注册验证码:{code}";
        if (db.Users.Any(P => P.email.ToLower() == email))
        {
            res.code = E_Res_Code.email_repeat;
            res.msg = "邮箱地址已存在";
            return res;
        }
        else
        {
            if (service_common.SendEmail(email, content))
            {
                FactoryService.instance.constant.redis.StringSet(FactoryService.instance.GetRedisVerificationCode(email), code, TimeSpan.FromMinutes(10));

                res.code = E_Res_Code.ok;
                res.data = true;
                return res;
            }
        }
        return res;
    }
 
}