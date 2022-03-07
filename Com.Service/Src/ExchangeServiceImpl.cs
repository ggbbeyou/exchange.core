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
        Res<string> res = new Res<string>();
        res.success = true;
        res.code = E_Res_Code.ok;
        Req<string>? req = JsonConvert.DeserializeObject<Req<string>>(request.Json);
        if (req == null)
        {
            res.success = false;
            res.code = E_Res_Code.fail;
            res.message = $"grpc 请求参数为空:{request.Json}";
            FactoryMatching.instance.constant.logger.LogError($"grpc 请求参数为空:{request.Json}");
            reply.Message = JsonConvert.SerializeObject(res);
            return reply;
        }
        res.op = req.op;
        res.market = req.market;
        res.data = req.data;
        if (req.op == E_Op.service_init)
        {
            BaseMarketInfo? marketInfo = JsonConvert.DeserializeObject<BaseMarketInfo>(req.data);
            if (marketInfo == null)
            {
                res.success = false;
                res.code = E_Res_Code.fail;
                res.message = $"初始化失败,未获取到请求参数:{request.Json}";
                FactoryMatching.instance.constant.logger.LogError($"初始化失败, 未获取到请求参数:{request.Json}");
                reply.Message = JsonConvert.SerializeObject(res);
                return reply;
            }
            BaseMarketInfo result = await FactoryMatching.instance.ServiceInit(marketInfo);

            res.message = $"初始化成功:{marketInfo.market}";
            FactoryMatching.instance.constant.logger.LogInformation($"初始化成功:{marketInfo.market}");
        }
        else if (req.op == E_Op.service_start)
        {
            BaseMarketInfo? marketInfo = JsonConvert.DeserializeObject<BaseMarketInfo>(req.data);
            if (marketInfo == null)
            {
                res.success = false;
                res.code = E_Res_Code.fail;
                res.message = $"服务启动失败,未获取到请求参数:{request.Json}";
                FactoryMatching.instance.constant.logger.LogError($"服务启动失败, 未获取到请求参数:{request.Json}");
                reply.Message = JsonConvert.SerializeObject(res);
                return reply;
            }
            FactoryMatching.instance.ServiceStart(marketInfo);
            res.message = $"服务启动成功:{marketInfo.market}";
            FactoryMatching.instance.constant.logger.LogInformation($"服务启动成功:{marketInfo.market}");
        }
        else if (req.op == E_Op.service_stop)
        {
            BaseMarketInfo? marketInfo = JsonConvert.DeserializeObject<BaseMarketInfo>(req.data);
            if (marketInfo == null)
            {
                res.success = false;
                res.code = E_Res_Code.fail;
                res.message = $"服务停止失败,未获取到请求参数:{request.Json}";
                FactoryMatching.instance.constant.logger.LogError($"服务停止失败, 未获取到请求参数:{request.Json}");
                return reply;
            }
            try
            {
                BaseMarketInfo result = FactoryMatching.instance.ServiceStop(marketInfo);
                res.data = JsonConvert.SerializeObject(result);
                res.message = $"服务停止成功:{marketInfo.market}";
                FactoryMatching.instance.constant.logger.LogInformation($"服务停止成功:{marketInfo.market}");
            }
            catch (System.Exception ex)
            {
                res.success = false;
                res.code = E_Res_Code.fail;
                res.message = ex.Message;
                return reply;
            }
        }
        else
        {
            //其它操作
        }
        reply.Message = JsonConvert.SerializeObject(res);
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