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
    /// 挂单总入口
    /// </summary>
    /// <param name="market">交易对</param>
    /// <param name="uid">用户id</param>
    /// <param name="order">订单列表</param>
    /// <returns></returns>
    public Res<List<MatchOrder>> PlaceOrder(string market, List<MatchOrder> order)
    {
        Req<List<MatchOrder>> req = new Req<List<MatchOrder>>();
        req.op = E_Op.place;
        req.market = market;
        req.data = order;
        FactoryService.instance.constant.i_model.BasicPublish(exchange: FactoryService.instance.GetMqOrderPlace(market), routingKey: "", basicProperties: FactoryService.instance.props, body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
        Res<List<MatchOrder>> res = new Res<List<MatchOrder>>();
        res.op = E_Op.place;
        res.success = true;
        res.code = E_Res_Code.ok;
        res.market = market;
        res.message = null;
        res.data = order;
        return res;
    }

}