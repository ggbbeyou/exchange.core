using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

using Com.Bll;
using Com.Db;
using Com.Api.Sdk.Enum;

using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Com.Api.Sdk.Models;
using Microsoft.EntityFrameworkCore;

namespace Com.Api.Admin.Controllers;

/// <summary>
/// 币种
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("[controller]")]
public class CoinController : ControllerBase
{
    /// <summary>
    /// 日志接口
    /// </summary>
    private readonly ILogger<CoinController> logger;
    /// <summary>
    /// 配置接口
    /// </summary>
    private readonly IConfiguration config;
    /// <summary>
    /// 数据库
    /// </summary>
    public DbContextEF db = null!;
    /// <summary>
    /// service:公共服务
    /// </summary>
    private ServiceCommon service_common = new ServiceCommon();

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
    /// 用户服务
    /// </summary>
    /// <returns></returns>
    private ServiceUser service_user = new ServiceUser();

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="db">db上下文</param>
    /// <param name="config">配置接口</param>
    /// <param name="logger">日志接口</param>
    public CoinController(DbContextEF db, IConfiguration config, ILogger<CoinController> logger)
    {
        this.logger = logger;
        this.db = db;
        this.config = config;
    }

    /// <summary>
    /// 添加币种
    /// </summary>
    /// <param name="coin_name">币名称(大写)</param>
    /// <param name="full_name">币全称</param>
    /// <param name="icon">币图标</param>
    /// <returns></returns>
    [HttpPost]
    [Route("AddCoin")]
    public async Task<Res<bool>> AddCoin(string coin_name, string full_name, IFormFile icon)
    {
        Res<bool> res = new Res<bool>();
        res.code = E_Res_Code.fail;
        res.data = false;
        if (icon == null || icon.Length <= 0)
        {
            res.code = E_Res_Code.file_not_found;
            res.data = false;
            res.msg = "未找到文件";
            return res;
        }
        if (this.db.Coin.Any(P => P.coin_name == coin_name.ToUpper()))
        {
            res.code = E_Res_Code.name_repeat;
            res.data = false;
            res.msg = "币名已重复";
            return res;
        }
        if (this.db.Coin.Any(P => P.full_name == full_name))
        {
            res.code = E_Res_Code.name_repeat;
            res.data = false;
            res.msg = "全名已重复";
            return res;
        }
        Coin coin = new Coin();
        coin.coin_id = FactoryService.instance.constant.worker.NextId();
        coin.coin_name = coin_name.ToUpper();
        coin.full_name = full_name;
        coin.contract = null;
        ServiceMinio service_minio = new ServiceMinio(config, logger);
        string object_name = FactoryService.instance.constant.worker.NextId().ToString() + Path.GetExtension(icon.FileName);
        coin.icon = await service_minio.UploadFile(icon.OpenReadStream(), FactoryService.instance.GetMinioCoin(), object_name, icon.ContentType);
        this.db.Coin.Add(coin);
        if (this.db.SaveChanges() > 0)
        {
            res.code = E_Res_Code.ok;
            res.data = true;
            res.msg = "";
            return res;
        }
        return res;
    }

    /// <summary>
    /// 获取币种列表
    /// </summary>
    /// <param name="coin_name">币名称</param>
    /// <returns></returns>
    [HttpGet]
    [Route("GetCoin")]

    public Res<List<Coin>> GetCoin(string? coin_name)
    {
        Res<List<Coin>> res = new Res<List<Coin>>();
        res.code = E_Res_Code.ok;
        res.data = db.Coin.WhereIf(coin_name != null, P => P.coin_name == coin_name!.ToUpper()).AsNoTracking().ToList();
        return res;
    }


}
