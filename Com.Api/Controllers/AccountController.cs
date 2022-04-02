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
/// 
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("[controller]")]
public class AccountController : ControllerBase
{
    /// <summary>
    /// 
    /// </summary>
    private readonly ILogger<AccountController> logger;
    /// <summary>
    /// 
    /// </summary>
    public (long? user_id, string? no, string? user_name, string? app, string? public_key) user;
    /// <summary>
    /// 用户服务
    /// </summary>
    /// <returns></returns>
    private ServiceUser service_user = new ServiceUser();

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="logger"></param>
    public AccountController(ILogger<AccountController> logger)
    {
        this.logger = logger;
        service_user.GetLoginUser(User);
    }

    /// <summary>
    /// 登录
    /// </summary>
    /// <param name="account">账户(用户/手机号码/emal)</param>
    /// <param name="password">密码</param>
    /// <param name="no">验证码编号</param>
    /// <param name="code">验证码</param>
    /// <param name="app">终端</param>
    /// <returns></returns>
    [HttpPost]
    [Route("login")]
    public Res<ResUser> Login(string account, string password, long no, string code, string app)
    {
        string ip = "";
        if (Request.Headers.TryGetValue("X-Real-IP", out var ip_addr))
        {
            ip = ip_addr;
        }
        return service_user.Login(account, password, no, code, app, ip);
    }

    /// <summary>
    /// 5:获取图形验证码
    /// </summary>
    /// <returns></returns>  
    [HttpGet]
    [Route("GetVerificationCode")]
    public Res<KeyValuePair<string, string>> GetVerificationCode()
    {
        Res<KeyValuePair<string, string>> res = new Res<KeyValuePair<string, string>>();
        res.success = true;
        res.code = E_Res_Code.ok;
        (long no, string code) verifiction = service_user.GetVerificationCode();
        res.data = new KeyValuePair<string, string>(verifiction.no.ToString(), verifiction.code);
        return res;
    }

}