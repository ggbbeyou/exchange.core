using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Com.Api.Models;
using Com.Bll;
using Com.Db;
using Com.Db.Enum;
using Com.Db.Model;
using System.Net.WebSockets;
using Newtonsoft.Json;

namespace Com.Api.Controllers;

public class WebSocketController : Controller
{
    /// <summary>
    /// 常用接口
    /// </summary>
    private FactoryConstant constant = null!;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="environment"></param>
    /// <param name="provider"></param>
    /// <param name="logger"></param>
    public WebSocketController(IServiceProvider provider, IConfiguration configuration, IHostEnvironment environment, ILogger<OrderController> logger)
    {
        this.constant = new FactoryConstant(provider, configuration, environment, logger);
    }


    /// <summary>
    /// websocket处理
    /// </summary>
    /// <returns></returns>
    public async Task<IActionResult> WebSocketUI()
    {
        try
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                // byte[] b = common.Compression(JsonConvert.SerializeObject(response));
                // try
                // {
                //     if (webSocket.State == WebSocketState.Open)
                //     {
                //         webSocket.SendAsync(new ArraySegment<byte>(b, 0, b.Length), WebSocketMessageType.Binary, true, CancellationToken.None);
                //     }
                //     else
                //     {
                //         break;
                //     }
                // }
                // catch (System.Exception ex)
                // {
                //     this.logger.LogError(ex, "websocket_coin发送消息出错:");
                // }

                var buffer = new byte[1024 * 10];
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                while (!result.CloseStatus.HasValue)
                {
                    
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            }
        }
        catch (System.Exception ex)
        {
            this.constant.logger.LogInformation(ex, $"websocket报错");
        }
        return new EmptyResult();
    }
}
