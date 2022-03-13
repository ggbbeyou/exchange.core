
using System.Text;
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
    /// 驱动接口
    /// </summary>
    public readonly IServiceProvider provider = null!;
    /// <summary>
    /// 数据库
    /// </summary>
    public readonly DbContextEF db = null!;
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
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"redis服务器连接不上");
        }
        try
        {
            var scope = provider.CreateScope();
            DbContextEF? db = scope.ServiceProvider.GetService<DbContextEF>();
            if (db != null)
            {
                this.db = db;
            }
            else
            {
                //下面可以创建数据库   Code First
                string? dbConnection = config.GetConnectionString("Mssql");
                if (!string.IsNullOrWhiteSpace(dbConnection))
                {
                    var options = new DbContextOptionsBuilder<DbContextEF>().UseSqlServer(dbConnection).Options;
                    var factorydb = new PooledDbContextFactory<DbContextEF>(options);
                    this.db = factorydb.CreateDbContext();
                    this.db.Database.EnsureCreated();
                }
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"DB服务器连接不上");
        }
        try
        {
            ConnectionFactory? factory = config.GetSection("RabbitMQ").Get<ConnectionFactory>();
            if (factory != null)
            {
                this.i_commection = factory!.CreateConnection();
                this.i_model = this.i_commection.CreateModel();
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"MQ服务器连接不上");
        }
    }

    /// <summary>
    /// MQ 发布消息
    /// </summary>
    /// <param name="exchange"></param>
    /// <param name="routingKey"></param>
    /// <param name="message"></param>
    public void MqPublish(string exchange, string routingKey, string message)
    {
        this.i_model.ExchangeDeclare(exchange: exchange, type: ExchangeType.Fanout);
        var body = Encoding.UTF8.GetBytes(message);
        this.i_model.BasicPublish(exchange: exchange, routingKey: routingKey, basicProperties: null, body);
    }

    /// <summary>
    /// MQ 订阅消息
    /// </summary>
    /// <param name="exchange"></param>
    /// <param name="routingKey"></param>
    /// <param name="action"></param>
    public string MqSubscribe(string exchange, string routingKey, Action<string> action)
    {
        this.i_model.ExchangeDeclare(exchange: exchange, type: ExchangeType.Fanout);
        string queueName = this.i_model.QueueDeclare().QueueName;
        this.i_model.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
        this.i_model.QueueBind(queue: queueName, exchange: exchange, routingKey: routingKey);
        EventingBasicConsumer consumer = new EventingBasicConsumer(this.i_model);
        consumer.Received += (model, ea) =>
        {
            byte[] body = ea.Body.ToArray();
            string message = Encoding.UTF8.GetString(body);
            action(message);
        };
        this.i_model.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
        return consumer.ConsumerTags.First();

    }




}