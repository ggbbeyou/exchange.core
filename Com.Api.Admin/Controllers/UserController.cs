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
    public UserController(ILogger<UserController> logger, IConfiguration config)
    {
        this.logger = logger;
        this.config = config;
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
    /// 申请手机验证
    /// </summary>
    /// <param name="phone">手机号</param>
    /// <returns></returns>
    [HttpPost]
    [Route("ApplyPhone")]
    public Res<bool> ApplyPhone(string phone)
    {
        Res<bool> res = new Res<bool>();
        res.success = false;
        res.code = E_Res_Code.fail;
        res.data = false;
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                Users? user = db.Users.AsNoTracking().SingleOrDefault(P => P.user_id == this.login.user_id);
                if (user == null || user.disabled == true || user.verify_phone == true)
                {
                    res.success = false;
                    res.code = E_Res_Code.apply_fail;
                    res.data = false;
                    res.message = "用户被禁用或已经验证过";
                    return res;
                }
                else
                {
                    string code = common.CreateRandomCode(6);
#if (DEBUG)
                    code = "123456";
#endif
                    string content = $"Exchange 手机验证码:{code}";
                    if (service_common.SendPhone(phone, content))
                    {
                        FactoryService.instance.constant.redis.StringSet(FactoryService.instance.GetRedisVerificationCode(this.login.user_id + phone.Trim()), code, TimeSpan.FromMinutes(10));
                        res.success = true;
                        res.code = E_Res_Code.ok;
                        res.data = true;
                        return res;
                    }
                }
            }
        }
        return res;
    }

    /// <summary>
    /// 验证手机申请
    /// </summary>
    /// <param name="phone">手机号</param>
    /// <param name="code">短信验证码</param>
    /// <returns></returns>
    [HttpPost]
    [Route("VerifyPhone")]
    public Res<bool> VerifyPhone(string phone, string code)
    {
        Res<bool> res = new Res<bool>();
        res.success = false;
        res.code = E_Res_Code.fail;
        res.data = false;
        RedisValue rv = FactoryService.instance.constant.redis.StringGet(FactoryService.instance.GetRedisVerificationCode(this.login.user_id + phone.Trim()));
        if (rv.HasValue)
        {
            if (rv.ToString().ToUpper() == code.Trim().ToUpper())
            {
                using (var scope = FactoryService.instance.constant.provider.CreateScope())
                {
                    using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
                    {
                        Users? user = db.Users.SingleOrDefault(P => P.user_id == this.login.user_id);
                        if (user == null || user.disabled == true || user.verify_phone == true)
                        {
                            res.success = false;
                            res.code = E_Res_Code.apply_fail;
                            res.data = false;
                            res.message = "用户被禁用或已经验证过";
                            return res;
                        }
                        else
                        {
                            user.verify_phone = true;
                            user.phone = phone.Trim();
                            if (db.SaveChanges() > 0)
                            {
                                res.success = true;
                                res.code = E_Res_Code.ok;
                                res.data = true;
                                return res;
                            }
                        }
                    }
                }
            }
            else
            {
                res.success = false;
                res.code = E_Res_Code.verification_error;
                res.data = false;
                res.message = "验证码错误";
                return res;
            }
        }
        return res;
    }

    /// <summary>
    /// 申请Google验证
    /// </summary>   
    /// <returns></returns>
    [HttpPost]
    [Route("ApplyGoogle")]
    public Res<string?> ApplyGoogle()
    {
        Res<string?> res = new Res<string?>();
        res.success = false;
        res.code = E_Res_Code.fail;
        res.data = service_common.CreateGoogle2FA(FactoryService.instance.constant.config["Jwt:Issuer"], this.login.user_id);
        if (string.IsNullOrWhiteSpace(res.data))
        {
            res.success = false;
            res.code = E_Res_Code.verification_disable;
            res.message = "用户被禁用或已申请过验证";
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
    /// 验证google申请
    /// </summary>
    /// <param name="code">google验证码</param>
    /// <returns></returns>
    [HttpPost]
    [Route("VerifyGoogle")]
    public Res<bool> VerifyGoogle(string code)
    {
        Res<bool> res = new Res<bool>();
        res.success = false;
        res.code = E_Res_Code.fail;
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                Users? user = db.Users.SingleOrDefault(P => P.user_id == this.login.user_id);
                if (user == null || user.disabled == true || user.verify_google == true || string.IsNullOrWhiteSpace(user.google_key))
                {
                    res.success = false;
                    res.code = E_Res_Code.apply_fail;
                    res.data = false;
                    res.message = "用户被禁用或已经验证过";
                    return res;
                }
                else
                {
                    res.data = service_common.Verification2FA(user.google_key, code);
                    if (res.data == false)
                    {
                        res.success = false;
                        res.code = E_Res_Code.verification_error;
                        res.message = "验证码错误";
                        return res;
                    }
                    else
                    {
                        user.verify_google = true;
                        db.Users.Update(user);
                        if (db.SaveChanges() > 0)
                        {
                            res.success = true;
                            res.code = E_Res_Code.ok;
                            return res;
                        }
                    }
                }
            }
        }
        return res;
    }

    /// <summary>
    /// 验证实名认证
    /// </summary>
    /// <param name="files">实名凭证</param>
    /// <returns></returns>
    [HttpPost]
    [Route("ApplyRealname")]
    public async Task<Res<bool>> ApplyRealname(IFormFile files)
    {
        Res<bool> res = new Res<bool>();
        res.success = false;
        res.code = E_Res_Code.fail;
        res.data = false;
        if (files == null || files.Length <= 0)
        {
            res.success = false;
            res.code = E_Res_Code.file_not_found;
            res.data = false;
            res.message = "未找到文件";
            return res;
        }
        ServiceMinio service_minio = new ServiceMinio(config, logger);
        string object_name = FactoryService.instance.constant.worker.NextId().ToString() + Path.GetExtension(files.FileName);
        await service_minio.UploadFile(files.OpenReadStream(), FactoryService.instance.GetMinioRealname(), object_name, files.ContentType);
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                Users? users = db.Users.SingleOrDefault(P => P.user_id == this.login.user_id);
                if (users == null || users.disabled == true || users.verify_realname == E_Verify.verify_ok)
                {
                    res.success = false;
                    res.code = E_Res_Code.apply_fail;
                    res.data = false;
                    res.message = "用户被禁用或已经验证过";
                    return res;
                }
                else
                {
                    users.realname_object_name = object_name;
                    users.verify_realname = E_Verify.verify_apply;
                    db.Users.Update(users);
                    if (db.SaveChanges() > 0)
                    {
                        res.success = true;
                        res.code = E_Res_Code.ok;
                        res.data = true;
                        return res;
                    }
                }
            }
        }
        return res;
    }

    /// <summary>
    /// 申请api用户
    /// </summary>
    /// <param name="code">google验证码</param>
    /// <param name="name">名称</param>
    /// <param name="transaction">是否可交易</param>
    /// <param name="withdrawal">是否可提现</param>
    /// <param name="ip">白名单,多个使用;进行隔离</param>
    /// <returns></returns>
    [HttpPost]
    [Route("ApplyApiUser")]
    public Res<KeyValuePair<string, string>> ApplyApiUser(string code, string? name, bool transaction, bool withdrawal, string ip)
    {
        Res<KeyValuePair<string, string>> res = new Res<KeyValuePair<string, string>>();
        res.success = false;
        res.code = E_Res_Code.fail;
        res.data = new KeyValuePair<string, string>();
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                UsersApi api = new UsersApi()
                {
                    id = FactoryService.instance.constant.worker.NextId(),
                    name = name,
                    user_id = this.login.user_id,
                    api_key = Encryption.SHA256Encrypt(FactoryService.instance.constant.worker.NextId().ToString()),
                    api_secret = Encryption.SHA256Encrypt(FactoryService.instance.constant.worker.NextId().ToString()) + Encryption.SHA256Encrypt(FactoryService.instance.constant.worker.NextId().ToString()),
                    transaction = transaction,
                    withdrawal = withdrawal,
                    white_list_ip = ip,
                    create_time = DateTimeOffset.UtcNow,
                };
                db.UsersApi.Add(api);
                if (db.SaveChanges() > 0)
                {
                    res.success = true;
                    res.code = E_Res_Code.ok;
                    res.data = new KeyValuePair<string, string>(api.api_key, api.api_secret);
                    return res;
                }
            }
        }
        return res;
    }

    /// <summary>
    /// 修改api用户信息
    /// </summary>
    /// <param name="code">google验证码</param>
    /// <param name="id">数据id</param>
    /// <param name="name">名称</param>
    /// <param name="transaction">是否可交易</param>
    /// <param name="withdrawal">是否可提现</param>
    /// <param name="ip">白名单,多个使用;进行隔离</param>
    /// <returns></returns>
    [HttpPost]
    [Route("UpdateApiUser")]
    public Res<bool> UpdateApiUser(string code, long id, string? name, bool transaction, bool withdrawal, string ip)
    {
        Res<bool> res = new Res<bool>();
        res.success = false;
        res.code = E_Res_Code.fail;
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                UsersApi? api = db.UsersApi.AsNoTracking().SingleOrDefault(P => P.id == id);
                if (api != null)
                {
                    api.name = name;
                    api.transaction = transaction;
                    api.withdrawal = withdrawal;
                    api.white_list_ip = ip;
                    db.UsersApi.Update(api);
                    if (db.SaveChanges() > 0)
                    {
                        res.success = true;
                        res.code = E_Res_Code.ok;
                        res.data = true;
                        return res;
                    }
                }
            }
        }
        return res;
    }

    /// <summary>
    /// 获取api用户
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("GetApiUser")]
    public Res<List<ResUsersApi>> GetApiUser()
    {
        Res<List<ResUsersApi>> res = new Res<List<ResUsersApi>>();
        res.success = false;
        res.code = E_Res_Code.fail;
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                res.success = true;
                res.code = E_Res_Code.ok;
                res.data = db.UsersApi.AsNoTracking().Where(P => P.user_id == this.login.user_id).ToList().ConvertAll(P => (ResUsersApi)P);
            }
        }
        return res;
    }

}