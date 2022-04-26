using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

using Com.Bll;
using Com.Db;
using Com.Api.Sdk.Enum;

using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Com.Api.Sdk.Models;
using Com.Bll.Util;
using Microsoft.EntityFrameworkCore;

namespace Com.Api.Admin.Controllers;

/// <summary>
/// 交易对
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("[controller]")]
public class MarketController : ControllerBase
{
    /// <summary>
    /// 日志接口
    /// </summary>
    private readonly ILogger<MarketController> logger;
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
    /// 
    /// </summary>
    /// <param name="db"></param>
    /// <param name="config"></param>
    /// <param name="logger"></param>
    public MarketController(DbContextEF db, IConfiguration config, ILogger<MarketController> logger)
    {
        this.logger = logger;
        this.config = config;
        this.db = db;
    }

    /// <summary>
    /// 添加交易对
    /// </summary>
    /// <param name="type">交易对类型</param>
    /// <param name="coin_id_base">基础币种id</param>
    /// <param name="coin_id_quote">报价币种id</param>
    /// <param name="places_price">交易价小数位数</param>
    /// <param name="places_amount">交易量小数位数</param>
    /// <param name="trade_min">除了市价卖单外每一笔最小交易额</param>
    /// <param name="trade_min_market_sell">市价卖单每一笔最小交易量</param>
    /// <param name="service_url">服务地址</param>
    /// <param name="market_email">作市账号邮箱</param>
    /// <param name="market_password">作市账号密码</param>
    /// <param name="settlement_email">结算账号邮箱</param>
    /// <param name="settlement_password">结算账号密码</param>
    /// <param name="sort">排序</param>
    /// <param name="tag">标签</param>
    /// <returns>排序</returns>
    [HttpPost]
    [Route("AddMarket")]
    public Res<bool> AddMarket(E_MarketType type, long coin_id_base, long coin_id_quote, int places_price, int places_amount, decimal trade_min, decimal trade_min_market_sell, string service_url, string market_email, string market_password, string settlement_email, string settlement_password, float sort, string? tag)
    {
        Res<bool> res = new Res<bool>();
        res.success = false;
        res.code = E_Res_Code.fail;
        res.data = false;
        if (places_price < 0 || places_amount < 0 || trade_min <= 0 || trade_min_market_sell <= 0)
        {
            res.success = false;
            res.code = E_Res_Code.not_less_0;
            res.message = "值不能小于或等于0";
            return res;
        }
        if (!this.db.Coin.Any(P => P.coin_id == coin_id_base || P.coin_id == coin_id_quote))
        {
            res.success = false;
            res.code = E_Res_Code.coin_not_found;
            res.message = "未找到币种";
            return res;
        }
        if (this.db.Market.Any(P => P.market_type == type && P.coin_id_base == coin_id_base && P.coin_id_quote == coin_id_quote))
        {
            res.success = false;
            res.code = E_Res_Code.name_repeat;
            res.message = "市场已存在";
            return res;
        }
        Coin coin_base = db.Coin.Single(P => P.coin_id == coin_id_base);
        Coin coin_quote = db.Coin.Single(P => P.coin_id == coin_id_quote);
        Market market = new Market();
        market.market = FactoryService.instance.constant.worker.NextId();
        if (type == E_MarketType.spot)
        {
            market.symbol = $"{coin_base.coin_name}/{coin_quote.coin_name}";
        }
        market.market_type = type;
        market.coin_id_base = coin_base.coin_id;
        market.coin_name_base = coin_base.coin_name;
        market.coin_id_quote = coin_quote.coin_id;
        market.coin_name_quote = coin_quote.coin_name;
        market.transaction = true;
        market.status = false;
        market.places_price = places_price;
        market.places_amount = places_amount;
        market.trade_min = trade_min;
        market.trade_min_market_sell = trade_min_market_sell;
        market.last_price = 0;
        market.service_url = service_url;
        market.sort = sort;
        market.tag = tag;
        (string public_key, string private_key) key_res = Encryption.GetRsaKey();
        Users market_user = new Users()
        {
            user_id = FactoryService.instance.constant.worker.NextId(),
            user_name = $"market_{market.market}",
            password = Encryption.SHA256Encrypt(market_password),
            email = market_email,
            phone = null,
            verify_email = true,
            verify_phone = false,
            verify_google = false,
            verify_realname = E_Verify.verify_not,
            realname_object_name = null,
            disabled = false,
            transaction = true,
            withdrawal = false,
            user_type = E_UserType.market,
            recommend = null,
            vip = 0,
            google_key = null,
            public_key = key_res.public_key,
            private_key = key_res.private_key,
        };
        db.Users.Add(market_user);
        market.market_uid = market_user.user_id;
        key_res = Encryption.GetRsaKey();
        Users settlement_user = new Users()
        {
            user_id = FactoryService.instance.constant.worker.NextId(),
            user_name = $"settlement_{market.market}",
            password = Encryption.SHA256Encrypt(settlement_password),
            email = settlement_email,
            phone = null,
            verify_email = true,
            verify_phone = false,
            verify_google = false,
            verify_realname = E_Verify.verify_not,
            realname_object_name = null,
            disabled = false,
            transaction = false,
            withdrawal = false,
            user_type = E_UserType.settlement,
            recommend = null,
            vip = 0,
            google_key = null,
            public_key = key_res.public_key,
            private_key = key_res.private_key,
        };
        db.Users.Add(settlement_user);
        market.settlement_uid = settlement_user.user_id;
        this.db.Market.Add(market);
        if (this.db.SaveChanges() > 0)
        {
            res.success = true;
            res.code = E_Res_Code.ok;
            res.data = true;
            return res;
        }
        return res;
    }

