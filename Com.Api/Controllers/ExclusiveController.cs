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
    /// 专有接口(需要登录的公开接口)
    /// </summary>
    public class ExclusiveController : Controller
    {
        private readonly ILogger<ExclusiveController> _logger;

        public ExclusiveController(ILogger<ExclusiveController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

    }
}
