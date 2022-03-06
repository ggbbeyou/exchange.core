using Com.Common;
using Grpc.Core;
using GrpcExchange;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Com.Server;

/// <summary>
/// gRPC服务实现
/// </summary>
public class GreeterImpl : ExchangeService.ExchangeServiceBase
{

    private readonly ILogger<GreeterImpl> _logger;
    public GreeterImpl(ILogger<GreeterImpl> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 一元方法
    /// </summary>
    /// <param name="request"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public override Task<Reply> UnaryCall(Request request, ServerCallContext context)
    {
        return Task.FromResult(new Reply { Message = "Hello " + request.Json });
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