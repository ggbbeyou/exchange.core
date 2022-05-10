
using System.Diagnostics;
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
    /// mq 连接工厂
    /// </summary>
    public readonly ConnectionFactory connection_factory = null!;
    /// <summary>
    /// mq 连接接口
    /// </summary>
    public readonly IConnection i_commection = null!;
    // /// <summary>
    // /// mq 通道接口
    // /// </summary>
    // public readonly IModel i_model = null!;

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
            ConnectionFactory? factory = config.GetSection("RabbitMQ").Get<ConnectionFactory>();
            if (factory != null)
            {
                this.i_commection = factory!.CreateConnection();
                // this.i_model = this.i_commection.CreateModel();
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
    }

    /// <summary>
    /// 上分布式锁
    /// </summary>
    /// <param name="key">redis键</param>
    /// <param name="value">redis值</param>
    /// <param name="timeout">超时(毫秒)</param>
    /// <param name="action">方法</param>
    public void Look(string key, string value, long timeout = 5000, Action action = null!)
    {
        if (action == null)
        {
            return;
        }
        try
        {
            if (this.redis.StringSet(key, value, TimeSpan.FromMilliseconds(timeout), When.NotExists))
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "redis分布试锁错误(业务)");
                }
                finally
                {
                    this.redis.KeyDelete(key);
                }
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "redis分布试锁错误");
        }
    }


    /// <summary>
    /// MQ 简单的队列 发送消息
    /// </summary>
    /// <param name="queue_name"></param>
    /// <param name="body"></param>
    public bool MqSend(string queue_name, byte[] body)
    {
        try
        {
            IModel i_model = this.i_commection.CreateModel();
            IBasicProperties props = i_model.CreateBasicProperties();
            props.DeliveryMode = 2;
            i_model.QueueDeclare(queue: queue_name, durable: true, exclusive: false, autoDelete: false, arguments: null);
            i_model.BasicPublish(exchange: "", routingKey: queue_name, basicProperties: props, body: body);
        }
        catch (System.Exception ex)
        {
            FactoryService.instance.constant.logger.LogError(ex, "MQ 简单的队列 发送消息");
            return false;
        }
        return true;
    }

    /// <summary>
    /// MQ 简单的队列 接收消息
    /// </summary>
    /// <param name="queue_name"></param>
    /// <param name="func"></param>
    /// <returns>队列标记</returns>
    public string MqReceive(string queue_name, Func<byte[], bool> func)
    {
        IModel i_model = this.i_commection.CreateModel();
        i_model.QueueDeclare(queue: queue_name, durable: true, exclusive: false, autoDelete: false, arguments: null);
        EventingBasicConsumer consumer = new EventingBasicConsumer(i_model);
        consumer.Received += (model, ea) =>
        {
            if (func(ea.Body.ToArray()))
            {
                i_model.BasicAck(deliveryTag: ea.DeliveryTag, multiple: true);
            }
            else
            {
                i_model.BasicNack(deliveryTag: ea.DeliveryTag, multiple: true, requeue: true);
            }
        };
        return i_model.BasicConsume(queue: queue_name, autoAck: false, consumer: consumer);
    }

    /// <summary>
    /// MQ 发布工作任务
    /// </summary>
    /// <param name="queue_name"></param>
    /// <param name="body"></param>
    public bool MqTask(string queue_name, byte[] body)
    {
        try
        {
            IModel i_model = this.i_commection.CreateModel();
            i_model.QueueDeclare(queue: queue_name, durable: true, exclusive: false, autoDelete: false, arguments: null);
            var properties = i_model.CreateBasicProperties();
            properties.Persistent = true;
            i_model.BasicPublish(exchange: "", routingKey: queue_name, basicProperties: properties, body: body);
        }
        catch (System.Exception ex)
        {
            FactoryService.instance.constant.logger.LogError(ex, "MQ 发布工作任务");
            return false;
        }
        return true;
    }

    /// <summary>
    /// MQ 处理工作任务
    /// </summary>
    /// <param name="queue_name"></param>
    /// <param name="func"></param>
    /// <returns></returns>
    public string MqWorker(string queue_name, Func<byte[], bool> func)
    {
        IModel i_model = this.i_commection.CreateModel();
        i_model.QueueDeclare(queue: queue_name, durable: true, exclusive: false, autoDelete: false, arguments: null);
        i_model.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        EventingBasicConsumer consumer = new EventingBasicConsumer(i_model);
        consumer.Received += (model, ea) =>
        {
            if (func(ea.Body.ToArray()))
            {
                i_model.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            else
            {
                i_model.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
        };
        return i_model.BasicConsume(queue: queue_name, autoAck: false, consumer: consumer);
    }

    /// <summary>
    /// MQ 发布消息
    /// </summary>
    /// <param name="exchange"></param>
    /// <param name="message"></param>
    public bool MqPublish(string exchange, string message)
    {
        try
        {
            IModel i_model = this.i_commection.CreateModel();
            i_model.ExchangeDeclare(exchange: exchange, type: ExchangeType.Fanout);
            var body = Encoding.UTF8.GetBytes(message);
            i_model.BasicPublish(exchange: exchange, routingKey: "", basicProperties: null, body);
        }
        catch (System.Exception ex)
        {
            FactoryService.instance.constant.logger.LogError(ex, "MQ发布消息错误");
            return false;
        }
        return true;
    }

    /// <summary>
    /// MQ 订阅消息
    /// </summary>
    /// <param name="exchange"></param>
    /// <param name="action"></param>
    public string MqSubscribe(string exchange, Action<byte[]> action)
    {
        IModel i_model = this.i_commection.CreateModel();
        i_model.ExchangeDeclare(exchange: exchange, type: ExchangeType.Fanout);
        string queueName = i_model.QueueDeclare().QueueName;
        i_model.QueueBind(queue: queueName, exchange: exchange, routingKey: "");
        EventingBasicConsumer consumer = new EventingBasicConsumer(i_model);
        consumer.Received += (model, ea) =>
        {
            action(ea.Body.ToArray());
        };
        return i_model.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
    }

    /// <summary>
    /// 删除消费者
    /// </summary>
    /// <param name="consumerTag">消费者标示</param>
    public void MqDeleteConsumer(string consumerTag)
    {
        try
        {
            IModel i_model = this.i_commection.CreateModel();
            i_model.BasicCancel(consumerTag);
        }
        catch (System.Exception ex)
        {
            FactoryService.instance.constant.logger.LogError(ex, "删除mq消费者失败");
        }
    }

    /// <summary>
    /// 请除队列
    /// </summary>
    /// <param name="consumerTag"></param>
    public void MqDeletePurge(string consumerTag)
    {
        try
        {
            IModel i_model = this.i_commection.CreateModel();
            i_model.QueuePurge(consumerTag);
        }
        catch (System.Exception ex)
        {
            FactoryService.instance.constant.logger.LogError(ex, "清除mq队列失败");
        }
    }

}