using System;
using Com.Model.Base;
using Snowflake;

namespace Com.Matching
{
    /// <summary>
    /// RabbitMQ 接收数据和发送数据
    /// </summary>
    public class MQ
    {
        private Core core;

        public MQ(Core core)
        {
            this.core = core;


            // ConnectionFactory factory = new ConnectionFactory() { HostName = "192.168.1.3", Port = 5672, UserName = "guest", Password = "guest" };
            // IConnection connection = factory.CreateConnection();
            // this.channel = connection.CreateModel();

            // channel.ExchangeDeclare(exchange: "PendingOrder", type: "topic");
            // string queueName = channel.QueueDeclare().QueueName;
            // channel.QueueBind(queue: queueName, exchange: "PendingOrder", routingKey: this.name);
            // EventingBasicConsumer consumer = new EventingBasicConsumer(channel);

            // consumer.Received += (model, ea) =>
            //                     {
            //                         var body = ea.Body.ToArray();
            //                         var message = Encoding.UTF8.GetString(body);
            //                         var routingKey = ea.RoutingKey;
            //                         Console.WriteLine(" [x] Received '{0}':'{1}'", routingKey, message);
            //                     };
            // channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
            // // channel.Close();
            // // connection.Close();


            // //ShutdownEventArgs args = new ShutdownEventArgs();
            // consumer.HandleModelShutdown(this.channel, null);
            // //Process();
            
        }

    }

}