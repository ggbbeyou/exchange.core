using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Com.Api.Models;

using Snowflake;
using Newtonsoft.Json;
using System.Text;
using RabbitMQ.Client;
using Microsoft.Extensions.Configuration;
using Com.Model.Enum;
using Com.Model;
using Com.Common;
using Microsoft.Extensions.Logging.Abstractions;

namespace Com.Api.Controllers
{
    /// <summary>
    /// 专有接口(需要登录的公开接口)
    /// </summary>
    [Route("api/private")]
    public class ExclusiveController : Controller
    {
        /// <summary>
        /// 常用接口
        /// </summary>
        public FactoryConstant constant = null!;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="configuration">配置接口</param>
        /// <param name="environment">环境接口</param>
        /// <param name="logger">日志接口</param>
        public ExclusiveController(IConfiguration configuration, IHostEnvironment environment, ILogger<publicityController> logger)
        {
            this.constant = new FactoryConstant(configuration, environment, logger ?? NullLogger<publicityController>.Instance);
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
        public IActionResult OrderPlace()
        {
            return Json(null);
        }



    }
}
