using System;
using System.Collections.Generic;
using System.Text;
using Com.Model.Base;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        public Dictionary<string, MQ> cores = new Dictionary<string, MQ>();

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
        }

        /// <summary>
        /// 启动撮合引擎
        /// </summary>
        public void Start()
        {
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: "rpc_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);
                    channel.BasicQos(0, 1, false);
                    var consumer = new EventingBasicConsumer(channel);
                    channel.BasicConsume(queue: "rpc_queue", autoAck: false, consumer: consumer);                 
                    consumer.Received += (model, ea) =>
                    {
                        string response = null;
                        var body = ea.Body.ToArray();
                        var props = ea.BasicProperties;
                        var replyProps = channel.CreateBasicProperties();
                        replyProps.CorrelationId = props.CorrelationId;
                        try
                        {
                            var message = Encoding.UTF8.GetString(body); //请求参数                      
                            response ="bbbbbbbbbb";//调用方法返回值
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(" [.] " + e.Message);
                            response = "";
                        }
                        finally
                        {
                            var responseBytes = Encoding.UTF8.GetBytes(response);
                            channel.BasicPublish(exchange: "", routingKey: props.ReplyTo,basicProperties: replyProps, body: responseBytes);
                            channel.BasicAck(deliveryTag: ea.DeliveryTag,multiple: false);
                        }
                    };
                }
            }
        }









    }
}