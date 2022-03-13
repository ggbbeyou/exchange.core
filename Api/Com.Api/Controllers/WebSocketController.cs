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

                var buffer = new byte[1024 * 1024];
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                byte[] a = buffer;
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
                        // byte[] b = System.Text.Encoding.UTF8.GetBytes("pong");

                        AA(webSocket, result, JsonConvert.DeserializeObject<ReqWebsocker<ReqChannel>>(str));
                        // await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
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


    private void AA(WebSocket webSocket, WebSocketReceiveResult result, ReqWebsocker<ReqChannel>? req)
    {
        if (req == null)
        {
            return;
        }
        if (req.op == "subscribe")
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
                        string ConsumerTags = "";
                        ConsumerTags = this.constant.MqSubscribe(item.channel, market.ToString(), async (message) =>
                        {
                            try
                            {
                                byte[] b = System.Text.Encoding.UTF8.GetBytes(message);
                                if (webSocket.State == WebSocketState.Open)
                                {
                                    await webSocket.SendAsync(new ArraySegment<byte>(b, 0, b.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                                }
                                else
                                {
                                    this.constant.i_model.BasicCancel(ConsumerTags);
                                }
                            }
                            catch (System.Exception ex)
                            {
                                this.constant.logger.LogError(ex, "websocket_coin发送消息出错:");
                            }
                        });
                    }
                }
            }
        }
        else if (req.op == "unsubscribe")
        {

        }

        // string queueName = this.constant.i_model.QueueDeclare().QueueName;
        // this.constant.i_model.QueueBind(queue: queueName, exchange: this.key_order_send, routingKey: this.model.info.market.ToString());
        // EventingBasicConsumer consumer = new EventingBasicConsumer(this.constant.i_model);
        // consumer.Received += (model, ea) =>
        // {
        //     if (!this.model.run)
        //     {
        //         this.constant.i_model.BasicNack(deliveryTag: ea.DeliveryTag, multiple: true, requeue: true);
        //     }
        //     else
        //     {
        //         string json = Encoding.UTF8.GetString(ea.Body.ToArray());
        //         CallRequest<List<Orders>>? req = JsonConvert.DeserializeObject<CallRequest<List<Orders>>>(json);
        //         if (req != null && req.op == E_Op.place && req.data != null && req.data.Count > 0)
        //         {
        //             deal.Clear();
        //             cancel_deal.Clear();
        //             foreach (Orders item in req.data)
        //             {
        //                 this.mutex.WaitOne();
        //                 (Orders? order, List<Deal> deal, List<Orders> cancel) match = this.model.match_core.Match(item);
        //                 this.mutex.ReleaseMutex();
        //                 if (match.order == null)
        //                 {
        //                     continue;
        //                 }
        //                 deal.Add((match.order, match.deal));
        //                 if (match.cancel.Count > 0)
        //                 {
        //                     cancel_deal.AddRange(match.cancel);
        //                 }
        //             }
        //             if (deal.Count() > 0)
        //             {
        //                 this.constant.i_model.BasicPublish(exchange: this.key_deal, routingKey: this.model.info.market.ToString(), basicProperties: props, body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(deal)));
        //             }
        //             if (cancel_deal.Count > 0)
        //             {
        //                 this.constant.i_model.BasicPublish(exchange: this.key_order_cancel_success, routingKey: this.model.info.market.ToString(), basicProperties: props, body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(cancel_deal)));
        //             }
        //         };
        //         this.constant.i_model.BasicAck(ea.DeliveryTag, true);
        //     }
        // };
        // this.constant.i_model.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);



        // await webSocket.SendAsync(new ArraySegment<byte>(b, 0, result.Count), WebSocketMessageType.Text, true, CancellationToken.None);
    }



}
