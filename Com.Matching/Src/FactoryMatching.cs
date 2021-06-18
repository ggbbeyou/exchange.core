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
            ServiceStatus();
        }

        /// <summary>
        /// 撮合引擎状态监测 
        /// open:servername:name:price
        /// close:servername:name
        /// </summary>
        public void ServiceStatus()
        {
            string queue_name = $"MatchingService";
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();
            channel.QueueDeclare(queue: queue_name, durable: true, exclusive: false, autoDelete: false, arguments: null);
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                if (!string.IsNullOrWhiteSpace(message))
                {
                    string[] status = message.Split(':', StringSplitOptions.RemoveEmptyEntries);
                    if (this.server_name == status[1])
                    {
                        string name = status[2].ToLower();
                        switch (status[0])
                        {
                            case "open":
                                decimal price = decimal.Parse(status[3]);
                                if (!this.cores.ContainsKey(name))
                                {
                                    Core core = new Core(name, this.configuration, this.logger);
                                    core.Start(price);
                                    this.cores.Add(name, core);
                                }
                                else
                                {
                                    Core core = this.cores[name];
                                    core.Start(price);
                                }
                                break;
                            case "close":
                                if (this.cores.ContainsKey(name))
                                {
                                    Core core = this.cores[name];
                                    core.Stop();
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            };
            channel.BasicConsume(queue: queue_name, autoAck: false, consumer: consumer);
        }

    }
}