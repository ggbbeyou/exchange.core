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
    public Res<List<Orders>> PlaceOrder(long market, List<Orders> order)
    {
        Req<List<Orders>> req = new Req<List<Orders>>();
        req.op = E_Op.place;
        req.market = market;
        req.data = order;
        FactoryService.instance.constant.i_model.BasicPublish(exchange: FactoryService.instance.GetMqOrderPlace(market), routingKey: "", basicProperties: FactoryService.instance.props, body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
        Res<List<Orders>> res = new Res<List<Orders>>();
        res.op = E_Op.place;
        res.success = true;
        res.code = E_Res_Code.ok;
        res.market = market;
        res.message = null;
        res.data = order;
        return res;
    }

}