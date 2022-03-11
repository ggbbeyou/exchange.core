using System;
using System.Collections.Generic;
using System.Text;
using Com.Db;
using Com.Db.Enum;
using Com.Db.Model;
using Com.Service.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Snowflake;

namespace Com.Service.Match;

/// <summary>
/// RabbitMQ 接收数据和发送数据
/// </summary>
public class MQ
{
    /// <summary>
    /// 撮合服务对象
    /// </summary>
    private MatchModel model;
    /// <summary>
    /// (Direct)接收挂单订单队列名称
    /// </summary>
    /// <value></value>
    public string key_order_send = "order_send";
    /// <summary>
    /// (Direct)发送历史成交记录
    /// </summary>
    /// <value></value>
    public string key_deal = "deal";
    /// <summary>
    /// (Direct)接收取消订单队列名称
    /// </summary>
    /// <value></value>
    public string key_order_cancel = "order_cancel";
    /// <summary>
    /// (Direct)发送撤单订单成功队列名称
    /// </summary>
    /// <value></value>
    public string key_order_cancel_success = "order_cancel_success";
    /// <summary>
    /// (Topics)发送orderbook记录,交易机名称
    /// </summary>
    /// <value></value>
    // public string key_exchange_orderbook = "orderbook";
    /// <summary>
    /// (Topics)发送K线记录,交易机名称
    /// </summary>
    /// <value></value>
    // public string key_exchange_kline = "kline";
    /// <summary>
    /// MQ基本属性
    /// </summary>
    /// <returns></returns>
    private IBasicProperties props = FactoryMatching.instance.constant.i_model.CreateBasicProperties();
    /// <summary>
    /// 互斥锁
    /// </summary>
    /// <returns></returns>
    private Mutex mutex = new Mutex(false);
    /// <summary>
    /// 临时变量
    /// </summary>
    /// <typeparam name="MatchDeal"></typeparam>
    /// <returns></returns>
    private List<(Orders order, List<Deal> deal)> deal = new List<(Orders order, List<Deal> deal)>();
    /// <summary>
    /// 临时变量
    /// </summary>
    /// <typeparam name="MatchOrder"></typeparam>
    /// <returns></returns>
    private List<Orders> cancel_deal = new List<Orders>();
    /// <summary>
    /// 临时变量
    /// </summary>
    /// <typeparam name="MatchOrder"></typeparam>
    /// <returns></returns>
    private List<Orders> cancel = new List<Orders>();

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="model">撮合核心</param>
    public MQ(MatchModel model)
    {
        this.model = model;
        props.DeliveryMode = 2;
        FactoryMatching.instance.constant.i_model.ExchangeDeclare(exchange: this.key_deal, type: ExchangeType.Direct, durable: true, autoDelete: false, arguments: null);
        FactoryMatching.instance.constant.i_model.ExchangeDeclare(exchange: this.key_order_cancel, type: ExchangeType.Direct, durable: true, autoDelete: false, arguments: null);
        FactoryMatching.instance.constant.i_model.ExchangeDeclare(exchange: this.key_order_send, type: ExchangeType.Direct, durable: true, autoDelete: false, arguments: null);
        FactoryMatching.instance.constant.i_model.ExchangeDeclare(exchange: this.key_order_cancel_success, type: ExchangeType.Direct, durable: true, autoDelete: false, arguments: null);
        OrderCancel();
        OrderReceive();
    }

