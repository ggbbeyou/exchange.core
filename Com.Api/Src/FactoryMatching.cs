using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Com.Model.Base;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Snowflake;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Com.Api
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
        /// MQ工厂
        /// </summary>
        private ConnectionFactory factory;

        private const string QUEUE_NAME = "rpc_queue";

        private IConnection connection;
        private IModel channel;
        private string replyQueueName;
        private EventingBasicConsumer consumer;
        private ConcurrentDictionary<string, TaskCompletionSource<string>> callbackMapper = new ConcurrentDictionary<string, TaskCompletionSource<string>>();

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
        public void Init(IConfiguration configuration, IRedisCacheClient redisCacheClient, IHostEnvironment environment, ILogger logger = null)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.factory = configuration.GetSection("RabbitMQ").Get<ConnectionFactory>();
            this.factory.AutomaticRecoveryEnabled = true;
            connection = factory.CreateConnection();
            channel = connection.CreateModel();
            replyQueueName = channel.QueueDeclare().QueueName;
            consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                if (!callbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out TaskCompletionSource<string> tcs))
                {
                    return;
                }
                var body = ea.Body.ToArray();
                var response = Encoding.UTF8.GetString(body);
                tcs.TrySetResult(response);
            };

            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    string replyQueueName = channel.QueueDeclare().QueueName;
                    channel.QueueDeclare(queue: "rpc_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);
                    var consumer = new EventingBasicConsumer(channel);
                    var props = channel.CreateBasicProperties();
                    var correlationId = Guid.NewGuid().ToString();
                    props.ReplyTo = replyQueueName;
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
                            response = "";//调用方法返回值
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(" [.] " + e.Message);
                            response = "";
                        }
                        finally
                        {
                            var responseBytes = Encoding.UTF8.GetBytes(response);
                            channel.BasicPublish(exchange: "", routingKey: props.ReplyTo, basicProperties: replyProps, body: responseBytes);
                            channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        }
                    };
                }
            }
        }

        /// <summary>
        /// 启动撮合引擎
        /// </summary>
        public async Task Start()
        {
            var response = await CallAsync("a");
            //Close();
        }

        public Task<string> CallAsync(string message, CancellationToken cancellationToken = default(CancellationToken))
        {
            IBasicProperties props = channel.CreateBasicProperties();
            var correlationId = Guid.NewGuid().ToString();
            props.CorrelationId = correlationId;
            props.ReplyTo = replyQueueName;
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var tcs = new TaskCompletionSource<string>();
            callbackMapper.TryAdd(correlationId, tcs);

            channel.BasicPublish(exchange: "",routingKey: QUEUE_NAME,basicProperties: props,body: messageBytes);

            channel.BasicConsume(consumer: consumer,queue: replyQueueName,autoAck: true);

            cancellationToken.Register(() => callbackMapper.TryRemove(correlationId, out var tmp));
            return tcs.Task;
        }

        public void Close()
        {
            connection.Close();
        }



    }
}