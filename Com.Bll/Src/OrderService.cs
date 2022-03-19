using Com.Db;
using Com.Db.Enum;
using Com.Db.Model;
using Newtonsoft.Json;
using System.Text;
using RabbitMQ.Client;
using StackExchange.Redis;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Com.Bll;

/// <summary>
/// Service:订单
/// </summary>
public class OrderService
{
    /// <summary>
    /// 数据库
    /// </summary>
    public DbContextEF db = null!;
    /// <summary>
    /// 秒表
    /// </summary>
    /// <returns></returns>
    private Stopwatch stopwatch = new Stopwatch();

    /// <summary>
    /// 初始化
    /// </summary>
    public OrderService()
    {
        var scope = FactoryService.instance.constant.provider.CreateScope();
        this.db = scope.ServiceProvider.GetService<DbContextEF>()!;
    }

    /// <summary>
    /// 挂单总入口
    /// </summary>
    /// <param name="market">交易对</param>
    /// <param name="uid">用户id</param>
    /// <param name="order">订单列表</param>
    /// <returns></returns>
    public CallResponse<List<Orders>> PlaceOrder(long market, List<Orders> order)
    {
        stopwatch.Restart();
        db.Orders.AddRange(order);
        db.SaveChanges();
        stopwatch.Stop();
        TimeSpan ts = stopwatch.Elapsed;
        FactoryService.instance.constant.logger.LogInformation($"插入{order.Count}条订单到DB:耗时:{stopwatch.Elapsed.ToString()}");
        CallRequest<List<Orders>> req = new CallRequest<List<Orders>>();
        req.op = E_Op.place;
        req.market = market;
        req.data = order;
        stopwatch.Restart();
        FactoryService.instance.constant.MqSend(FactoryService.instance.GetMqOrderPlace(market), Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
        stopwatch.Stop();
        FactoryService.instance.constant.logger.LogInformation($"插入{order.Count}条订单到Mq:耗时:{stopwatch.Elapsed.ToString()}");
        CallResponse<List<Orders>> res = new CallResponse<List<Orders>>();
        res.op = E_Op.place;
        res.success = true;
        res.code = E_Res_Code.ok;
        res.market = market;
        res.message = null;
        res.data = order;
        return res;
    }


    /// <summary>
    /// 挂单总入口
    /// </summary>  
    /// <param name="market">交易对</param>
    /// <param name="uid">用户id</param>
    /// <param name="type">1:按交易对全部撤单,2:按交易对和用户全部撤单,3:按用户和订单id撤单,4:按用户和用户订单id撤单</param>
    /// <param name="order">订单列表</param>
    /// <returns></returns>
    public CallResponse<KeyValuePair<long, List<long>>> CancelOrder(long market, long uid, int type, List<long> order)
    {
        CallRequest<KeyValuePair<long, List<long>>> req = new CallRequest<KeyValuePair<long, List<long>>>();
        req.op = E_Op.place;
        req.market = market;
        req.data = new KeyValuePair<long, List<long>>(uid, order);
        FactoryService.instance.constant.MqSend(FactoryService.instance.GetMqOrderPlace(market), Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
        CallResponse<KeyValuePair<long, List<long>>> res = new CallResponse<KeyValuePair<long, List<long>>>();
        res.op = E_Op.place;
        res.success = true;
        res.code = E_Res_Code.ok;
        res.market = market;
        res.message = null;
        res.data = new KeyValuePair<long, List<long>>(uid, order);
        return res;
    }

    /// <summary>
    /// 更新订单
    /// </summary>
    /// <param name="data"></param>
    public void UpdateOrder(List<(long order_id, decimal amount, DateTimeOffset last_deal_date)> data)
    {
        List<Orders> orders = this.db.Orders.Where(P => data.Select(p => p.order_id).Contains(P.order_id)).ToList();
        foreach (var item in data)
        {
            Orders? order = orders.FirstOrDefault(p => p.order_id == item.order_id);
            if (order == null)
            {
                continue;
            }
            order.amount_done += item.amount;
            order.amount_unsold -= item.amount;
            order.deal_last_time = item.last_deal_date;
            if (order.amount_unsold == 0)
            {
                order.state = E_OrderState.completed;
            }
            else
            {
                order.state = E_OrderState.partial;
            }
        }
        this.db.SaveChanges();
    }

   


}