using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Com.Api.Models;
using Com.Model.Base;
using Snowflake;
using Newtonsoft.Json;
using System.Text;
using RabbitMQ.Client;
using Microsoft.Extensions.Configuration;

namespace Com.Api.Controllers
{
    /// <summary>
    /// 专有接口(需要登录的公开接口)
    /// </summary>
    public class ExclusiveController : Controller
    {
        private readonly ILogger<ExclusiveController> _logger;
        /// <summary>
        /// 配置接口
        /// </summary>
        public readonly IConfiguration configuration;
        public IdWorker worker = new IdWorker(1, 1);
        public ExclusiveController(IConfiguration configuration, ILogger<ExclusiveController> logger)
        {
            _logger = logger;
            this.configuration = configuration;
        }

        /// <summary>
        /// 下单
        /// </summary>
        /// <param name="name">交易对名称</param>
        /// <param name="type">订单类型</param>
        /// <param name="direction">交易方向</param>
        /// <param name="price">挂单价格</param>
        /// <param name="amount">挂单量</param>
        /// <returns></returns>
        public IActionResult OrderPlace(string name, E_OrderType type, E_Direction direction, decimal? price, decimal amount)
        {
            Order order = new Order()
            {
                id = worker.WorkerId.ToString(),
                name = name,
                uid = "0",
                price = price ?? 0,
                amount = amount,
                total = price ?? 0 * amount,
                time = DateTimeOffset.UtcNow,
                direction = direction,
                state = E_DealState.unsold,
                type = type,
            };
            string queue_name = $"order_send.{name}";
            string comman = JsonConvert.SerializeObject(order);
            byte[] body = Encoding.UTF8.GetBytes(comman);
            try
            {
                ConnectionFactory factory = this.configuration.GetSection("RabbitMQ").Get<ConnectionFactory>();
                IConnection connection = factory.CreateConnection();
                IModel channel = connection.CreateModel();
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
            return Json(order.id);
        }

        public IActionResult Index()
        {
            return View();
        }

    }
}
