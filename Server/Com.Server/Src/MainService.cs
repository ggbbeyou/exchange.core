using Com.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Com.Server
{
    /// <summary>
    /// 网站后台服务
    /// </summary>
    public class MainService : BackgroundService
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
        /// <param name="provider"></param>
        /// <param name="logger"></param>
        public MainService(IConfiguration configuration, IHostEnvironment environment, IServiceProvider provider, ILogger<MainService> logger)
        {
            this.constant = new FactoryConstant(configuration, environment, logger);
        }

        /// <summary>
        /// 任务执行方法
        /// </summary>
        /// <param name="stoppingToken">后台任务取消令牌</param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this.constant.logger.LogInformation("准备启动业务后台服务");
            try
            {
                FactoryMatching.instance.Init(this.constant);
                this.constant.logger.LogInformation("启动业务后台服务成功");
            }
            catch (Exception ex)
            {
                this.constant.logger.LogError(ex, "启动业务后台服务异常");
            }
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}