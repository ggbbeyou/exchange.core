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
/// 用户接口
/// </summary>
[Route("[controller]")]
[Authorize]
[ApiController]
public class UserController : ControllerBase
{
    /// <summary>
    /// 日志
    /// </summary>
    private readonly ILogger<UserController> logger;
    /// <summary>
    /// 登录信息
    /// </summary>
    private (long no, long user_id, string user_name, E_App app, string public_key) login
    {
        get
        {
            return this.service_user.GetLoginUser(User);
        }
    }
    /// <summary>
    /// service:公共服务
    /// </summary>
    private ServiceCommon service_common = new ServiceCommon();
    /// <summary>
    /// 用户服务
    /// </summary>
    /// <returns></returns>
    private ServiceUser service_user = new ServiceUser();


    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="logger"></param>
    public UserController(ILogger<UserController> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// 登出
    /// </summary>   
    /// <returns></returns>
    [HttpPost]
    [Route("logout")]
    public Res<bool> Logout()
    {
        return this.service_user.Logout(this.login.no, this.login.user_id, this.login.app);
    }

    /// <summary>
    /// 验证手机号码
    /// </summary>   
    /// <returns></returns>
    [HttpPost]
    [Route("VerifyPhone")]
    public Res<bool> VerifyPhone()
    {
        Res<bool> res = new Res<bool>();
        return res;
    }

    /// <summary>
    /// 验证Google验证码
    /// </summary>
    /// <param name="_2FA">google验证码</param>
    /// <returns></returns>
    [HttpPost]
    [Route("VerifyGoogle")]
    public Res<bool> VerifyGoogle(string _2FA)
    {
        Res<bool> res = new Res<bool>();
        res.success = false;
        res.code = E_Res_Code.fail;
        res.data = service_common.Verification2FA(this.login.user_id, _2FA);
        if (res.data == false)
        {
            res.success = false;
            res.code = E_Res_Code.verification_error;
            res.message = "验证码错误";
            return res;
        }
        else
        {
            res.success = true;
            res.code = E_Res_Code.ok;
            return res;
        }
    }

    // <summary>
    /// 创建Google验证码
    /// </summary>   
    /// <returns></returns>
    [HttpPost]
    [Route("CreateGoogle")]
    public Res<string?> CreateGoogle()
    {
        Res<string?> res = new Res<string?>();
        res.success = false;
        res.code = E_Res_Code.fail;
        res.data = service_common.CreateGoogle2FA(FactoryService.instance.constant.config["Jwt:Issuer"], this.login.user_id);
        if (string.IsNullOrWhiteSpace(res.data))
        {
            res.success = false;
            res.code = E_Res_Code.user_disable;
            res.message = "用户已禁用";
            return res;
        }
        else
        {
            res.success = true;
            res.code = E_Res_Code.ok;
            return res;
        }
    }

    /// <summary>
    /// 验证实名认证
    /// </summary>   
    /// <returns></returns>
    [HttpPost]
    [Route("VerifyRealname")]
    public Res<bool> VerifyRealname()
    {
        Res<bool> res = new Res<bool>();
        return res;
    }




}