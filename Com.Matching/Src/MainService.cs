using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace Com.Matching;

/// <summary>
/// 工作进程
/// </summary>
public class MainService : BackgroundService
{
    /// <summary>
    /// 配置接口
    /// </summary>
    private readonly IConfiguration configuration;
    /// <summary>
    /// 日志接口
    /// </summary>
    private readonly ILogger<MainService> logger;

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="configuration">配置接口</param>
    /// <param name="logger">日志接口</param>
    public MainService(IConfiguration configuration, ILogger<MainService> logger)
    {
        this.configuration = configuration;
        this.logger = logger ?? NullLogger<MainService>.Instance;
    }

    /// <summary>
    /// 任务执行方法
    /// </summary>
    /// <param name="stoppingToken">后台任务取消令牌</param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.logger.LogInformation("准备启动后台服务");
        try
        {
            Test test = new Test(this.configuration, this.logger);
            // FactoryMatching.instance.Info(this.configuration, this.logger);
            this.logger.LogInformation("启动后台服务成功");
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "启动后台服务异常");
        }
        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
    }

}