
using Com.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Snowflake;
using StackExchange.Redis;

namespace Com.Common;

/// <summary>
/// 常用接口工厂类
/// </summary>
public class FactoryConstant
{
    /// <summary>
    /// 日志接口
    /// </summary>
    public readonly ILogger logger;
    /// <summary>
    /// 环境接口
    /// </summary>
    public readonly IHostEnvironment environment;
    /// <summary>
    /// 配置接口
    /// </summary>
    public readonly IConfiguration config;
    /// <summary>
    /// 雪花算法
    /// </summary>
    /// <returns></returns>
    public readonly IdWorker worker = new IdWorker(1, 1);
    /// <summary>
    /// 数据库
    /// </summary>
    public readonly DbContextEF db;
    /// <summary>
    /// redis数据库
    /// </summary>
    public readonly IDatabase redis = null!;
    /// <summary>
    /// mq 连接工厂
    /// </summary>
    public readonly ConnectionFactory connection_factory = null!;
    /// <summary>
    /// mq 连接接口
    /// </summary>
    public readonly IConnection i_commection = null!;
    /// <summary>
    /// mq 通道接口
    /// </summary>
    public readonly IModel i_model = null!;

    /// <summary>
    /// 初始化
    /// </summary>    
    /// <param name="config">配置接口</param>
    /// <param name="environment">环境接口</param>
    /// <param name="logger">日志接口</param>
    public FactoryConstant(IConfiguration config, IHostEnvironment environment, ILogger logger)
    {
        this.config = config;
        this.environment = environment;
        this.logger = logger ?? NullLogger.Instance;
        string? redisConnection = config.GetConnectionString("Redis");
        try
        {
            if (!string.IsNullOrWhiteSpace(redisConnection))
            {
                ConnectionMultiplexer redisMultiplexer = ConnectionMultiplexer.Connect(redisConnection);
                this.redis = redisMultiplexer.GetDatabase();
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"redis服务器连接不上,地址:{redisConnection}");
        }
        string? dbConnection = config.GetConnectionString("Mysql");
        try
        {
            var options = new DbContextOptionsBuilder<DbContextEF>().UseMySQL(dbConnection).Options;
            var factorydb = new PooledDbContextFactory<DbContextEF>(options);
            this.db = factorydb.CreateDbContext();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"DB服务器连接不上,地址:{dbConnection}");
        }
        ConnectionFactory? factory = config.GetSection("RabbitMQ").Get<ConnectionFactory>();
        try
        {
            if (factory != null)
            {
                this.i_commection = factory!.CreateConnection();
                this.i_model = this.i_commection.CreateModel();
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"MQ服务器连接不上,地址:{JsonConvert.SerializeObject(factory)}");
        }
    }

}