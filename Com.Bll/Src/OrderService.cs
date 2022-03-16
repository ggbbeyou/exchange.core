using Com.Db;
using Com.Db.Enum;
using Com.Db.Model;
using Newtonsoft.Json;
using System.Text;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace Com.Bll;

/// <summary>
/// Service:订单
/// </summary>
public class OrderService
{
    /// <summary>
    /// 挂单总入口
    /// </summary>
    /// <param name="market">交易对</param>
    /// <param name="uid">用户id</param>
    /// <param name="order">订单列表</param>
    /// <returns></returns>
    public CallResponse<List<Orders>> PlaceOrder(long market, List<Orders> order)
    {
        CallRequest<List<Orders>> req = new CallRequest<List<Orders>>();
        req.op = E_Op.place;
        req.market = market;
        req.data = order;
        FactoryService.instance.constant.MqSend(FactoryService.instance.GetMqOrderPlace(market), Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
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



}