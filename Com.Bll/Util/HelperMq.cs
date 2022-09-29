
using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Com.Bll.Util;

/// <summary>
/// mq帮助类
/// </summary>
public class HelperMq
{
    /// <summary>
    /// mq 通道接口
    /// </summary>
    public readonly IModel i_model = null!;
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
    /// 初始化
    /// </summary>
    /// <param name="connectionFactory">mq连接接口</param>
    public HelperMq(ConnectionFactory connectionFactory)
    {
        IConnection i_commection = connectionFactory.CreateConnection();
        this.i_model = i_commection.CreateModel();
    }

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="i_commection">mq连接对象</param>
    public HelperMq(IConnection i_commection)
    {
        this.i_model = i_commection.CreateModel();
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
            IBasicProperties props = i_model.CreateBasicProperties();
            props.DeliveryMode = 2;
            i_model.QueueDeclare(queue: queue_name, durable: true, exclusive: false, autoDelete: false, arguments: null);
            i_model.BasicPublish(exchange: "", routingKey: queue_name, basicProperties: props, body: body);
            if (!mq_queues.Contains(queue_name))
            {
                mq_queues.Add(queue_name);
            }
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
    /// <returns>队列名,队列标记</returns>
    public (string queue_name, string consume_tag) MqReceive(string queue_name, Func<byte[], bool> func)
    {
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
        string consume_tag = i_model.BasicConsume(queue: queue_name, autoAck: false, consumer: consumer);
        if (!mq_queues.Contains(queue_name))
        {
            mq_queues.Add(queue_name);
        }
        if (!mq_consumer.Contains(consume_tag))
        {
            mq_consumer.Add(consume_tag);
        }
        return (queue_name, consume_tag);
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
            i_model.QueueDeclare(queue: queue_name, durable: true, exclusive: false, autoDelete: false, arguments: null);
            var properties = i_model.CreateBasicProperties();
            properties.Persistent = true;
            i_model.BasicPublish(exchange: "", routingKey: queue_name, basicProperties: properties, body: body);
            if (!mq_queues.Contains(queue_name))
            {
                mq_queues.Add(queue_name);
            }
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
    public (string queue_name, string consume_tag) MqWorker(string queue_name, Func<byte[], bool> func)
    {
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
        string consume_tag = i_model.BasicConsume(queue: queue_name, autoAck: false, consumer: consumer);
        if (!mq_queues.Contains(queue_name))
        {
            mq_queues.Add(queue_name);
        }
        if (!mq_consumer.Contains(consume_tag))
        {
            mq_consumer.Add(consume_tag);
        }
        return (queue_name, consume_tag);
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
    public (string queue_name, string consume_tag) MqSubscribe(string exchange, Action<byte[]> action)
    {
        i_model.ExchangeDeclare(exchange: exchange, type: ExchangeType.Fanout);
        string queueName = i_model.QueueDeclare().QueueName;
        i_model.QueueBind(queue: queueName, exchange: exchange, routingKey: "");
        EventingBasicConsumer consumer = new EventingBasicConsumer(i_model);
        consumer.Received += (model, ea) =>
        {
            action(ea.Body.ToArray());
        };
        string consume_tag = i_model.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
        if (!mq_consumer.Contains(consume_tag))
        {
            mq_consumer.Add(consume_tag);
        }
        return (queueName, consume_tag);
    }

    /// <summary>
    /// 请除队列
    /// </summary>
    public void MqDeletePurge()
    {
        try
        {
            foreach (var item in mq_queues)
            {
                i_model.QueuePurge(item);
            }
        }
        catch (System.Exception ex)
        {
            FactoryService.instance.constant.logger.LogError(ex, "清除mq队列失败");
        }
    }

    /// <summary>
    /// 请除队列
    /// </summary>
    public void MqDeletePurge(string queueName)
    {
        try
        {
            i_model.QueuePurge(queueName);
        }
        catch (System.Exception ex)
        {
            FactoryService.instance.constant.logger.LogError(ex, "清除mq队列失败");
        }
    }

    /// <summary>
    /// 删除队列
    /// </summary>
    /// <param name="queueName"></param>
    public void MqDelete(string queueName)
    {
        try
        {
            i_model.QueueDelete(queueName);
        }
        catch (System.Exception ex)
        {
            FactoryService.instance.constant.logger.LogError(ex, "删除mq队列失败");
        }
    }

    /// <summary>
    /// 删除队列
    /// </summary>
    /// <param name="queueName"></param>
    public void MqDelete()
    {
        try
        {
            foreach (var item in mq_queues)
            {
                i_model.QueueDelete(item);
            }
        }
        catch (System.Exception ex)
        {
            FactoryService.instance.constant.logger.LogError(ex, "删除mq队列失败");
        }
    }

    /// <summary>
    /// 删除消费者
    /// </summary>
    public void MqDeleteConsumer()
    {
        try
        {
            foreach (var item in mq_consumer)
            {
                i_model.BasicCancel(item);
            }
        }
        catch (System.Exception ex)
        {
            FactoryService.instance.constant.logger.LogError(ex, "删除mq消费者失败");
        }
    }

    /// <summary>
    /// 删除消费者
    /// </summary>
    public void MqDeleteConsumer(string consume_tag)
    {
        try
        {
            i_model.BasicCancel(consume_tag);
        }
        catch (System.Exception ex)
        {
            FactoryService.instance.constant.logger.LogError(ex, "删除mq消费者失败");
        }
    }

    /// <summary>
    /// 关闭
    /// </summary>
    public void Close()
    {
        i_model.Close();
    }

}