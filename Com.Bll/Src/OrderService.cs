using System.Text;
using Com.Api.Model;
using Com.Common;
using Com.Db;
using Com.Model;
using Com.Model.Enum;
using Newtonsoft.Json;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace Com.Bll;

/// <summary>
/// 订单服务
/// </summary>
public class OrderService
{
    /// <summary>
    /// 单例类的实例
    /// </summary>
    /// <returns></returns>
    public static readonly OrderService instance = new OrderService();
    /// <summary>
    /// 常用接口
    /// </summary>
    private FactoryConstant constant = null!;
    /// <summary>
    /// (Direct)接收挂单订单队列名称
    /// </summary>
    /// <value></value>
    public string key_order_send = "order_send";
    /// <summary>
    /// MQ基本属性
    /// </summary>
    /// <returns></returns>
    private IBasicProperties props = null!;

    /// <summary>
    /// private构造方法
    /// </summary>
    private OrderService()
    {

    }

    /// <summary>
    /// 初始化方法
    /// </summary>
    /// <param name="constant"></param>
    public void Init(FactoryConstant constant)
    {
        this.constant = constant;
        this.props = constant.i_model.CreateBasicProperties();
    }


    /// <summary>
    /// 挂单总入口
    /// </summary>
    /// <param name="market">交易对</param>
    /// <param name="uid">用户id</param>
    /// <param name="order">订单列表</param>
    /// <returns></returns>
    public List<MatchOrder> PlaceOrder(string market, long uid, List<PlaceOrder> order)
    {
        Req<List<MatchOrder>> req = new Req<List<MatchOrder>>();
        req.op = E_Op.place;
        req.market = market;
        req.data = new List<MatchOrder>();
        foreach (var item in order)
        {
            MatchOrder orderResult = new MatchOrder();
            orderResult.order_id = this.constant.worker.NextId();
            orderResult.client_id = item.client_id;
            orderResult.market = market;
            orderResult.uid = uid;
            orderResult.price = item.price ?? 0;
            orderResult.amount = item.amount;
            orderResult.total = item.price ?? 0 * item.amount;
            orderResult.create_time = DateTimeOffset.UtcNow;
            orderResult.amount_unsold = 0;
            orderResult.amount_done = item.amount;
            orderResult.deal_last_time = null;
            orderResult.side = item.side;
            orderResult.state = E_OrderState.unsold;
            orderResult.type = item.type;
            orderResult.data = null;
            orderResult.remarks = null;
            req.data.Add(orderResult);
        }
        this.constant.i_model.BasicPublish(exchange: this.key_order_send, routingKey: market, basicProperties: props, body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
        return req.data;
    }

}