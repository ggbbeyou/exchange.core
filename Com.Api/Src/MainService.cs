using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Com.Bll;

namespace Com.Api;

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
    /// 初始化
    /// </summary>
    /// <param name="provider">驱动</param>
    /// <param name="configuration">配置接口</param>
    /// <param name="environment">环境接口</param>
    /// <param name="logger">日志接口</param>
    public MainService(IServiceProvider provider, IConfiguration configuration, IHostEnvironment environment, ILogger<MainService> logger)
    {
        this.constant = new FactoryConstant(provider, configuration, environment, logger ?? NullLogger<MainService>.Instance);

    }

    /// <summary>
    /// 任务执行方法
    /// </summary>
    /// <param name="stoppingToken">后台任务取消令牌</param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.constant.logger.LogInformation("准备启动Api后台服务");
        try
        {
            FactoryService.instance.Init(this.constant);
            ServiceMinio service_minio = new ServiceMinio(this.constant.config, this.constant.logger);
            await service_minio.MakeBucket(FactoryService.instance.GetMinioRealname());


            this.constant.logger.LogInformation("启动Api后台服务成功");
        }
        catch (Exception ex)
        {
            this.constant.logger.LogError(ex, "启动Api后台服务异常");
        }
        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
    }

}
