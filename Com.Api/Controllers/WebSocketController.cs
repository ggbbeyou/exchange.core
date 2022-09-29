
/*

websocket工具:http://www.easyswoole.com/wstool.html
websocket地址:ws://localhost/WebSocket/push

公共订阅
{"op":"subscribe","args":[
    {"channel":"tickers","data":"btc/usdt"},
    {"channel":"trades","data":"btc/usdt"},
    {"channel":"books10","data":"btc/usdt"},
    {"channel":"books50","data":"btc/usdt"},
    {"channel":"books200","data":"btc/usdt"},
    {"channel":"books10_inc","data":"btc/usdt"},
    {"channel":"books50_inc","data":"btc/usdt"},
    {"channel":"books200_inc","data":"btc/usdt"},
    {"channel":"min1","data":"btc/usdt"},
    {"channel":"min5","data":"btc/usdt"},
    {"channel":"min15","data":"btc/usdt"},
    {"channel":"min30","data":"btc/usdt"},
    {"channel":"hour1","data":"btc/usdt"},
    {"channel":"hour6","data":"btc/usdt"},
    {"channel":"trades","data":"btc/usdt"},
    {"channel":"hour12","data":"btc/usdt"},
    {"channel":"day1","data":"btc/usdt"},
    {"channel":"week1","data":"btc/usdt"},
    {"channel":"month1","data":"btc/usdt"}
]}

私有订阅
登录:{"op":"login","args":[{"channel":"","data":"{api_key:'你的api用户key',timestamp:时间戳(毫秒),sign:'签名'}"}]}    签名算法 HMACSHA256(secret).ComputeHash(timestamp)
订阅
{"op":"subscribe","args":[
    {"channel":"assets","data":"用户id"},
    {"channel":"orders","data":"用户id"},
]}
登出:{"op":"Logout","args":[{"channel":"","data":""}]}

*/


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
using System.Text;
using Com.Bll.Util;

namespace Com.Api.Controllers;

