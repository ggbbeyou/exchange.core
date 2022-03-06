using System;
using System.Collections.Generic;
using System.Text;
using Com.Model;
using Com.Model.Enum;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Snowflake;

namespace Com.Matching;

/// <summary>
/// RabbitMQ 接收数据和发送数据
/// </summary>
public class MQ
{
    /// <summary>
    /// 撮合核心
    /// </summary>
    private Core core;
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
    /// 初始化
    /// </summary>
    /// <param name="core">撮合核心</param>
    public MQ(Core core)
    {
        this.core = core;
        props.DeliveryMode = 2;
        FactoryMatching.instance.constant.i_model.ExchangeDeclare(exchange: this.key_deal, type: ExchangeType.Direct, durable: true, autoDelete: false, arguments: null);
        FactoryMatching.instance.constant.i_model.ExchangeDeclare(exchange: this.key_order_cancel, type: ExchangeType.Direct, durable: true, autoDelete: false, arguments: null);
        FactoryMatching.instance.constant.i_model.ExchangeDeclare(exchange: this.key_order_send, type: ExchangeType.Direct, durable: true, autoDelete: false, arguments: null);
        FactoryMatching.instance.constant.i_model.ExchangeDeclare(exchange: this.key_order_cancel_success, type: ExchangeType.Direct, durable: true, autoDelete: false, arguments: null);
        OrderReceive();
        OrderCancel();
    }

    /// <summary>
    /// 接收订单列队
    /// </summary>
    public void OrderReceive()
    {
        string queueName = FactoryMatching.instance.constant.i_model.QueueDeclare().QueueName;
        FactoryMatching.instance.constant.i_model.QueueBind(queue: queueName, exchange: this.key_order_send, routingKey: this.core.market);
        EventingBasicConsumer consumer = new EventingBasicConsumer(FactoryMatching.instance.constant.i_model);
        consumer.Received += (model, ea) =>
        {
            if (!this.core.run)
            {
                FactoryMatching.instance.constant.i_model.BasicNack(deliveryTag: ea.DeliveryTag, multiple: true, requeue: true);
            }
            else
            {
                string json = Encoding.UTF8.GetString(ea.Body.ToArray());
                Req<List<MatchOrder>>? req = JsonConvert.DeserializeObject<Req<List<MatchOrder>>>(json);
                if (req != null && req.op == E_Op.place && req.data != null && req.data.Count > 0)
                {
                    foreach (MatchOrder item in req.data)
                    {
                        this.mutex.WaitOne();
                        (List<MatchDeal> deal, List<MatchOrder> cancel) deals = this.core.Match(item);
                        if (deals.deal != null && deals.deal.Count > 0)
                        {
                            FactoryMatching.instance.constant.i_model.BasicPublish(exchange: this.key_deal, routingKey: this.core.market, basicProperties: props, body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(deals.deal)));
                        }
                        if (deals.cancel != null && deals.cancel.Count > 0)
                        {
                            FactoryMatching.instance.constant.i_model.BasicPublish(exchange: this.key_order_cancel_success, routingKey: this.core.market, basicProperties: props, body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(deals.cancel)));
                        }
                        this.mutex.ReleaseMutex();
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
        FactoryMatching.instance.constant.i_model.QueueBind(queue: queueName, exchange: this.key_order_cancel, routingKey: this.core.market);
        EventingBasicConsumer consumer = new EventingBasicConsumer(FactoryMatching.instance.constant.i_model);
        consumer.Received += (model, ea) =>
        {
            if (!this.core.run)
            {
                FactoryMatching.instance.constant.i_model.BasicNack(deliveryTag: ea.DeliveryTag, multiple: true, requeue: true);
            }
            else
            {
                string json = Encoding.UTF8.GetString(ea.Body.ToArray());
                Req<List<long>>? req = JsonConvert.DeserializeObject<Req<List<long>>>(json);
                if (req != null && req.op == E_Op.place && req.data != null)
                {
                    this.mutex.WaitOne();
                    List<MatchOrder> cancel = new List<MatchOrder>();
                    if (req.op == E_Op.cancel_by_id)
                    {
                        cancel.AddRange(this.core.CancelOrder(req.data));
                    }
                    else if (req.op == E_Op.cancel_by_uid)
                    {
                        cancel.AddRange(this.core.CancelOrder(req.data.First()));
                    }
                    else if (req.op == E_Op.cancel_by_clientid)
                    {
                        cancel.AddRange(this.core.CancelOrder(req.data.ToArray()));
                    }
                    else if (req.op == E_Op.cancel_by_all)
                    {
                        cancel.AddRange(this.core.CancelOrder());
                    }
                    if (cancel.Count > 0)
                    {
                        FactoryMatching.instance.constant.i_model.BasicPublish(exchange: this.key_order_cancel_success, routingKey: this.core.market, basicProperties: props, body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(cancel)));
                    }
                    this.mutex.ReleaseMutex();
                    FactoryMatching.instance.constant.i_model.BasicAck(ea.DeliveryTag, false);

                }
                List<long>? order = JsonConvert.DeserializeObject<List<long>>(Encoding.UTF8.GetString(ea.Body.ToArray()));
                if (order != null)
                {
                    this.mutex.WaitOne();
                    List<MatchOrder> cancel = this.core.CancelOrder(order);
                    if (cancel != null && cancel.Count > 0)
                    {
                        FactoryMatching.instance.constant.i_model.BasicPublish(exchange: this.key_order_cancel_success, routingKey: this.core.market, basicProperties: props, body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(cancel)));
                    }
                    this.mutex.ReleaseMutex();
                    FactoryMatching.instance.constant.i_model.BasicAck(ea.DeliveryTag, false);
                }
            }
        };
        FactoryMatching.instance.constant.i_model.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
    }

}