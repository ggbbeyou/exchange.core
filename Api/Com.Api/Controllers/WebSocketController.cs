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
                Dictionary<string, string> channel = new Dictionary<string, string>();
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
                        Subscribe(webSocket, result, JsonConvert.DeserializeObject<ReqWebsocker>(str), channel);
                    }
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                foreach (var item in channel)
                {
                    this.constant.i_model.BasicCancel(item.Value);
                }
                channel.Clear();
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
    /// {"op":"subscribe","args":[{"channel":"tickers","data":"btc/usdt"},{"channel":"tickers","data":"eth/usdt"}]}
    /// {"op":"unsubscribe","args":[{"channel":"tickers","data":"btc/usdt"},{"channel":"tickers","data":"eth/usdt"}]}
    /// </summary>
    /// <param name="webSocket"></param>
    /// <param name="result"></param>
    /// <param name="req"></param>
    /// <param name="channel"></param>
    private void Subscribe(WebSocket webSocket, WebSocketReceiveResult result, ReqWebsocker? req, Dictionary<string, string> channel)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.op))
        {
            return;
        }
        else if (req.op == "subscribe")
        {
            foreach (ReqChannel item in req.args)
            {
                if (string.IsNullOrWhiteSpace(item.data))
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
                        if (channel.ContainsKey(key))
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
                        ResWebsocker res = new ResWebsocker();
                        res.success = true;
                        res.op = req.op;
                        res.channel = item.channel;
                        res.data = item.data;
                        res.message = "订阅成功";
                        string Json = JsonConvert.SerializeObject(res);
                        byte[] bb = System.Text.Encoding.UTF8.GetBytes(Json);
                        if (webSocket.State == WebSocketState.Open)
                        {
                            webSocket.SendAsync(new ArraySegment<byte>(bb, 0, bb.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                        channel.Add(key, ConsumerTags);
                    }
                }
            }
        }
        else if (req.op == "unsubscribe")
        {
            foreach (ReqChannel item in req.args)
            {
                if (string.IsNullOrWhiteSpace(item.data))
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
                        if (channel.ContainsKey(key))
                        {
                            this.constant.i_model.BasicCancel(channel[key]);
                            channel.Remove(key);
                            ResWebsocker res = new ResWebsocker();
                            res.success = true;
                            res.op = req.op;
                            res.channel = item.channel;
                            res.data = item.data;
                            res.message = "取消订阅成功";
                            string Json = JsonConvert.SerializeObject(res);
                            byte[] bb = System.Text.Encoding.UTF8.GetBytes(Json);
                            if (webSocket.State == WebSocketState.Open)
                            {
                                webSocket.SendAsync(new ArraySegment<byte>(bb, 0, bb.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                            }
                        }
                    }
                }
            }
        }

    }



}
