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
    /// 需要登录权限的订阅频道
    /// </summary>
    /// <typeparam name="string"></typeparam>
    /// <returns></returns>
    public List<string> login_channel = new List<string>() { "account", "orders", "trades", "books50-l2-tbt", "tickers", "order" };

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
    public async Task<IActionResult> WebSocket()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            try
            {
                WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                Dictionary<string, string> channel = new Dictionary<string, string>();
                bool login = false;
                var buffer = new byte[1024 * 1024];
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                while (!result.CloseStatus.HasValue)
                {
                    string str = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                    if (str.ToLower() == "ping")
                    {
                        byte[] b = System.Text.Encoding.UTF8.GetBytes("pong");
                        await webSocket.SendAsync(new ArraySegment<byte>(b, 0, b.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    else
                    {
                        try
                        {
                            ReqWebsocker? req = JsonConvert.DeserializeObject<ReqWebsocker>(str);
                            Subscribe(webSocket, result, JsonConvert.DeserializeObject<ReqWebsocker>(str), channel, ref login);
                        }
                        catch (System.Exception ex)
                        {
                            byte[] b = System.Text.Encoding.UTF8.GetBytes($"无法解析请求命令:{str},{ex.Message}");
                            await webSocket.SendAsync(new ArraySegment<byte>(b, 0, b.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                        }
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
            catch (System.Exception ex)
            {
                this.constant.logger.LogInformation(ex, $"websocket报错");
            }
        }
        return new EmptyResult();
    }



    /// <summary>
    /// 订阅消息
    /// {"op":"login","args":[{"channel":"","data":"密文"}]}
    /// {"op":"Logout","args":[{"channel":"","data":""}]}
    /// {"op":"subscribe","args":[{"channel":"tickers","data":"btc/usdt"},{"channel":"tickers","data":"eth/usdt"}]}
    /// {"op":"unsubscribe","args":[{"channel":"tickers","data":"btc/usdt"},{"channel":"tickers","data":"eth/usdt"}]}
    /// </summary>
    /// <param name="webSocket"></param>
    /// <param name="result"></param>
    /// <param name="req"></param>
    /// <param name="channel"></param>
    private void Subscribe(WebSocket webSocket, WebSocketReceiveResult result, ReqWebsocker? req, Dictionary<string, string> channel, ref bool login)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.op))
        {
            return;
        }
        if (login == false && req.op == "subscribe")
        {
            List<ReqChannel> Logout = req.args.Where(P => login_channel.Contains(P.channel)).ToList();
            ResWebsocker resWebsocker = new ResWebsocker();
            resWebsocker.success = false;
            resWebsocker.op = req.op;
            foreach (var item in Logout)
            {
                resWebsocker.channel = item.channel;
                resWebsocker.data = item.data;
                resWebsocker.message = "该订阅需要登录权限,请先登录!";
                byte[] b = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(resWebsocker));
                if (webSocket.State == WebSocketState.Open)
                {
                    webSocket.SendAsync(new ArraySegment<byte>(b, 0, b.Length), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
                }
            }
            req.args.RemoveAll(P => Logout.Contains(P));
        }
        if (req.op == "login")
        {
            login = true;
        }
        else if (req.op == "Logout")
        {
            login = false;
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
                            if (webSocket.State == WebSocketState.Open)
                            {
                                await webSocket.SendAsync(new ArraySegment<byte>(b, 0, b.Length), WebSocketMessageType.Text, true, CancellationToken.None);
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
