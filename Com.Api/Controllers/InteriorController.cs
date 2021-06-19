using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Com.Api.Models;
using RabbitMQ.Client;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace Com.Api.Controllers
{
    /// <summary>
    /// 内部接口(系统内部)
    /// </summary>
    public class InteriorController : Controller
    {
        private readonly ILogger<InteriorController> logger;

        /// <summary>
        /// 配置接口
        /// </summary>
        public readonly IConfiguration configuration;
        public readonly IConnection connection;

        public InteriorController(IConfiguration configuration, ILogger<InteriorController> logger)
        {
            this.configuration = configuration;
            this.logger = logger;
            ConnectionFactory factory = this.configuration.GetSection("RabbitMQ").Get<ConnectionFactory>();
            this.connection = factory.CreateConnection();
        }

        public IActionResult Index()
        {

            return View();
        }

        /// <summary>
        /// 启动撮合服务
        /// </summary>
        /// <param name="service_name">撮合服务器名称</param>
        /// <param name="name">交易对</param>
        /// <param name="price">最后成交价</param>
        /// <returns></returns>
        public IActionResult MatchingStart(string service_name, string name, decimal price)
        {
            string queue_name = $"MatchingService";
            string comman = $"open:{service_name}:{name}:{price}";
            byte[] body = Encoding.UTF8.GetBytes(comman);
            try
            {
                IModel channel = this.connection.CreateModel();
                channel.QueueDeclare(queue: queue_name, durable: true, exclusive: false, autoDelete: false, arguments: null);
                IBasicProperties basicProperties = channel.CreateBasicProperties();
                basicProperties.Persistent = true;
                channel.BasicPublish(exchange: "", routingKey: queue_name, basicProperties: basicProperties, body: body);
                channel.Close();
            }
            catch (System.Exception)
            {
                return Json(false);
            }
            return Json(true);
        }

        /// <summary>
        /// 关闭撮合服务
        /// </summary>
        /// <param name="service_name">撮合服务器名称</param>
        /// <param name="name">交易对</param>
        /// <returns></returns>
        public IActionResult MatchingStop(string service_name, string name)
        {
            string queue_name = $"MatchingService";
            string comman = $"close:{service_name}:{name}";
            byte[] body = Encoding.UTF8.GetBytes(comman);
            try
            {
                IModel channel = this.connection.CreateModel();
                channel.QueueDeclare(queue: queue_name, durable: true, exclusive: false, autoDelete: false, arguments: null);
                IBasicProperties basicProperties = channel.CreateBasicProperties();
                basicProperties.Persistent = true;
                channel.BasicPublish(exchange: "", routingKey: queue_name, basicProperties: basicProperties, body: body);
                channel.Close();
            }
            catch (System.Exception)
            {
                return Json(false);
            }
            return Json(true);
        }

    }
}