    /// <summary>
    /// 修改交易对
    /// </summary>
    /// <param name="market">交易对id</param>
    /// <param name="transaction">是否交易(true:可以交易,false:禁止交易)</param>
    /// <param name="places_price">交易价小数位数</param>
    /// <param name="places_amount">交易量小数位数</param>
    /// <param name="trade_min">除了市价卖单外每一笔最小交易额</param>
    /// <param name="trade_min_market_sell">市价卖单每一笔最小交易量</param>
    /// <param name="sort">排序</param>
    /// <param name="tag">标签</param>
    /// <param name="service_url">服务地址</param>
    /// <returns></returns>
    [HttpPost]
    [Route("UpdateMarket")]
    public Res<bool> UpdateMarket(long market, bool transaction, int places_price, int places_amount, decimal trade_min, decimal trade_min_market_sell, float sort, string? tag, string service_url)
    {
        Res<bool> res = new Res<bool>();
        res.success = false;
        res.code = E_Res_Code.fail;
        if (!this.db.Market.Any(P => P.market == market))
        {
            res.success = false;
            res.code = E_Res_Code.not_found_symbol;
            res.message = "交易对不存在";
            return res;
        }
        Market obj_market = db.Market.Single(P => P.market == market);
        obj_market.transaction = transaction;
        obj_market.places_price = places_price;
        obj_market.places_amount = places_amount;
        obj_market.trade_min = trade_min;
        obj_market.trade_min_market_sell = trade_min_market_sell;
        obj_market.sort = sort;
        obj_market.tag = tag;
        obj_market.service_url = service_url;
        db.Market.Update(obj_market);
        if (db.SaveChanges() > 0)
        {
            res.success = true;
            res.code = E_Res_Code.ok;
            res.data = true;
            return res;
        }
        return res;
    }

    /// <summary>
    /// 获取交易对列表
    /// </summary>
    /// <param name="symbol">交易对</param>
    /// <returns></returns>
    [HttpGet]
    [Route("GetMarket")]

    public Res<List<Market>> GetMarket(string? symbol)
    {
        Res<List<Market>> res = new Res<List<Market>>();
        res.success = true;
        res.code = E_Res_Code.ok;
        res.data = db.Market.WhereIf(symbol != null, P => P.symbol == symbol!.ToUpper()).AsNoTracking().ToList();
        return res;
    }



}
