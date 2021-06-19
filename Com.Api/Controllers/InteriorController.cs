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
    /// 内部接口(系统内部)
    /// </summary>
    public class InteriorController : Controller
    {
        private readonly ILogger<InteriorController> _logger;

        public InteriorController(ILogger<InteriorController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        

       
    }
}
