using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Com.Bll;
using Com.Db;
using Com.Api.Sdk.Enum;

using System.Net.WebSockets;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.AspNetCore.Authorization;
using StackExchange.Redis;
using Com.Api.Sdk.Models;

namespace Com.Api.Controllers;

/// <summary>
/// 
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("[controller]")]
public class WebSocketController : ControllerBase
{
    /// <summary>
    /// 需要登录权限的订阅频道
    /// </summary>    
    /// <returns></returns>
    private List<E_WebsockerChannel> login_channel = new List<E_WebsockerChannel>() { E_WebsockerChannel.assets, E_WebsockerChannel.orders };
    /// <summary>
    /// pong
    /// </summary>
    /// <returns></returns>
    private byte[] pong = System.Text.Encoding.UTF8.GetBytes("pong");

    /// <summary>
    /// 交易对基础信息
    /// </summary>
    /// <returns></returns>
    public ServiceMarket market_info_service = new ServiceMarket();

    /// <summary>
    /// 
    /// </summary>
    public WebSocketController()
    {

    }

    /// <summary>
    /// websocket处理
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("push")]
    public async Task<IActionResult> WebSocket()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            try
            {
                WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                Dictionary<string, string> channel = new Dictionary<string, string>();
                await new TaskFactory().StartNew(async () =>
                {
                    while (true)
                    {
                        await Task.Delay(1000 * 60 * 5);
                        if (webSocket.State == WebSocketState.Closed)
                        {
                            foreach (var item in channel)
                            {
                                FactoryService.instance.constant.MqDeleteConsumer(item.Value);
                            }
                            channel.Clear();
                            break;
                        }
                    }
                });
                long uid = 0;
                var buffer = new byte[1024 * 1024];
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                while (!result.CloseStatus.HasValue)
                {
                    Subscribe(webSocket, System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count), channel, ref uid);
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                foreach (var item in channel)
                {
                    FactoryService.instance.constant.MqDeleteConsumer(item.Value);
                }
                channel.Clear();
                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            }
            catch (System.Exception ex)
            {
                FactoryService.instance.constant.logger.LogInformation(ex, $"websocket报错");
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
    /// {"op":"subscribe","args":[{"channel":"books10","data":"btc/usdt"},{"channel":"books10","data":"eth/usdt"}]}
    /// {"op":"subscribe","args":[{"channel":"books200","data":"btc/usdt"},{"channel":"books200","data":"eth/usdt"}]}
    /// </summary>
    /// <param name="webSocket"></param>
    /// <param name="str"></param>
    /// <param name="channel"></param>
    /// <param name="uid"></param>
    private void Subscribe(WebSocket webSocket, string str, Dictionary<string, string> channel, ref long uid)
    {
        if (str.ToLower() == "ping")
        {
            webSocket.SendAsync(new ArraySegment<byte>(pong, 0, pong.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        ReqWebsocker? req = null;
        try
        {
            req = JsonConvert.DeserializeObject<ReqWebsocker>(str);
        }
        catch (System.Exception ex)
        {
            byte[] b = System.Text.Encoding.UTF8.GetBytes($"无法解析请求命令:{str},{ex.Message}");
            webSocket.SendAsync(new ArraySegment<byte>(b, 0, b.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        if (req == null)
        {
            return;
        }
        ResWebsocker<string> resWebsocker = new ResWebsocker<string>();
        resWebsocker.success = true;
        resWebsocker.op = req.op;
        if (uid == 0 && req.op == E_WebsockerOp.subscribe)
        {
            List<ReqChannel> Logout = req.args.Where(P => login_channel.Contains(P.channel)).ToList();
            foreach (var item in Logout)
            {
                resWebsocker.success = false;
                resWebsocker.channel = item.channel;
                resWebsocker.data = item.data;
                resWebsocker.message = "该订阅需要登录权限,请先登录!";
                byte[] b = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(resWebsocker));
                webSocket.SendAsync(new ArraySegment<byte>(b, 0, b.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            req.args.RemoveAll(P => Logout.Contains(P));
        }
        if (req.op == E_WebsockerOp.login)
        {
            //执行登录操作，并设置用户id            
            uid = 1;
            resWebsocker.channel = E_WebsockerChannel.none;
            resWebsocker.data = "";
            resWebsocker.message = "登录成功!";
            byte[] b = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(resWebsocker));
            webSocket.SendAsync(new ArraySegment<byte>(b, 0, b.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        else if (req.op == E_WebsockerOp.Logout)
        {
            uid = 0;
            resWebsocker.channel = E_WebsockerChannel.none;
            resWebsocker.data = "";
            resWebsocker.message = "登出成功!";
            byte[] b = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(resWebsocker));
            webSocket.SendAsync(new ArraySegment<byte>(b, 0, b.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        else if (req.op == E_WebsockerOp.subscribe)
        {
            foreach (ReqChannel item in req.args)
            {
                if (string.IsNullOrWhiteSpace(item.data))
                {
                    continue;
                }
                else
                {
                    Market? market = this.market_info_service.GetMarketBySymbol(item.data);
                    if (market == null)
                    {
                        continue;
                    }
                    else
                    {
                        string key = FactoryService.instance.GetMqSubscribe(item.channel, market.market);
                        if (login_channel.Contains(item.channel))
                        {
                            key = FactoryService.instance.GetMqSubscribe(item.channel, market.market, uid);
                        }
                        if (channel.ContainsKey(key))
                        {
                            continue;
                        }
                        resWebsocker.channel = item.channel;
                        resWebsocker.data = item.data;
                        resWebsocker.message = "订阅成功";
                        string Json = JsonConvert.SerializeObject(resWebsocker);
                        byte[] bb = System.Text.Encoding.UTF8.GetBytes(Json);
                        webSocket.SendAsync(new ArraySegment<byte>(bb, 0, bb.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                        if (item.channel == E_WebsockerChannel.books10_inc || item.channel == E_WebsockerChannel.books50_inc || item.channel == E_WebsockerChannel.books200_inc)
                        {
                            string key_depth = "";
                            if (item.channel == E_WebsockerChannel.books10_inc)
                            {
                                key_depth = E_WebsockerChannel.books10.ToString();
                            }
                            else if (item.channel == E_WebsockerChannel.books50_inc)
                            {
                                key_depth = E_WebsockerChannel.books50.ToString();
                            }
                            else if (item.channel == E_WebsockerChannel.books200_inc)
                            {
                                key_depth = E_WebsockerChannel.books200.ToString();
                            }
                            RedisValue rv = FactoryService.instance.constant.redis.HashGet(FactoryService.instance.GetRedisDepth(market.market), key_depth);
                            ResDepth? depth = JsonConvert.DeserializeObject<ResDepth>(rv);
                            if (depth != null)
                            {
                                ResWebsocker<ResDepth> depth_res = new ResWebsocker<ResDepth>();
                                depth_res.success = true;
                                depth_res.op = E_WebsockerOp.subscribe_date;
                                depth_res.channel = item.channel;
                                depth_res.data = depth;
                                byte[] bb1 = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(depth_res));
                                webSocket.SendAsync(new ArraySegment<byte>(bb1, 0, bb1.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                            }
                        }
                        string ConsumerTags = FactoryService.instance.constant.MqSubscribe(key, async (b) =>
                        {
                            await webSocket.SendAsync(new ArraySegment<byte>(b, 0, b.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                        });
                        channel.Add(key, ConsumerTags);
                    }
                }
            }
        }
        else if (req.op == E_WebsockerOp.unsubscribe)
        {
            foreach (ReqChannel item in req.args)
            {
                if (string.IsNullOrWhiteSpace(item.data))
                {
                    continue;
                }
                else
                {
                    Market? market = this.market_info_service.GetMarketBySymbol(item.data);
                    if (market == null)
                    {
                        continue;
                    }
                    else
                    {
                        string key = FactoryService.instance.GetMqSubscribe(item.channel, market.market);
                        if (login_channel.Contains(item.channel))
                        {
                            key = FactoryService.instance.GetMqSubscribe(item.channel, market.market, uid);
                        }
                        if (channel.ContainsKey(key))
                        {
                            FactoryService.instance.constant.MqDeleteConsumer(channel[key]);
                            channel.Remove(key);
                            resWebsocker.channel = item.channel;
                            resWebsocker.data = item.data;
                            resWebsocker.message = "取消订阅成功";
                            string Json = JsonConvert.SerializeObject(resWebsocker);
                            byte[] bb = System.Text.Encoding.UTF8.GetBytes(Json);
                            webSocket.SendAsync(new ArraySegment<byte>(bb, 0, bb.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                    }
                }
            }
        }

    }



}
