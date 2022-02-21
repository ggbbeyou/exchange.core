using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Com.Server
{
    /// <summary>
    /// 网站后台服务
    /// </summary>
    public class MainService : BackgroundService
    {
        public MainService(IConfiguration configuration, IHostEnvironment environment, IServiceProvider provider, ILogger<MainService> logger)
        {
            var a = configuration.GetConnectionString("RedisConnectionString");
            logger.LogInformation(a);
            logger.LogError(a);
        }
        /// <summary>
        /// 任务执行方法
        /// </summary>
        /// <param name="stoppingToken">后台任务取消令牌</param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 后台任务执行代码
            await Task.Delay(1000, stoppingToken);
        }
    }
}