/// <summary>
/// websocket推送
/// </summary>
[Route("[controller]")]
[AllowAnonymous]
[ApiController]
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
    /// 用户服务
    /// </summary>
    /// <returns></returns>
    private ServiceUser service_user = new ServiceUser();
    /// <summary>
    /// mq
    /// </summary>
    public readonly HelperMq mq_helper = null!;

    /// <summary>
    /// 
    /// </summary>
    public WebSocketController()
    {
        try
        {
            this.mq_helper = new HelperMq(FactoryService.instance.constant.connection_factory.CreateConnection());
        }
        catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException ex)
        {
            this.mq_helper = new HelperMq(FactoryService.instance.constant.connection_factory);
            FactoryService.instance.constant.logger.LogInformation(ex, $"mq创建通道报错,使用mq连接重新创建通道");
        }
    }

    /// <summary>
    /// websocket处理
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("push")]
    public async Task<IActionResult> Push()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            try
            {
                WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                Dictionary<string, (string, string)> channel = new Dictionary<string, (string, string)>();
                await new TaskFactory().StartNew(async () =>
                {
                    while (true)
                    {
                        await Task.Delay(1000 * 60 * 5);
                        if (webSocket.State == WebSocketState.Closed)
                        {
                            // foreach (var item in channel)
                            // {
                            //     this.mq_helper.MqDeleteConsumer(item.Value);
                            // }
                            this.mq_helper.MqDeleteConsumer();
                            this.mq_helper.MqDeletePurge();
                            this.mq_helper.MqDelete();
                            this.mq_helper.Close();
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
                // foreach (var item in channel)
                // {
                //     this.mq_helper.MqDeleteConsumer(item.Value);
                // }
                this.mq_helper.MqDeleteConsumer();
                this.mq_helper.MqDeletePurge();
                this.mq_helper.MqDelete();
                this.mq_helper.Close();
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
    /// </summary>
    /// <param name="webSocket">websocket对象</param>
    /// <param name="str">接收到的字符串</param>
    /// <param name="channel">频道</param>
    /// <param name="uid">用户id</param>
    private void Subscribe(WebSocket webSocket, string str, Dictionary<string, (string queueName, string consume_tag)> channel, ref long uid)
    {
        if (str.ToLower() == "ping")
        {
            webSocket.SendAsync(new ArraySegment<byte>(pong, 0, pong.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            return;
        }
        ReqWebsocker? req = null;
        ResWebsocker<string> resWebsocker = new ResWebsocker<string>();
        try
        {
            req = JsonConvert.DeserializeObject<ReqWebsocker>(str);
        }
        catch (System.Exception ex)
        {
            resWebsocker.success = false;
            resWebsocker.msg = $"无法解析请求命令:{str},{ex.Message}";
            byte[] b = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(resWebsocker));
            webSocket.SendAsync(new ArraySegment<byte>(b, 0, b.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        if (req == null)
        {
            return;
        }
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
                resWebsocker.msg = "该订阅需要登录权限,请先登录!";
                byte[] b = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(resWebsocker));
                webSocket.SendAsync(new ArraySegment<byte>(b, 0, b.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            req.args.RemoveAll(P => Logout.Contains(P));
        }
        if (req.op == E_WebsockerOp.login)
        {
            //执行登录操作，并设置用户id
            if (req.args != null && req.args.Count > 0 && !string.IsNullOrWhiteSpace(req.args[0].data))
            {
                try
                {
                    Req_Login? req_login = JsonConvert.DeserializeObject<Req_Login>(req.args[0].data);
                    if (req_login != null)
                    {
                        UsersApi? users_api = service_user.GetApi(req_login.api_key);
                        if (users_api != null)
                        {
                            if (req_login.sign == Encryption.HmacSHA256Encrypt(users_api.api_secret, req_login.timestamp.ToString()))
                            {
                                uid = users_api.user_id;
                                resWebsocker.channel = E_WebsockerChannel.none;
                                resWebsocker.data = "";
                                resWebsocker.msg = "登录成功!";
                                byte[] b = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(resWebsocker));
                                webSocket.SendAsync(new ArraySegment<byte>(b, 0, b.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                                return;
                            }
                        }
                    }
                }
                catch
                {
                }
            }
            resWebsocker.success = false;
            resWebsocker.channel = E_WebsockerChannel.none;
            resWebsocker.msg = "签名失败. {api_key:'你的api用户key',timestamp:时间戳(毫秒),sign:'签名'},签名算法 HMACSHA256(secret).ComputeHash(timestamp)";
            byte[] bb = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(resWebsocker));
            webSocket.SendAsync(new ArraySegment<byte>(bb, 0, bb.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            return;
        }
        else if (req.op == E_WebsockerOp.logout)
        {
            uid = 0;
            resWebsocker.channel = E_WebsockerChannel.none;
            resWebsocker.data = "";
            resWebsocker.msg = "登出成功!";
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
                            key = FactoryService.instance.GetMqSubscribe(item.channel, uid);
                        }
                        if (channel.ContainsKey(key))
                        {
                            continue;
                        }
                        resWebsocker.channel = item.channel;
                        resWebsocker.data = item.data;
                        resWebsocker.msg = "订阅成功";
                        string Json = JsonConvert.SerializeObject(resWebsocker);
                        byte[] bb = Encoding.UTF8.GetBytes(Json);
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
                                depth_res.op = E_WebsockerOp.subscribe_event;
                                depth_res.channel = item.channel;
                                depth_res.data = depth;
                                byte[] bb1 = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(depth_res));
                                webSocket.SendAsync(new ArraySegment<byte>(bb1, 0, bb1.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                            }
                        }
                        (string queue_name, string ConsumerTags) subscribe = this.mq_helper.MqSubscribe(key, async (b) =>
                        {
                            await webSocket.SendAsync(new ArraySegment<byte>(b, 0, b.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                        });
                        channel.Add(key, (subscribe.queue_name, subscribe.ConsumerTags));
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
                            key = FactoryService.instance.GetMqSubscribe(item.channel, uid);
                        }
                        if (channel.ContainsKey(key))
                        {
                            this.mq_helper.MqDeleteConsumer(channel[key].consume_tag);
                            this.mq_helper.MqDeletePurge(channel[key].queueName);
                            channel.Remove(key);
                            resWebsocker.channel = item.channel;
                            resWebsocker.data = item.data;
                            resWebsocker.msg = "取消订阅成功";
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
