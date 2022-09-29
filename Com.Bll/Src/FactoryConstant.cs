
using System.Diagnostics;
using System.Text;
using Com.Bll.Util;
using Com.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Snowflake.Core;
using StackExchange.Redis;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Com.Bll;

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
    /// 驱动接口
    /// </summary>
    public readonly IServiceProvider provider = null!;
    /// <summary>
    /// 雪花算法
    /// </summary>
    /// <returns></returns>
    public readonly IdWorker worker = new IdWorker(1, 1);
    /// <summary>
    /// 随机数
    /// </summary>
    /// <returns></returns>
    public readonly Random random = new Random();
    /// <summary>
    /// 秒表
    /// </summary>
    /// <returns></returns>
    public Stopwatch stopwatch = new Stopwatch();
    /// <summary>
    /// redis数据库
    /// </summary>
    public readonly IDatabase redis = null!;
    /// <summary>
    /// MongoDb数据库
    /// </summary>//
    public readonly IMongoDatabase mongodb = null!;
    /// <summary>
    /// mq 连接工厂
    /// </summary>
    public readonly ConnectionFactory connection_factory = null!;

    /// <summary>
    /// mq
    /// </summary>
    public readonly HelperMq mq_helper = null!;

    /// <summary>
    /// 初始化
    /// </summary>    
    /// <param name="provider">驱动接口</param>
    /// <param name="config">配置接口</param>
    /// <param name="environment">环境接口</param>
    /// <param name="logger">日志接口</param>
    public FactoryConstant(IServiceProvider provider, IConfiguration config, IHostEnvironment environment, ILogger logger)
    {
        this.provider = provider;
        this.config = config;
        this.environment = environment;
        this.logger = logger ?? NullLogger.Instance;



        try
        {
            string? redisConnection = config.GetConnectionString("Redis");
            if (!string.IsNullOrWhiteSpace(redisConnection))
            {
                ConnectionMultiplexer redisMultiplexer = ConnectionMultiplexer.Connect(redisConnection);
                this.redis = redisMultiplexer.GetDatabase();
            }
            else
            {
                this.logger.LogError($"Redis服务器地址没有找到");
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"redis服务器连接不上");
        }
        try
        {
            this.connection_factory = config.GetSection("RabbitMQ").Get<ConnectionFactory>();
            if (this.connection_factory != null)
            {
                // this.i_commection = this.connection_factory.CreateConnection();
                // this.i_model = this.i_commection.CreateModel();
                mq_helper = new HelperMq(this.connection_factory);
            }
            else
            {
                this.logger.LogError($"mq服务器地址没有找到");
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"MQ服务器连接不上");
        }
        try
        {
            //下面可以创建数据库   Code First
            string? dbConnection = config.GetConnectionString("Mssql");
            if (!string.IsNullOrWhiteSpace(dbConnection))
            {
                var options = new DbContextOptionsBuilder<DbContextEF>().UseSqlServer(dbConnection).Options;
                var factorydb = new PooledDbContextFactory<DbContextEF>(options);
                DbContextEF db = factorydb.CreateDbContext();
                db.Database.EnsureCreated();
            }
            else
            {
                this.logger.LogError($"mssql服务器地址没有找到");
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"mssql服务器连接不上");
        }
        try
        {
            string? mongodbConnection = config.GetConnectionString("MongoDb");
            if (!string.IsNullOrWhiteSpace(mongodbConnection))
            {
                MongoClient client = new MongoClient(mongodbConnection);
                this.mongodb = client.GetDatabase(new MongoUrlBuilder(mongodbConnection).DatabaseName);
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"MongoDb服务器连接不上");
        }
    }

    // /// <summary>
    // /// 初始化雪花算法
    // /// </summary>
    // /// <param name="service_type"></param>
    // public void InitSnowflake(E_ServiceType service_type)
    // {
    //     if (this.redis == null)
    //     {
    //         throw new Exception("请先初始化redis,再初始化雪花算法");
    //     }
    //     long worker_id = 0;
    //     do
    //     {
    //         worker_id = this.redis.HashIncrement(GetWorkerId(), service_type.ToString());
    //         if (worker_id > 31)
    //         {
    //             worker_id %= 32;
    //         }
    //     } while (worker_id == 0);
    //     this.worker = new IdWorker(worker_id, (int)service_type);
    // }

}