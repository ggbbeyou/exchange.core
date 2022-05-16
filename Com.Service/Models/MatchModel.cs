using System;
using System.Collections.Concurrent;
using Com.Db;
using Com.Api.Sdk.Enum;
using Com.Service.Match;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using RabbitMQ.Client;
using Com.Bll;

namespace Com.Service.Models;

/// <summary>
/// 撮合服务
/// </summary>
public class MatchModel
{
    /// <summary>
    /// 是否运行
    /// </summary>
    /// <value></value>
    public bool run { get; set; }
    /// <summary>
    /// 日志事件ID
    /// </summary>
    public EventId eventId;
    /// <summary>
    /// 交易对基本信息
    /// </summary>
    /// <value></value>
    public Market info { get; set; } = null!;
    /// <summary>
    /// 撮合服务
    /// </summary>
    /// <value></value>
    public MatchCore match_core { get; set; } = null!;
    /// <summary>
    /// 消息队列服务
    /// </summary>
    /// <value></value>
    public MQ mq { get; set; } = null!;
    /// <summary>
    /// 核心服务
    /// </summary>
    /// <value></value>
    public Core core { get; set; } = null!;
    /// <summary>
    /// mq 队列名称
    /// </summary>
    /// <typeparam name="string"></typeparam>
    /// <returns></returns>
    public HashSet<string> mq_queues = new HashSet<string>();
    /// <summary>
    /// mq 消费者事件标示
    /// </summary>
    /// <typeparam name="string"></typeparam>
    /// <returns></returns>
    public HashSet<string> mq_consumer = new HashSet<string>();
    /// <summary>
    /// 秒表
    /// </summary>
    /// <returns></returns>
    public Stopwatch stopwatch = new Stopwatch();
    /// <summary>
    /// mq 通道接口
    /// </summary>
    public readonly IModel i_model = null!;

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="info"></param>
    public MatchModel(Market info)
    {
        this.info = info;
        this.eventId = new EventId(1, info.symbol);
        this.i_model = FactoryService.instance.constant.i_commection.CreateModel();
    }
}