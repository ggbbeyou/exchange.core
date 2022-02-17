using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Com.Api.Models;
using Com.Common;
using Microsoft.Extensions.Logging.Abstractions;

namespace Com.Api.Controllers
{
    /// <summary>
    /// 公开的接口(不需要登录的公开接口)
    /// </summary>
    [Route("api/public")]
    public class publicityController : Controller
    {
        /// <summary>
        /// 常用接口
        /// </summary>
        public FactoryConstant constant = null!;

        public publicityController(IConfiguration configuration, IHostEnvironment environment, ILogger<publicityController> logger)
        {
            this.constant = new FactoryConstant(configuration, environment, logger ?? NullLogger<publicityController>.Instance);
        }

        public IActionResult Index()
        {
            return View();
        }

    }
}
