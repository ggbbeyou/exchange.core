using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
    /// 登录
    /// </summary>
    /// <param name="email">emal)</param>
    /// <param name="password">密码</param>
    /// <param name="code">验证码</param>
    /// <param name="app">终端</param>
    /// <returns></returns>
    [HttpPost]
    [Route("login")]
    public Res<ResUser> Login(string email, string password, string code, string app)
    {
        string ip = "";
        if (Request.Headers.TryGetValue("X-Real-IP", out var ip_addr))
        {
            ip = ip_addr;
        }
        return service_user.Login(email, password, code, app, ip);
    }

    /// <summary>
    /// 注册时发送Email验证码
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public Res<bool> SendVerificationByRegister(string email)
    {
        Res<bool> res = new Res<bool>();
        res.success = true;
        res.code = E_Res_Code.ok;
        if (string.IsNullOrWhiteSpace(email))
        {
            res.success = false;
            res.code = E_Res_Code.email_not_found;
            res.message = "邮箱不能为空";
            res.data = false;
            return res;
        }
        email = email.Trim().ToLower();
        string code = "123456";
        string content = $"Exchange Code:{code}";
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                if (!db.Users.Any(P => P.email == email))
                {
                    if (service_common.SendEmail(email, content))
                    {
                        FactoryService.instance.constant.redis.StringSet(FactoryService.instance.GetRedisVerificationCode(email), code, TimeSpan.FromMinutes(10));
                    }
                }
            }
        }
        res.data = true;
        return res;
    }


    // /// <summary>
    // /// 5:获取图形验证码
    // /// </summary>
    // /// <returns></returns>  
    // [HttpGet]
    // [Route("GetVerificationCode")]
    // public Res<KeyValuePair<string, string>> GetVerificationCode()
    // {
    //     Res<KeyValuePair<string, string>> res = new Res<KeyValuePair<string, string>>();
    //     res.success = true;
    //     res.code = E_Res_Code.ok;
    //     (long no, string code) verifiction = service_common.GetVerificationCode();
    //     res.data = new KeyValuePair<string, string>(verifiction.no.ToString(), verifiction.code);
    //     return res;
    // }

}