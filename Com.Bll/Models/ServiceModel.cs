using System;
using Com.Api.Sdk.Enum;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Com.Bll.Models;

/// <summary>
/// 所有服务模型
/// </summary>
public class ServiceModel
{
    /// <summary>
    /// 日志接口
    /// </summary>
    private readonly ILogger logger;
    /// <summary>
    /// 公共类
    /// </summary>
    public readonly ServiceCommon service_common;
    /// <summary>
    /// Service:交易记录
    /// </summary>
    public readonly ServiceDeal service_deal;
    /// <summary>
    /// Service:深度行情
    /// </summary>
    public readonly ServiceDepth service_depth;
    /// <summary>
    /// Service:K线
    /// </summary>
    public readonly ServiceKline service_kline;
    /// <summary>
    /// Db:交易对
    /// </summary>
    public readonly ServiceMarket service_market;
    /// <summary>
    /// Service:订单
    /// </summary>
    public readonly ServiceOrder service_order;
    /// <summary>
    /// 
    /// </summary>
    public readonly ServiceUser service_user;
    /// <summary>
    /// 
    /// </summary>
    public readonly ServiceWallet service_wallet;
    
    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="logger">日志接口</param>
    public ServiceModel(ILogger? logger = null)
    {
        this.logger = logger ?? NullLogger.Instance;
        this.service_common = new ServiceCommon(this.logger);
        this.service_deal = new ServiceDeal(this.logger);
        this.service_depth = new ServiceDepth(this.logger);
        this.service_kline = new ServiceKline(this.logger);
        this.service_market = new ServiceMarket(this.logger);
        this.service_order = new ServiceOrder(this.logger);
        this.service_user = new ServiceUser(this.logger);
        this.service_wallet=new ServiceWallet(this.logger);
    }
}