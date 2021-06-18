using System;
using System.Collections.Generic;
using System.Text;
using Com.Model.Base;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Snowflake;

namespace Com.Matching
{
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
        /// 连接工厂
        /// </summary>
        private IConnection connection;
        /// <summary>
        /// (Base)发送订单队列名称
        /// </summary>
        /// <value></value>
        public string key_order_send = "order_send.{0}";
        /// <summary>
        /// (Base)取消订单队列名称
        /// </summary>
        /// <value></value>
        public string key_order_cancel = "order_cancel.{0}";
        /// <summary>
        /// (Topics)发送历史成交记录,交易机名称
        /// </summary>
        /// <value></value>
        public string key_exchange_deal = "deal.{0}";
        /// <summary>
        /// 发送历史成交
        /// </summary>
        public IModel channel_deal = null;
        /// <summary>
        /// (Topics)发送orderbook记录,交易机名称
        /// </summary>
        /// <value></value>
        public string key_exchange_orderbook = "orderbook.{0}";
        /// <summary>
        /// 发送orderbook
        /// </summary>
        public IModel channel_orderbook = null;
        /// <summary>
        /// (Topics)发送K线记录,交易机名称
        /// </summary>
        /// <value></value>
        public string key_exchange_kline = "kline.{0}";
        /// <summary>
        /// 发送K线记录
        /// </summary>
        public IModel channel_kline = null;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="core">撮合核心</param>
        public MQ(Core core)
        {
            this.core = core;
            this.key_order_send = string.Format(this.key_order_send, core.name);
            this.key_order_cancel = string.Format(this.key_order_cancel, core.name);
            this.key_exchange_deal = string.Format(this.key_exchange_deal, core.name);
            this.key_exchange_orderbook = string.Format(this.key_exchange_orderbook, core.name);
            this.key_exchange_kline = string.Format(this.key_exchange_kline, core.name);
            ConnectionFactory factory = this.core.configuration.GetSection("RabbitMQ").Get<ConnectionFactory>();
            this.connection = factory.CreateConnection();
            this.channel_deal = this.connection.CreateModel();
            this.channel_orderbook = this.connection.CreateModel();
            this.channel_kline = this.connection.CreateModel();
            OrderReceive();
            OrderCancel();
        }

        /// <summary>
        /// 接收订单列队
        /// </summary>
        public void OrderReceive()
        {
            IModel channel = connection.CreateModel();
            channel.QueueDeclare(queue: this.key_order_send, durable: true, exclusive: false, autoDelete: false, arguments: null);
            EventingBasicConsumer consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                if (this.core.run)
                {
                    string json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    Order order = JsonConvert.DeserializeObject<Order>(json);
                    if (order != null)
                    {
                        this.core.Process(order);
                        channel.BasicAck(ea.DeliveryTag, false);
                    }
                }
            };
            channel.BasicConsume(queue: this.key_order_send, autoAck: false, consumer: consumer);
        }


        /// <summary>
        /// 取消订单列队
        /// </summary>
        public void OrderCancel()
        {
            IModel channel = connection.CreateModel();
            channel.QueueDeclare(queue: this.key_order_cancel, durable: true, exclusive: false, autoDelete: false, arguments: null);
            EventingBasicConsumer consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                string json = Encoding.UTF8.GetString(ea.Body.ToArray());
                Order order = JsonConvert.DeserializeObject<Order>(json);
                if (order != null)
                {
                    this.core.Process(order);
                    channel.BasicAck(ea.DeliveryTag, false);
                }
            };
            channel.BasicConsume(queue: this.key_order_cancel, autoAck: false, consumer: consumer);
        }

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
            string json = JsonConvert.SerializeObject(deals);
            byte[] body = Encoding.UTF8.GetBytes(json);
            this.channel_deal.ExchangeDeclare(exchange: this.key_exchange_deal, type: ExchangeType.Topic);
            this.channel_deal.BasicPublish(exchange: this.key_exchange_deal, routingKey: this.core.name, basicProperties: null, body: body);
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
            string json = JsonConvert.SerializeObject(orderBooks);
            byte[] body = Encoding.UTF8.GetBytes(json);
            this.channel_deal.ExchangeDeclare(exchange: this.key_exchange_orderbook, type: ExchangeType.Topic);
            this.channel_deal.BasicPublish(exchange: this.key_exchange_orderbook, routingKey: this.core.name, basicProperties: null, body: body);
        }

        /// <summary>
        /// 发送K线
        /// </summary>
        /// <param name="kline">K线</param>
        public void SendKline(Kline kline)
        {
            if (kline == null)
            {
                return;
            }
            string json = JsonConvert.SerializeObject(kline);
            byte[] body = Encoding.UTF8.GetBytes(json);
            this.channel_deal.ExchangeDeclare(exchange: this.key_exchange_kline, type: ExchangeType.Topic);
            this.channel_deal.BasicPublish(exchange: this.key_exchange_kline, routingKey: this.core.name, basicProperties: null, body: body);
        }

    }
}