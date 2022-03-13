using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Com.Api.Models;
using Com.Bll;
using Com.Db;
using Com.Db.Enum;
using Com.Db.Model;
using System.Net.WebSockets;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.AspNetCore.Authorization;

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
        FactoryService.instance.Init(this.constant);

    }


    /// <summary>
    /// websocket处理
    /// </summary>
    /// <returns></returns>
    [AllowAnonymous]
    public async Task<IActionResult> WebSocketUI()
    {
        try
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                Dictionary<string, string> dic = new Dictionary<string, string>();
                var buffer = new byte[1024 * 1024];
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                while (!result.CloseStatus.HasValue)
                {
                    string str = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                    if (str == "ping")
                    {
                        byte[] b = System.Text.Encoding.UTF8.GetBytes("pong");
                        await webSocket.SendAsync(new ArraySegment<byte>(b, 0, b.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    else
                    {
                        Subscribe(webSocket, result, JsonConvert.DeserializeObject<ReqWebsocker<ReqChannel>>(str), dic);
                    }
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                foreach (var item in dic)
                {
                    this.constant.i_model.BasicCancel(item.Value);
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

    /// <summary>
    /// 订阅消息
    /// {"op":"subscribe","args":[{"channel":"tickers","data":"eth/usdt"}]}
    /// 
    /// </summary>
    /// <param name="webSocket"></param>
    /// <param name="result"></param>
    /// <param name="req"></param>
    /// <param name="dic"></param>
    private void Subscribe(WebSocket webSocket, WebSocketReceiveResult result, ReqWebsocker<ReqChannel>? req, Dictionary<string, string> dic)
    {
        if (req == null)
        {
            return;
        }

        if (req.op == "")
        {

        }
        else if(req.op == "subscribe")
        {
            foreach (ReqChannel item in req.args)
            {
                if (item.data == null)
                {
                    continue;
                }
                else
                {
                    long market = FactoryService.instance.market_info_db.GetMarketBySymbol(item.data);
                    if (market == 0)
                    {
                        continue;
                    }
                    else
                    {
                        string key = FactoryService.instance.GetMqTickers(market);
                        if (dic.ContainsKey(key))
                        {
                            continue;
                        }
                        string ConsumerTags = this.constant.MqSubscribe(key, async (b) =>
                        {
                            try
                            {
                                if (webSocket.State == WebSocketState.Open)
                                {
                                    await webSocket.SendAsync(new ArraySegment<byte>(b, 0, b.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                                }
                            }
                            catch (System.Exception ex)
                            {
                                this.constant.logger.LogError(ex, "websocket报错:");
                            }
                        });
                        dic.Add(key, ConsumerTags);
                    }
                }
            }
        }
        else if (req.op == "unsubscribe")
        {
            foreach (ReqChannel item in req.args)
            {
                if (item.data == null)
                {
                    continue;
                }
                else
                {
                    long market = FactoryService.instance.market_info_db.GetMarketBySymbol(item.data);
                    if (market == 0)
                    {
                        continue;
                    }
                    else
                    {
                        string key = FactoryService.instance.GetMqTickers(market);
                        if (dic.ContainsKey(key))
                        {
                            this.constant.i_model.BasicCancel(dic[key]);
                            dic.Remove(key);
                        }
                    }
                }
            }
        }

    }



}
