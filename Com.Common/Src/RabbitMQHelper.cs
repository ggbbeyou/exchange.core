using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Com.Common;

public class RabbitMQHelper
{
    private readonly ConnectionFactory connectionFactory;
    private readonly IConnection conn;


    private ConcurrentDictionary<string, IModel> ModelDic = new ConcurrentDictionary<string, IModel>();

    public RabbitMQHelper(ConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
        conn = connectionFactory.CreateConnection();
    }


    // 步骤：初始化链接->声明交换器->声明队列->换机器与队列绑定->发布消息。
    // 注意的是，我将Model存到了ConcurrentDictionary里面，因为声明与绑定是非常耗时的，
    // 其次，往重复的队列发送消息是不需要重新初始化的。

    #region Publish(发布)的封装'

    /// <summary>
    /// 交换器声明
    /// </summary>
    /// <param name="iModel"></param>
    /// <param name="exchange">交换器</param>
    /// <param name="type">交换器类型：
    /// 1、Direct Exchange – 处理路由键。需要将一个队列绑定到交换机上，要求该消息与一个特定的路由键完全
    /// 匹配。这是一个完整的匹配。如果一个队列绑定到该交换机上要求路由键 “dog”，则只有被标记为“dog”的
    /// 消息才被转发，不会转发dog.puppy，也不会转发dog.guard，只会转发dog
    /// 2、Fanout Exchange – 不处理路由键。你只需要简单的将队列绑定到交换机上。一个发送到交换机的消息都
    /// 会被转发到与该交换机绑定的所有队列上。很像子网广播，每台子网内的主机都获得了一份复制的消息。Fanout
    /// 交换机转发消息是最快的。
    /// 3、Topic Exchange – 将路由键和某模式进行匹配。此时队列需要绑定要一个模式上。符号“#”匹配一个或多
    /// 个词，符号“*”匹配不多不少一个词。因此“audit.#”能够匹配到“audit.irs.corporate”，但是“audit.*”
    /// 只会匹配到“audit.irs”。</param>
    /// <param name="durable">持久化</param>
    /// <param name="autoDelete">自动删除</param>
    /// <param name="arguments">参数</param>
    private void ExchangeDeclare(IModel iModel, string exchange, string type = ExchangeType.Direct, bool durable = true, bool autoDelete = false, IDictionary<string, object>? arguments = null)
    {
        exchange = string.IsNullOrWhiteSpace(exchange) ? "" : exchange.Trim();
        iModel.ExchangeDeclare(exchange, type, durable, autoDelete, arguments);
    }

    /// <summary>
    /// 队列声明
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="queue">队列</param>
    /// <param name="durable">持久化</param>
    /// <param name="exclusive">排他队列，如果一个队列被声明为排他队列，该队列仅对首次声明它的连接可见，
    /// 并在连接断开时自动删除。这里需要注意三点：其一，排他队列是基于连接可见的，同一连接的不同信道是可
    /// 以同时访问同一个连接创建的排他队列的。其二，“首次”，如果一个连接已经声明了一个排他队列，其他连
    /// 接是不允许建立同名的排他队列的，这个与普通队列不同。其三，即使该队列是持久化的，一旦连接关闭或者
    /// 客户端退出，该排他队列都会被自动删除的。这种队列适用于只限于一个客户端发送读取消息的应用场景。</param>
    /// <param name="autoDelete">自动删除</param>
    /// <param name="arguments">参数</param>
    private void QueueDeclare(IModel channel, string queue, bool durable = true, bool exclusive = false, bool autoDelete = false, IDictionary<string, object>? arguments = null)
    {
        queue = string.IsNullOrWhiteSpace(queue) ? "UndefinedQueueName" : queue.Trim();
        channel.QueueDeclare(queue, durable, exclusive, autoDelete, arguments);
    }

    /// <summary>
    /// 获取Model
    /// </summary>
    /// <param name="exchange">交换机名称</param>
    /// <param name="queue">队列名称</param>
    /// <param name="routingKey"></param>
    /// <param name="isProperties">是否持久化</param>
    /// <returns></returns>
    private IModel GetModel(string exchange, string queue, string routingKey, bool isProperties = false)
    {
        return ModelDic.GetOrAdd(queue, key =>
        {
            var model = conn.CreateModel();
            ExchangeDeclare(model, exchange, ExchangeType.Fanout, isProperties);
            QueueDeclare(model, queue, isProperties);
            model.QueueBind(queue, exchange, routingKey);
            ModelDic[queue] = model;
            return model;
        });
    }

