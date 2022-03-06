using Com.Common;
using Com.Model;
using Com.Model.Enum;
using Grpc.Core;
using GrpcExchange;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace Com.Server;

/// <summary>
/// gRPC服务实现
/// </summary>
public class GreeterImpl : ExchangeService.ExchangeServiceBase
{

    private readonly ILogger<GreeterImpl> logger;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    public GreeterImpl(ILogger<GreeterImpl> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// 一元方法
    /// </summary>
    /// <param name="request"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public override Task<Reply> UnaryCall(Request request, ServerCallContext context)
    {
        Reply reply = new Reply();
        Res<string> res = new Res<string>();
        Req<BaseMarketInfo>? req = JsonConvert.DeserializeObject<Req<BaseMarketInfo>>(request.Json);
        if (req == null)
        {
            res.success = false;
            res.code = E_Res_Code.fail;
            res.message = $"初始化失败,未获取到请求参数";
            this.logger.LogError($"初始化失败, 未获取到请求参数");
            reply.Message = JsonConvert.SerializeObject(res);
            return Task.FromResult(reply);
        }
        res.op = req.op;
        if (req.op == E_Op.init)
        {
            FactoryMatching.instance.DealDbToRedis(req.data);
            FactoryMatching.instance.KlindDBtoRedis(req.data);
            res.message = $"初始化成功:{req.data.market}";
            this.logger.LogInformation($"初始化成功:{req.data.market}");
        }
        else
        {
            //其它操作
        }
        res.success = true;
        res.code = E_Res_Code.ok;
        reply.Message = JsonConvert.SerializeObject(res);
        return Task.FromResult(reply);
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