using Com.Common;
using Grpc.Core;
using GrpcExchange;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Com.Server;


public class GreeterImpl : ExchangeService.ExchangeServiceBase
{

    private readonly ILogger<GreeterImpl> _logger;
    public GreeterImpl(ILogger<GreeterImpl> logger)
    {
        _logger = logger;
    }
    
    // Server side handler of the SayHello RPC
    public override Task<Reply> gRPC_Call(Request request, ServerCallContext context)
    {
        // Program.Shutdown.Set(); // <--- Signals the main thread to continue 
        return Task.FromResult(new Reply { Message = "Hello " + request.Json });
    }
}