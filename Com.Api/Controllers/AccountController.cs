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
[Route("[controller]")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly ILogger<AccountController> logger;
    /// <summary>
    /// 登录玩家id
    /// </summary>
    /// <value></value>
    public int uid
    {
        get
        {
            Claim? claim = User.Claims.FirstOrDefault(P => P.Type == JwtRegisteredClaimNames.Aud);
            if (claim != null)
            {
                return Convert.ToInt32(claim.Value);
            }
            return 5;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <value></value>
    public string user_name
    {
        get
        {
            Claim? claim = User.Claims.FirstOrDefault(P => P.Type == JwtRegisteredClaimNames.Aud);
            if (claim != null)
            {
                return (claim.Value);
            }
            return "";
        }
    }
    /// <summary>
    /// 用户服务
    /// </summary>
    /// <returns></returns>
    private ServiceUser user_service = new ServiceUser();

    /// <summary>
    /// 交易对基础信息
    /// </summary>
    /// <returns></returns>
    public ServiceMarket service_market = new ServiceMarket();

    /// <summary>
    /// Service:订单
    /// </summary>
    public ServiceOrder service_order = new ServiceOrder();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    public AccountController(ILogger<AccountController> logger)
    {
        this.logger = logger;
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
    [AllowAnonymous]
    public Res<ResUser> Login(string account, string password, long no, string code, string app)
    {
        string ip = "";
        if (Request.Headers.TryGetValue("X-Real-IP", out var ip_addr))
        {
            ip = ip_addr;
        }
        return user_service.Login(account, password, no, code, app, ip);
    }


    /// <summary>
    /// 5:获取图形验证码
    /// </summary>
    /// <returns></returns>  
    [HttpGet]
    [AllowAnonymous]
    [Route("GetVerificationCode")]
    public Res<KeyValuePair<string, string>> GetVerificationCode()
    {
        Res<KeyValuePair<string, string>> res = new Res<KeyValuePair<string, string>>();
        res.success = true;
        res.code = E_Res_Code.ok;
        (long no, string code) verifiction = user_service.GetVerificationCode();
        res.data = new KeyValuePair<string, string>(verifiction.no.ToString(), verifiction.code);
        return res;
    }

}