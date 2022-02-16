using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Com.Api
{
    /// <summary>
    /// 网站后台服务
    /// </summary>
    public class MainService : BackgroundService
    {
        /// <summary>
        /// 配置文件接口
        /// </summary>
        private readonly IConfiguration _configuration;
        /// <summary>
        /// redis接口
        /// </summary>
        private readonly IRedisCacheClient _redisCacheClient;
        /// <summary>
        /// 环境接口
        /// </summary>
        private readonly IHostEnvironment _environment;
        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="configuration">配置接口</param>
        /// <param name="redisCacheClient">redis接口</param>
        /// <param name="environment">环境接口</param>
        /// <param name="logger">日志接口</param>
        public MainService(IConfiguration configuration, IRedisCacheClient redisCacheClient, IHostEnvironment environment, ILogger<MainService>? logger = null)
        {
            this._configuration = configuration;
            this._redisCacheClient = redisCacheClient;
            this._environment = environment;
            this._logger = logger ?? NullLogger<MainService>.Instance;
        }

        /// <summary>
        /// 任务执行方法
        /// </summary>
        /// <param name="stoppingToken">后台任务取消令牌</param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this._logger.LogInformation("准备启动后台服务");
            try
            {
                FactoryMatching.instance.Init(this._configuration, this._redisCacheClient, this._environment, this._logger);
                await FactoryMatching.instance.Start();
                this._logger.LogInformation("启动后台服务成功");
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "启动后台服务异常");
            }
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

    }
}