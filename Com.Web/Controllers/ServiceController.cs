using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Com.Web.Models;
using System.Text;
using Com.Common;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;

namespace Com.Web.Controllers;

public class ServiceController : Controller
{
    /// <summary>
    /// 常用接口
    /// </summary>
    public FactoryConstant constant = null!;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="environment"></param>
    /// <param name="logger"></param>
    public ServiceController(IConfiguration configuration, IHostEnvironment environment, ILogger<ServiceController> logger)
    {
        this.constant = new FactoryConstant(configuration, environment, logger ?? NullLogger<ServiceController>.Instance);
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

            constant.i_model.QueueDeclare(queue: queue_name, durable: true, exclusive: false, autoDelete: false, arguments: null);
            IBasicProperties basicProperties = this.constant.i_model.CreateBasicProperties();
            basicProperties.Persistent = true;
            this.constant.i_model.BasicPublish(exchange: "", routingKey: queue_name, basicProperties: basicProperties, body: body);
            this.constant.i_model.Close();
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
            this.constant.i_model.QueueDeclare(queue: queue_name, durable: true, exclusive: false, autoDelete: false, arguments: null);
            IBasicProperties basicProperties = this.constant.i_model.CreateBasicProperties();
            basicProperties.Persistent = true;
            this.constant.i_model.BasicPublish(exchange: "", routingKey: queue_name, basicProperties: basicProperties, body: body);
            this.constant.i_model.Close();
        }
        catch (System.Exception)
        {
            return Json(false);
        }
        return Json(true);
    }
}
