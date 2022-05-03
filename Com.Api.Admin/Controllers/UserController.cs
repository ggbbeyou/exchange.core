using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Com.Api.Sdk.Enum;
using Com.Api.Sdk.Models;
using Com.Bll;
using Com.Bll.Util;
using Com.Db;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Com.Api.Admin.Controllers;

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
    /// db
    /// </summary>
    private readonly DbContextEF db;
    /// <summary>
    /// 配置接口
    /// </summary>
    private readonly IConfiguration config;
    /// <summary>
    /// 公共类
    /// </summary>
    /// <returns></returns>
    private Common common = new Common();
    /// <summary>
    /// 登录信息
    /// </summary>
    /// <value></value>
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
    /// <param name="logger">日志接口</param>
    /// <param name="config">配置接口</param>
    /// <param name="db">db</param>
    public UserController(ILogger<UserController> logger, IConfiguration config, DbContextEF db)
    {
        this.logger = logger;
        this.config = config;
        this.db = db;
    }

    /// <summary>
    /// 获取用户
    /// </summary>
    /// <param name="uid">用户id</param>
    /// <param name="user_name">用户名</param>
    /// <param name="email">邮箱地址</param>
    /// <param name="phone">手机号</param>
    /// <param name="skip">跳过多少行</param>
    /// <param name="take">提取多少行</param>
    /// <returns></returns>
    [HttpGet]
    [Route("GetUser")]
    public Res<List<Users>> GetUser(long? uid, string? user_name, string? email, string? phone, int skip = 0, int take = 50)
    {
        Res<List<Users>> res = new Res<List<Users>>();

        res.code = E_Res_Code.ok;
        res.data = service_user.GetUser(uid, user_name, email, phone, skip, take);
        return res;
    }

    /// <summary>
    /// 需要实名认证用户
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("ApplyRealname")]
    public Res<List<ResUser>> ApplyRealname()
    {
        Res<List<ResUser>> res = new Res<List<ResUser>>();

        res.code = E_Res_Code.ok;
        res.data = db.Users.AsNoTracking().Where(P => P.verify_realname == E_Verify.verify_no).ToList().ConvertAll(P => (ResUser)P);
        return res;
    }

    /// <summary>
    /// 实名认证用户
    /// </summary>
    /// <param name="uid">用户id</param>
    /// <param name="verify">验证方式</param>
    /// <returns></returns>
    [HttpPost]
    [Route("VerifyRealname")]
    public Res<bool> VerifyRealname(long uid, E_Verify verify)
    {
        Res<bool> res = new Res<bool>();
        Users? users = db.Users.FirstOrDefault(P => P.user_id == uid && P.verify_realname == E_Verify.verify_no);
        if (users == null)
        {
            res.code = E_Res_Code.fail;
            res.message = "用户不存在或已实名认证";
            res.data = false;
            return res;
        }
        users.verify_realname = verify;
        db.SaveChanges();

        res.code = E_Res_Code.ok;
        res.data = true;
        return res;
    }



}