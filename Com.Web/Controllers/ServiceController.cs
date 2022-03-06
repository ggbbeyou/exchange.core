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
    public ServiceController(IServiceProvider provider, IConfiguration configuration, IHostEnvironment environment, ILogger<ServiceController> logger)
    {
        this.constant = new FactoryConstant(provider, configuration, environment, logger ?? NullLogger<ServiceController>.Instance);
    }

    public IActionResult Index()
    {
        return View();
    }


}