    /// <summary>
    /// 发布消息
    /// </summary>
    /// <param name="routingKey">路由键</param>
    /// <param name="body">队列信息</param>
    /// <param name="exchange">交换机名称</param>
    /// <param name="queue">队列名</param>
    /// <param name="isProperties">是否持久化</param>
    /// <returns></returns>
    public void Publish(string exchange, string queue, string routingKey, string body, bool isProperties = false)
    {
        var channel = GetModel(exchange, queue, routingKey, isProperties);
        try
        {
            channel.BasicPublish(exchange, routingKey, null, Encoding.UTF8.GetBytes(body));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    #endregion

    #region Subscribe(订阅)的封装

    /// <summary>
    /// 获取Model
    /// </summary>
    /// <param name="queue">队列名称</param>
    /// <param name="isProperties"></param>
    /// <returns></returns>
    private IModel GetModel(string queue, bool isProperties = false)
    {
        return ModelDic.GetOrAdd(queue, value =>
         {
             var model = conn.CreateModel();
             QueueDeclare(model, queue, isProperties);
                 //每次消费的消息数
                 model.BasicQos(0, 1, false);
             ModelDic[queue] = model;
             return model;
         });
    }

    /// <summary>
    /// 接收消息
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="queue">队列名称</param>
    /// <param name="isProperties"></param>
    /// <param name="handler">消费处理</param>
    /// <param name="isDeadLetter"></param>
    public void Subscribe<T>(string queue, bool isProperties, Action<T?> handler, bool isDeadLetter) where T : class
    {
        //队列声明
        var channel = GetModel(queue, isProperties);
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            string json = Encoding.UTF8.GetString(body);
            var msg = JsonConvert.DeserializeObject<T>(json);
            try
            {
                handler(msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                    // ex.GetInnestException().WriteToFile("队列接收消息", "RabbitMq");
                    // if (!isDeadLetter)
                    //     PublishToDead<DeadLetterQueue>(queue, msgStr, ex);
                }
            finally
            {
                channel.BasicAck(ea.DeliveryTag, false);
            }
        };
        channel.BasicConsume(queue, false, consumer);
    }

    #endregion

    #region  Pull(拉)的封装

    /// <summary>
    /// 获取消息
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="exchange"></param>
    /// <param name="queue"></param>
    /// <param name="routingKey"></param>
    /// <param name="handler">消费处理</param>
    private void Poll<T>(string exchange, string queue, string routingKey, Action<T?> handler) where T : class
    {
        var channel = GetModel(exchange, queue, routingKey);
        var result = channel.BasicGet(queue, false);
        if (result == null)
            return;
        var body = result.Body.ToArray();
        string json = Encoding.UTF8.GetString(body);
        var msg = JsonConvert.DeserializeObject<T>(json);
        try
        {
            handler(msg);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            channel.BasicAck(result.DeliveryTag, false);
        }
    }

    #endregion

    #region Rpc(远程调用)的封装

    /// <summary>
    /// RPC客户端
    /// </summary>
    /// <param name="exchange"></param>
    /// <param name="queue"></param>
    /// <param name="routingKey"></param>
    /// <param name="body"></param>
    /// <param name="isProperties"></param>
    /// <returns></returns>
    public Task<string> RpcClient(string exchange, string queue, string routingKey, string body, bool isProperties = false, CancellationToken cancellationToken = default(CancellationToken))
    {
        ConcurrentDictionary<string, TaskCompletionSource<string>> callbackMapper = new ConcurrentDictionary<string, TaskCompletionSource<string>>();

        var channel = GetModel(exchange, queue, routingKey, isProperties);
        string replyQueueName = channel.QueueDeclare().QueueName;
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            if (!callbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out TaskCompletionSource<string>? tcs))
                return;
            var body = ea.Body.ToArray();
            var response = Encoding.UTF8.GetString(body);
            tcs.TrySetResult(response);
        };
        IBasicProperties props = channel.CreateBasicProperties();
        var correlationId = Guid.NewGuid().ToString();
        props.CorrelationId = correlationId;
        props.ReplyTo = replyQueueName;
        var messageBytes = Encoding.UTF8.GetBytes(body);
        var tcs = new TaskCompletionSource<string>();
        callbackMapper.TryAdd(correlationId, tcs);
        channel.BasicPublish(
            exchange: "",
            routingKey: routingKey,
            basicProperties: props,
            body: messageBytes);

        channel.BasicConsume(
            consumer: consumer,
            queue: replyQueueName,
            autoAck: true);

        cancellationToken.Register(() => callbackMapper.TryRemove(correlationId, out var tmp));
        return tcs.Task;
    }

    /// <summary>
    /// RPC服务端
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="exchange"></param>
    /// <param name="queue"></param>
    /// <param name="isProperties"></param>
    /// <param name="handler"></param>
    /// <param name="isDeadLetter"></param>
    public void RpcService<T>(string exchange, string queue, bool isProperties, Func<T?, T> handler, bool isDeadLetter)
    {
        //队列声明
        var channel = GetModel(queue, isProperties);
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var msgStr = Encoding.UTF8.GetString(body);
            var msg = JsonConvert.DeserializeObject<T>(msgStr);
            var props = ea.BasicProperties;
            var replyProps = channel.CreateBasicProperties();
            replyProps.CorrelationId = props.CorrelationId;
            try
            {
                msg = handler(msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                channel.BasicPublish(exchange, props.ReplyTo, replyProps, Encoding.UTF8.GetBytes(msgStr));
                channel.BasicAck(ea.DeliveryTag, false);
            }
        };
        channel.BasicConsume(queue, false, consumer);
    }

    #endregion

}