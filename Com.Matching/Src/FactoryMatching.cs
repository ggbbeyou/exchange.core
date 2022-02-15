using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;
using Com.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Snowflake;

namespace Com.Matching;

/// <summary>
/// 撮合工厂
/// </summary>
public class FactoryMatching
{
    /// <summary>
    /// 单例类的实例
    /// </summary>
    /// <returns></returns>
    public static readonly FactoryMatching instance = new FactoryMatching();
    /// <summary>
    /// 常用接口
    /// </summary>
    public FactoryConstant constant = null!;
    /// <summary>
    /// 服务器名称
    /// </summary>
    public string server_name = null!;

    /// <summary>
    /// 撮合集合
    /// </summary>
    /// <typeparam name="string">交易对</typeparam>
    /// <typeparam name="Core">撮合器</typeparam>
    /// <returns></returns>
    public Dictionary<string, Core> cores = new Dictionary<string, Core>();

    /// <summary>
    /// 私有构造方法
    /// </summary>
    private FactoryMatching()
    {

    }

    /// <summary>
    /// 初始化方法
    /// </summary>
    /// <param name="configuration">配置接口</param>
    /// <param name="logger">日志接口</param>
    public void Info(FactoryConstant constant)
    {
        this.constant = constant;
        this.server_name = this.constant.config.GetValue<string>("server_name");
        ServiceStatus();
    }

    /// <summary>
    /// 撮合引擎状态监测 
    /// open:servername:name:price
    /// close:servername:name
    /// </summary>
    public void ServiceStatus()
    {
        string queue_name = $"MatchingService";
        this.constant.i_model.QueueDeclare(queue: queue_name, durable: true, exclusive: false, autoDelete: false, arguments: null);
        var consumer = new EventingBasicConsumer(this.constant.i_model);
        consumer.Received += (model, ea) =>
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            if (!string.IsNullOrWhiteSpace(message))
            {
                string[] status = message.Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (this.server_name == status[1])
                {
                    string name = status[2].ToLower();
                    switch (status[0])
                    {
                        case "open":
                            decimal price = decimal.Parse(status[3]);
                            if (!this.cores.ContainsKey(name))
                            {
                                Core core = new Core(name, this.constant);
                                core.Start(price);
                                this.cores.Add(name, core);
                            }
                            else
                            {
                                Core core = this.cores[name];
                                core.Start(price);
                            }
                            break;
                        case "close":
                            if (this.cores.ContainsKey(name))
                            {
                                Core core = this.cores[name];
                                core.Stop();
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            this.constant.i_model.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        };
        this.constant.i_model.BasicConsume(queue: queue_name, autoAck: false, consumer: consumer);
    }

}