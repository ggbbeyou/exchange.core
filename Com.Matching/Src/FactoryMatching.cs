using System;
using System.Collections.Generic;
using System.Text;
using Com.Model.Base;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Snowflake;

namespace Com.Matching
{
    /// <summary>
    /// 工厂
    /// </summary>
    public class FactoryMatching
    {
        /// <summary>
        /// 单例类的实例
        /// </summary>
        /// <returns></returns>
        public static readonly FactoryMatching instance = new FactoryMatching();
        /// <summary>
        /// 配置接口
        /// </summary>
        public IConfiguration configuration;
        /// <summary>
        /// 日志接口
        /// </summary>
        public ILogger logger;
        /// <summary>
        /// 服务器名称
        /// </summary>
        public string server_name;
        /// <summary>
        /// MQ工厂
        /// </summary>
        private ConnectionFactory factory;
        /// <summary>
        /// 撮合集合
        /// </summary>
        /// <typeparam name="string">名称</typeparam>
        /// <typeparam name="MQ">撮合器</typeparam>
        /// <returns></returns>
       // public Dictionary<string, MQ> cores = new Dictionary<string, MQ>();

        /// <summary>
        /// 撮合集合
        /// </summary>
        /// <typeparam name="string">交易对</typeparam>
        /// <typeparam name="Core">撮合器</typeparam>
        /// <returns></returns>
        public Dictionary<string, Core> cores = new Dictionary<string, Core>();

        /// <summary>
        /// 私有构造方法
        /// </summary>
        private FactoryMatching()
        {

        }

        /// <summary>
        /// 初始化方法
        /// </summary>
        /// <param name="configuration">配置接口</param>
        /// <param name="logger">日志接口</param>
        public void Info(IConfiguration configuration, ILogger logger)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.server_name = configuration.GetValue<string>("server_name");
            this.factory = configuration.GetSection("RabbitMQ").Get<ConnectionFactory>();
            // OrderReceive();
            // OrderCancel();
            Status();
        }

        /// <summary>
        /// 撮合引擎状态监测 
        /// open:servername:name:price
        /// close:servername:name
        /// </summary>
        public void Status()
        {
            string queue_name = $"MatchingService";
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: queue_name, durable: true, exclusive: false, autoDelete: false, arguments: null);
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        string[] status = message.Split(':', StringSplitOptions.RemoveEmptyEntries);
                        switch (status[0])
                        {
                            case "open":
                                if (this.server_name == status[1])
                                {
                                    if (!this.cores.ContainsKey(status[2].ToLower()))
                                    {
                                        Core core = new Core(status[2].ToLower(), this.configuration, this.logger);
                                        core.Start(decimal.Parse(status[3]));
                                        this.cores.Add(status[2].ToLower(), core);
                                    }
                                    else
                                    {
                                        Core core = this.cores[status[2].ToLower()];
                                        core.Start(decimal.Parse(status[3]));
                                    }
                                }
                                break;
                            case "close":
                                if (this.cores.ContainsKey(status[2].ToLower()))
                                {
                                    Core core = this.cores[status[2].ToLower()];
                                    core.Stop();
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                };
                channel.BasicConsume(queue: queue_name, autoAck: false, consumer: consumer);
            }
        }

        /// <summary>
        /// 接收订单列队
        /// </summary>
        public void OrderReceive()
        {
            string queue_name = $"{this.server_name}.OrderReceive";
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: queue_name, durable: true, exclusive: false, autoDelete: false, arguments: null);
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                    Order order = JsonConvert.DeserializeObject<Order>(message);
                    if (order != null)
                    {
                        if (this.cores.ContainsKey(order.name))
                        {
                            this.cores[order.name].Process(order);
                        }
                    }
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                };
                channel.BasicConsume(queue: queue_name, autoAck: false, consumer: consumer);
            }
        }

        /// <summary>
        /// 取消订单列队
        /// </summary>
        public void OrderCancel()
        {
            string queue_name = $"{this.server_name}.OrderCancel";
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: queue_name, durable: true, exclusive: false, autoDelete: false, arguments: null);
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var message = Encoding.UTF8.GetString(ea.Body.ToArray());

                    // if (this.core.ContainsKey(order.name))
                    // {
                    //     this.core[order.name].CancelOrder(message);
                    // }                    
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                };
                channel.BasicConsume(queue: queue_name, autoAck: true, consumer: consumer);
            }
        }






    }
}