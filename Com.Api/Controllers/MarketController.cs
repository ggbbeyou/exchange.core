using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Com.Api.Sdk.Enum;
using Com.Api.Sdk.Models;
using Com.Bll;
using Com.Db;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Com.Api.Controllers;

/// <summary>
/// 行情数据
/// </summary>
[Route("[controller]")]
[ApiController]
[AllowAnonymous]
public class MarketController : ControllerBase
{
    private readonly ILogger<MarketController> logger;
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
    /// Service:交易记录
    /// </summary>
    public ServiceDeal service_deal = new ServiceDeal();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    public MarketController(ILogger<MarketController> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// 获取交易对基本信息
    /// </summary>
    /// <param name="symbol">交易对</param>
    /// <returns></returns>
    [HttpGet]
    [Route("market")]
    public Res<ResMarket?> Market(string symbol)
    {
        Res<ResMarket?> res = new Res<ResMarket?>();
        ResMarket? market = service_market.GetMarketBySymbol(symbol);
        if (market != null)
        {
            res.success = true;
            res.code = E_Res_Code.ok;
            res.data = market;
        }
        return res;
    }

    /// <summary>
    /// 获取聚合行情
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("ticker")]
    public Res<ResTicker?> Ticker(string symbol)
    {
        Res<ResTicker?> res = new Res<ResTicker?>();
        Market? market = service_market.GetMarketBySymbol(symbol);
        if (market != null)
        {
            res.success = true;
            res.code = E_Res_Code.ok;
            res.data = JsonConvert.DeserializeObject<ResTicker>(FactoryService.instance.constant.redis.HashGet(FactoryService.instance.GetRedisTicker(), market.market.ToString()));
        }
        return res;
    }

    /// <summary>
    /// 获取聚合行情
    /// </summary>
    /// <param name="symbol">交易对</param>
    /// <param name="sz">深度档数,只支持10,50,200</param>
    /// <returns></returns>
    [HttpGet]
    [Route("depth")]
    public Res<ResDepth?> Depth(string symbol, int sz = 50)
    {
        Res<ResDepth?> res = new Res<ResDepth?>();
        Market? market = service_market.GetMarketBySymbol(symbol);
        if (market != null && (sz == 10 || sz == 50 || sz == 200))
        {
            res.success = true;
            res.code = E_Res_Code.ok;
            res.data = JsonConvert.DeserializeObject<ResDepth>(FactoryService.instance.constant.redis.HashGet(FactoryService.instance.GetRedisDepth(market.market), "books" + sz));
        }
        return res;
    }

    /// <summary>
    /// 获取K线数据
    /// </summary>
    /// <param name="symbol">交易对</param>
    /// <param name="type">K线类型</param>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    /// <param name="skip">跳过行数</param>
    /// <param name="take">获取行数</param>
    /// <returns></returns>
    [HttpGet]
    [Route("klines")]
    public Res<List<ResKline>?> Klines(string symbol, E_KlineType type, DateTimeOffset start, DateTimeOffset? end, long skip, long take)
    {
        Res<List<ResKline>?> res = new Res<List<ResKline>?>();
        double stop = double.PositiveInfinity;
        if (end != null)
        {
            stop = end.Value.ToUnixTimeMilliseconds();
        }
        Market? market = service_market.GetMarketBySymbol(symbol);
        if (market != null)
        {
            res.success = true;
            res.code = E_Res_Code.ok;
            res.data = new List<ResKline>();
            RedisValue[] rv = FactoryService.instance.constant.redis.SortedSetRangeByScore(key: FactoryService.instance.GetRedisKline(market.market, type), start: start.ToUnixTimeMilliseconds(), stop: stop, exclude: Exclude.Both, skip: skip, take: take, order: StackExchange.Redis.Order.Ascending);
            foreach (var item in rv)
            {
                ResKline? resKline = JsonConvert.DeserializeObject<ResKline>(item);
                if (resKline != null)
                {
                    res.data.Add(resKline);
                }
            }
            if (end == null || (end != null && end >= DateTimeOffset.UtcNow))
            {
                ResKline? resKline = JsonConvert.DeserializeObject<ResKline>(FactoryService.instance.constant.redis.HashGet(FactoryService.instance.GetRedisKlineing(market.market), type.ToString()));
                if (resKline != null)
                {
                    res.data.Add(resKline);
                }
            }
        }
        return res;
    }

    /// <summary>
    /// 获取历史成交记录
    /// </summary>
    /// <param name="symbol">交易对</param>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    /// <param name="skip">跳过行数</param>
    /// <param name="take">获取行数</param>
    /// <returns></returns>
    [HttpGet]
    [Route("klines")]
    public Res<ResDeal?> deals(string symbol, DateTimeOffset start, DateTimeOffset? end, long skip, long take)
    {
        Res<ResDeal?> res = new Res<ResDeal?>();
        double stop = double.PositiveInfinity;
        if (end != null)
        {
            stop = end.Value.ToUnixTimeMilliseconds();
        }
        Market? market = service_market.GetMarketBySymbol(symbol);
        if (market != null)
        {
            RedisValue[] rv = FactoryService.instance.constant.redis.SortedSetRangeByScore(key: FactoryService.instance.GetRedisDeal(market.market), start: start.ToUnixTimeMilliseconds(), stop: stop, exclude: Exclude.Both, skip: skip, take: take, order: StackExchange.Redis.Order.Ascending);
            List<Deal> deals = new List<Deal>();
            foreach (var item in rv)
            {
                Deal? resKline = JsonConvert.DeserializeObject<Deal>(item);
                if (resKline != null)
                {
                    deals.Add(resKline);
                }
            }
            res.data = service_deal.ConvertDeal(market.symbol, deals);
        }
        return res;
    }


}