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
// [AllowAnonymous]
public class MarketController : ControllerBase
{
    /// <summary>
    /// 交易对基础信息
    /// </summary>
    /// <returns></returns>
    public ServiceMarket service_market = new ServiceMarket();

    /// <summary>
    /// 获取交易对基本信息
    /// </summary>
    /// <param name="symbol">交易对</param>
    /// <returns></returns>
    [HttpPost]
    [Route("market")]
    public Res<List<ResMarket>> Market(List<string> symbol)
    {
        return service_market.Market(symbol);
    }

    /// <summary>
    /// 获取聚合行情
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("ticker")]
    public Res<List<ResTicker>> Ticker(List<string> symbol)
    {
        return service_market.Ticker(symbol);
    }

    /// <summary>
    /// 获取深度行情
    /// </summary>
    /// <param name="symbol">交易对</param>
    /// <param name="sz">深度档数,只支持10,50,200</param>
    /// <returns></returns>
    [HttpPost]
    [Route("depth")]
    public Res<ResDepth?> Depth(string symbol, int sz = 50)
    {
        return service_market.Depth(symbol, sz);
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
        return service_market.Klines(symbol, type, start, end, skip, take);
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
    [Route("deals")]
    public Res<List<ResDeal>> Deals(string symbol, DateTimeOffset start, DateTimeOffset? end, long skip, long take)
    {
        return service_market.Deals(symbol, start, end, skip, take);
    }

}