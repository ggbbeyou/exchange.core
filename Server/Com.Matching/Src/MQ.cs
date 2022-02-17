using System;
using System.Collections.Generic;
using System.Text;
using Com.Model;
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
    /// (Direct)发送订单队列名称
    /// </summary>
    /// <value></value>
    public string key_order_send = "order_send";
    /// <summary>
    /// (Direct)取消订单队列名称
    /// </summary>
    /// <value></value>
    public string key_order_cancel = "order_cancel";
    /// <summary>
    /// (Direct)取消订单成功队列名称
    /// </summary>
    /// <value></value>
    public string key_order_cancel_success = "order_cancel_success";
    /// <summary>
    /// (Direct)发送历史成交记录
    /// </summary>
    /// <value></value>
    public string key_exchange_deal = "deal";
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
        // FactoryMatching.instance.constant.i_model.ExchangeDeclare(exchange: this.key_exchange_deal, type: ExchangeType.Direct, durable: true, autoDelete: false, arguments: null);
        // FactoryMatching.instance.constant.i_model.ExchangeDeclare(exchange: this.key_exchange_orderbook, type: ExchangeType.Direct, durable: true, autoDelete: false, arguments: null);
        // FactoryMatching.instance.constant.i_model.ExchangeDeclare(exchange: this.key_exchange_kline, type: ExchangeType.Direct, durable: true, autoDelete: false, arguments: null);
        OrderReceive();
        OrderCancel();
    }

    /// <summary>
    /// 接收订单列队
    /// </summary>
    public void OrderReceive()
    {
        FactoryMatching.instance.constant.i_model.ExchangeDeclare(exchange: this.key_order_send, type: ExchangeType.Direct, durable: true, autoDelete: false, arguments: null);
        string queueName = FactoryMatching.instance.constant.i_model.QueueDeclare().QueueName;
        FactoryMatching.instance.constant.i_model.QueueBind(queue: queueName, exchange: this.key_order_send, routingKey: this.core.name);
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
                List<Order>? order = JsonConvert.DeserializeObject<List<Order>>(json);
                if (order != null)
                {
                    foreach (var item in order)
                    {
                        this.mutex.WaitOne();
                        List<Deal> deals = this.core.Match(item);
                        if (deals != null && deals.Count > 0)
                        {
                            FactoryMatching.instance.constant.i_model.BasicPublish(exchange: this.key_exchange_deal, routingKey: this.core.name, basicProperties: props, body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(deals)));
                        }
                        this.mutex.ReleaseMutex();
                    }
                    FactoryMatching.instance.constant.i_model.BasicAck(ea.DeliveryTag, true);
                }
            }
        };
        FactoryMatching.instance.constant.i_model.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
    }

    /// <summary>
    /// 取消订单列队
    /// </summary>
    public void OrderCancel()
    {
        FactoryMatching.instance.constant.i_model.ExchangeDeclare(exchange: this.key_order_cancel_success, type: ExchangeType.Direct, durable: true, autoDelete: false, arguments: null);
        FactoryMatching.instance.constant.i_model.ExchangeDeclare(exchange: this.key_order_cancel, type: ExchangeType.Direct, durable: true, autoDelete: false, arguments: null);
        string queueName = FactoryMatching.instance.constant.i_model.QueueDeclare().QueueName;
        FactoryMatching.instance.constant.i_model.QueueBind(queue: queueName, exchange: this.key_order_cancel, routingKey: this.core.name);
        EventingBasicConsumer consumer = new EventingBasicConsumer(FactoryMatching.instance.constant.i_model);
        consumer.Received += (model, ea) =>
        {
            if (!this.core.run)
            {
                FactoryMatching.instance.constant.i_model.BasicNack(deliveryTag: ea.DeliveryTag, multiple: true, requeue: true);
            }
            else
            {
                List<string>? order = JsonConvert.DeserializeObject<List<string>>(Encoding.UTF8.GetString(ea.Body.ToArray()));
                if (order != null)
                {
                    this.mutex.WaitOne();
                    List<Order> cancel = this.core.CancelOrder(order);
                    if (cancel != null && cancel.Count > 0)
                    {
                        FactoryMatching.instance.constant.i_model.BasicPublish(exchange: this.key_order_cancel_success, routingKey: this.core.name, basicProperties: props, body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(cancel)));
                    }
                    this.mutex.ReleaseMutex();
                    FactoryMatching.instance.constant.i_model.BasicAck(ea.DeliveryTag, false);
                }
            }
        };
        FactoryMatching.instance.constant.i_model.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
    }

    /*

    /// <summary>
    /// 发送历史成交记录
    /// </summary>
    /// <param name="deals">成交记录</param>
    public void SendDeal(List<Deal> deals)
    {
        if (deals == null || deals.Count == 0)
        {
            return;
        }
        FactoryMatching.instance.constant.i_model.BasicPublish(exchange: this.key_exchange_deal, routingKey: this.core.name, basicProperties: props, body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(deals)));
    }

    /// <summary>
    /// 发送OrderBook
    /// </summary>
    /// <param name="orderBooks">OrderBook</param>
    public void SendOrderBook(List<OrderBook> orderBooks)
    {
        if (orderBooks == null || orderBooks.Count == 0)
        {
            return;
        }
        FactoryMatching.instance.constant.i_model.BasicPublish(exchange: this.key_exchange_orderbook, routingKey: this.core.name, basicProperties: props, body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(orderBooks)));
    }

    /// <summary>
    /// 发送K线
    /// </summary>
    /// <param name="kline">K线</param>
    public void SendKline(Kline? kline)
    {
        if (kline == null)
        {
            return;
        }
        FactoryMatching.instance.constant.i_model.BasicPublish(exchange: this.key_exchange_kline, routingKey: this.core.name, basicProperties: props, body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(kline)));
    }
    */

}