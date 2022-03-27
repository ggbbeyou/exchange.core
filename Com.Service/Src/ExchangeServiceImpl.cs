using Com.Bll;
using Com.Db;
using Com.Api.Sdk.Enum;

using Grpc.Core;
using GrpcExchange;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Com.Api.Sdk.Models;

namespace Com.Service;

/// <summary>
/// gRPC服务实现
/// </summary>
public class GreeterImpl : ExchangeService.ExchangeServiceBase
{

    /// <summary>
    /// 一元方法
    /// </summary>
    /// <param name="request">请求参数</param>
    /// <param name="context">上下文</param>
    /// <returns></returns>
    public override async Task<Reply> UnaryCall(Request request, ServerCallContext context)
    {
        Reply reply = new Reply();
        ResCall<string> res = new ResCall<string>();
        res.success = true;
        res.code = E_Res_Code.ok;
        ReqCall<string>? req = JsonConvert.DeserializeObject<ReqCall<string>>(request.Json);
        if (req == null)
        {
            res.success = false;
            res.code = E_Res_Code.fail;
            res.message = $"grpc 请求参数为空:{request.Json}";
            FactoryService.instance.constant.logger.LogError($"grpc 请求参数为空:{request.Json}");
            reply.Message = JsonConvert.SerializeObject(res);
            return reply;
        }
        res.op = req.op;
        res.market = req.market;
        res.data = req.data;
        if (req.op == E_Op.service_get_status || req.op == E_Op.service_start || req.op == E_Op.service_stop)
        {
            Market? marketInfo = JsonConvert.DeserializeObject<Market>(req.data);
            if (marketInfo == null)
            {
                res.success = false;
                res.code = E_Res_Code.fail;
                res.message = $"服务(失败):获取服务状态,未获取到请求参数:{request.Json}";
                FactoryService.instance.constant.logger.LogError($"服务(失败):获取服务状态,未获取到请求参数:{request.Json}");
                reply.Message = JsonConvert.SerializeObject(res);
                return reply;
            }
            if (req.op == E_Op.service_get_status)
            {
                marketInfo = FactoryMatching.instance.ServiceGetStatus(marketInfo);
                res.data = JsonConvert.SerializeObject(marketInfo);
                res.message = $"服务(成功):获取服务状态:{marketInfo.market}";
                FactoryService.instance.constant.logger.LogInformation($"服务(成功):获取服务状态:{marketInfo.market}");
            }
            else if (req.op == E_Op.service_start)
            {
                marketInfo = FactoryMatching.instance.ServiceStart(marketInfo);
                res.data = JsonConvert.SerializeObject(marketInfo);
                res.message = $"服务(成功):启动服务:{marketInfo.market}";
                FactoryService.instance.constant.logger.LogInformation($"服务(成功):启动服务:{marketInfo.market}");
            }
            else if (req.op == E_Op.service_stop)
            {
                marketInfo = FactoryMatching.instance.ServiceStop(marketInfo);
                res.data = JsonConvert.SerializeObject(marketInfo);
                res.message = $"服务(成功):关闭服务:{marketInfo.market}";
                FactoryService.instance.constant.logger.LogInformation($"服务(成功):关闭服务:{marketInfo.market}");
            }
        }
        else
        {
            //其它操作
        }
        reply.Message = JsonConvert.SerializeObject(res);
        await Task.CompletedTask;
        return reply;
    }

    /// <summary>
    /// 服务器流式处理方法
    /// </summary>
    /// <param name="request"></param>
    /// <param name="responseStream"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public override async Task StreamingFromServer(Request request, IServerStreamWriter<Reply> responseStream, ServerCallContext context)
    {
        while (!context.CancellationToken.IsCancellationRequested)
        {
            await responseStream.WriteAsync(new Reply());
            await Task.Delay(TimeSpan.FromSeconds(1), context.CancellationToken);
        }
    }
    /// <summary>
    /// 客户端流式处理方法
    /// </summary>
    /// <param name="requestStream"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public override async Task<Reply> StreamingFromClient(IAsyncStreamReader<Request> requestStream, ServerCallContext context)
    {
        await foreach (var message in requestStream.ReadAllAsync())
        {
            // ...
        }
        return new Reply();
    }

    /// <summary>
    /// 双向流式处理方法
    /// </summary>
    /// <param name="requestStream"></param>
    /// <param name="responseStream"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public override async Task StreamingBothWays(IAsyncStreamReader<Request> requestStream, IServerStreamWriter<Reply> responseStream, ServerCallContext context)
    {
        // Read requests in a background task.
        var readTask = Task.Run(async () =>
        {
            await foreach (var message in requestStream.ReadAllAsync())
            {
                // Process request.
            }
        });
        // Send responses until the client signals that it is complete.
        while (!readTask.IsCompleted)
        {
            await responseStream.WriteAsync(new Reply());
            await Task.Delay(TimeSpan.FromSeconds(1), context.CancellationToken);
        }
    }
}