    /// <summary>
    /// 接收订单列队
    /// </summary>
    public void OrderReceive()
    {
        string queueName = FactoryMatching.instance.constant.i_model.QueueDeclare().QueueName;
        FactoryMatching.instance.constant.i_model.QueueBind(queue: queueName, exchange: this.key_order_send, routingKey: this.model.info.market.ToString());
        EventingBasicConsumer consumer = new EventingBasicConsumer(FactoryMatching.instance.constant.i_model);
        consumer.Received += (model, ea) =>
        {
            if (!this.model.run)
            {
                FactoryMatching.instance.constant.i_model.BasicNack(deliveryTag: ea.DeliveryTag, multiple: true, requeue: true);
            }
            else
            {
                string json = Encoding.UTF8.GetString(ea.Body.ToArray());
                CallRequest<List<Orders>>? req = JsonConvert.DeserializeObject<CallRequest<List<Orders>>>(json);
                if (req != null && req.op == E_Op.place && req.data != null && req.data.Count > 0)
                {
                    deal.Clear();
                    cancel_deal.Clear();
                    foreach (Orders item in req.data)
                    {
                        this.mutex.WaitOne();
                        (Orders? order, List<Deal> deal, List<Orders> cancel) match = this.model.match_core.Match(item);
                        this.mutex.ReleaseMutex();
                        if (match.order == null)
                        {
                            continue;
                        }
                        deal.Add((match.order, match.deal));
                        if (match.cancel.Count > 0)
                        {
                            cancel_deal.AddRange(match.cancel);
                        }
                    }
                    if (deal.Count() > 0)
                    {
                        FactoryMatching.instance.constant.i_model.BasicPublish(exchange: this.key_deal, routingKey: this.model.info.market.ToString(), basicProperties: props, body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(deal)));
                    }
                    if (cancel_deal.Count > 0)
                    {
                        FactoryMatching.instance.constant.i_model.BasicPublish(exchange: this.key_order_cancel_success, routingKey: this.model.info.market.ToString(), basicProperties: props, body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(cancel_deal)));
                    }
                };
                FactoryMatching.instance.constant.i_model.BasicAck(ea.DeliveryTag, true);
            }
        };
        FactoryMatching.instance.constant.i_model.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
    }

    /// <summary>
    /// 取消订单列队
    /// </summary>
    public void OrderCancel()
    {
        string queueName = FactoryMatching.instance.constant.i_model.QueueDeclare().QueueName;
        FactoryMatching.instance.constant.i_model.QueueBind(queue: queueName, exchange: this.key_order_cancel, routingKey: this.model.info.market.ToString());
        EventingBasicConsumer consumer = new EventingBasicConsumer(FactoryMatching.instance.constant.i_model);
        consumer.Received += (model, ea) =>
        {
            if (!this.model.run)
            {
                FactoryMatching.instance.constant.i_model.BasicNack(deliveryTag: ea.DeliveryTag, multiple: true, requeue: true);
            }
            else
            {
                string json = Encoding.UTF8.GetString(ea.Body.ToArray());
                CallRequest<List<long>>? req = JsonConvert.DeserializeObject<CallRequest<List<long>>>(json);
                if (req != null && req.op == E_Op.place && req.data != null)
                {
                    cancel.Clear();
                    this.mutex.WaitOne();
                    if (req.op == E_Op.cancel_by_id)
                    {
                        cancel.AddRange(this.model.match_core.CancelOrder(req.data));
                    }
                    else if (req.op == E_Op.cancel_by_uid)
                    {
                        cancel.AddRange(this.model.match_core.CancelOrder(req.data.First()));
                    }
                    else if (req.op == E_Op.cancel_by_clientid)
                    {
                        cancel.AddRange(this.model.match_core.CancelOrder(req.data.ToArray()));
                    }
                    else if (req.op == E_Op.cancel_by_all)
                    {
                        cancel.AddRange(this.model.match_core.CancelOrder());
                    }
                    this.mutex.ReleaseMutex();
                    if (cancel.Count > 0)
                    {
                        FactoryMatching.instance.constant.i_model.BasicPublish(exchange: this.key_order_cancel_success, routingKey: this.model.info.market.ToString(), basicProperties: props, body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(cancel)));
                    }
                }
                FactoryMatching.instance.constant.i_model.BasicAck(ea.DeliveryTag, false);
            }
        };
        FactoryMatching.instance.constant.i_model.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
    }

}