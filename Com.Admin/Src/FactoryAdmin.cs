using System.Text;
using Com.Db;
using Com.Api.Sdk.Enum;
using Com.Db.Model;
using Grpc.Net.Client;
using GrpcExchange;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace Com.Bll;

/// <summary>
/// 服务工厂
/// </summary>
public class FactoryAdmin
{
    /// <summary>
    /// 单例类的实例
    /// </summary>
    /// <returns></returns>
    public static readonly FactoryAdmin instance = new FactoryAdmin();
    /// <summary>
    /// 常用接口
    /// </summary>
    // public FactoryConstant constant = null!;


    /// <summary>
    /// private构造方法
    /// </summary>
    private FactoryAdmin()
    {

    }

    /// <summary>
    /// 初始化方法
    /// </summary>
    /// <param name="constant"></param>
    // public void Init(FactoryConstant constant)
    // {
    //     this.constant = constant;
    // }

    /// <summary>
    /// 服务:清除所有缓存
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    public async Task<bool> ServiceClearCache(Market info)
    {
        try
        {
            GrpcChannel channel = GrpcChannel.ForAddress(FactoryService.instance.constant.config.GetValue<string>("manage_url"));
            var client = new ExchangeService.ExchangeServiceClient(channel);
            ReqCall<string> req = new ReqCall<string>();
            req.op = E_Op.service_clear_cache;
            req.market = info.market;
            req.data = JsonConvert.SerializeObject(info);
            string json = JsonConvert.SerializeObject(req);
            var reply = await client.UnaryCallAsync(new Request { Json = json });
            channel.ShutdownAsync().Wait();
            return true;
        }
        catch (System.Exception ex)
        {
            FactoryService.instance.constant.logger.LogError(ex, "服务:清除所有缓存");
        }
        return false;
    }

    /// <summary>
    /// 服务:预热缓存
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    public async Task<bool> ServiceWarmCache(Market info)
    {
        try
        {
            GrpcChannel channel = GrpcChannel.ForAddress(FactoryService.instance.constant.config.GetValue<string>("manage_url"));
            var client = new ExchangeService.ExchangeServiceClient(channel);
            ReqCall<string> req = new ReqCall<string>();
            req.op = E_Op.service_warm_cache;
            req.market = info.market;
            req.data = JsonConvert.SerializeObject(info);
            string json = JsonConvert.SerializeObject(req);
            var reply = await client.UnaryCallAsync(new Request { Json = json });
            channel.ShutdownAsync().Wait();
            return true;
        }
        catch (System.Exception ex)
        {
            FactoryService.instance.constant.logger.LogError(ex, "服务:预热缓存");
        }
        return false;
    }

    /// <summary>
    /// 服务:启动服务
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    public async Task<bool> ServiceStart(Market info)
    {
        try
        {
            GrpcChannel channel = GrpcChannel.ForAddress(FactoryService.instance.constant.config.GetValue<string>("manage_url"));
            var client = new ExchangeService.ExchangeServiceClient(channel);
            ReqCall<string> req = new ReqCall<string>();
            req.op = E_Op.service_start;
            req.market = info.market;
            req.data = JsonConvert.SerializeObject(info);
            string json = JsonConvert.SerializeObject(req);
            var reply = await client.UnaryCallAsync(new Request { Json = json });
            channel.ShutdownAsync().Wait();
            return true;
        }
        catch (System.Exception ex)
        {
            FactoryService.instance.constant.logger.LogError(ex, "服务:启动服务");
        }
        return false;
    }

    /// <summary>
    /// 服务:停止服务
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    public async Task<bool> ServiceStop(Market info)
    {
        try
        {
            GrpcChannel channel = GrpcChannel.ForAddress(FactoryService.instance.constant.config.GetValue<string>("manage_url"));
            var client = new ExchangeService.ExchangeServiceClient(channel);
            ReqCall<string> req = new ReqCall<string>();
            req.op = E_Op.service_stop;
            req.market = info.market;
            req.data = JsonConvert.SerializeObject(info);
            string json = JsonConvert.SerializeObject(req);
            var reply = await client.UnaryCallAsync(new Request { Json = json });
            channel.ShutdownAsync().Wait();
            return true;
        }
        catch (System.Exception ex)
        {
            FactoryService.instance.constant.logger.LogError(ex, "服务:停止服务");
        }
        return false;
    }

}