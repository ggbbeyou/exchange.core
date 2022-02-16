using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Com.Api.Models;

namespace Com.Api.Controllers
{
    /// <summary>
    /// 公开的接口(不需要登录的公开接口)
    /// </summary>
    [Route("api/public")]
    public class publicityController : Controller
    {
        private readonly ILogger<publicityController> logger;

        public publicityController(ILogger<publicityController> logger)
        {
            this.logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

    }
}
