using System;
using System.Collections.Generic;
using System.Text;
using Com.Bll;
using Com.Db;
using Com.Db.Enum;
using Com.Db.Model;
using Com.Service.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Snowflake;

namespace Com.Service.Match;

/// <summary>
/// RabbitMQ 接收数据和发送数据
/// </summary>
public class MQ
{
    /// <summary>
    /// 撮合服务对象
    /// </summary>
    private MatchModel model;
    /// <summary>
    /// 接收挂单订单队列标记
    /// </summary>
    /// <value></value>
    public string? consumerTags_order_send;
    /// <summary>
    /// 接收撤单订单队列标记
    /// </summary>
    /// <value></value>
    public string? consumerTags_order_cancel;

    /// <summary>
    /// MQ基本属性
    /// </summary>
    /// <returns></returns>
    private IBasicProperties props = FactoryService.instance.constant.i_model.CreateBasicProperties();
    /// <summary>
    /// 互斥锁
    /// </summary>
    /// <returns></returns>
    private Mutex mutex = new Mutex(false);
    /// <summary>
    /// 临时变量
    /// </summary>
    /// <typeparam name="MatchDeal"></typeparam>
    /// <returns></returns>
    private List<Deal> deal = new List<Deal>();
    /// <summary>
    /// 临时变量
    /// </summary>
    /// <typeparam name="MatchOrder"></typeparam>
    /// <returns></returns>
    private List<long> cancel_deal = new List<long>();
    /// <summary>
    /// 临时变量
    /// </summary>
    /// <typeparam name="MatchOrder"></typeparam>
    /// <returns></returns>
    private List<Orders> cancel = new List<Orders>();
    private DepthService depth_service = new DepthService();

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="model">撮合核心</param>
    public MQ(MatchModel model)
    {
        this.model = model;
        props.DeliveryMode = 2;
        OrderCancel();
        OrderReceive();
    }

    /// <summary>
    /// 接收订单列队
    /// </summary>
    public void OrderReceive()
    {
        this.consumerTags_order_send = FactoryService.instance.constant.MqReceive(FactoryService.instance.GetMqOrderPlace(this.model.info.market), (e) =>
        {
            if (!this.model.run)
            {
                Thread.Sleep(1000);
                return false;
            }
            else
            {
                string json = Encoding.UTF8.GetString(e);
                CallRequest<List<Orders>>? req = JsonConvert.DeserializeObject<CallRequest<List<Orders>>>(json);
                if (req != null && req.op == E_Op.place && req.data != null && req.data.Count > 0)
                {
                    deal.Clear();
                    cancel_deal.Clear();
                    foreach (Orders item in req.data)
                    {
                        this.mutex.WaitOne();
                        List<Deal> match = this.model.match_core.Match(item);
                        if (match.Count > 0)
                        {
                            cancel_deal.AddRange(this.model.match_core.CancelOrder(match.Last().price));
                        }
                        this.mutex.ReleaseMutex();
                        deal.AddRange(match);
                    }
                    if (deal.Count() > 0)
                    {
                        FactoryService.instance.constant.MqTask(FactoryService.instance.GetMqOrderDeal(this.model.info.market), Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(deal)));
                    }
                    if (cancel_deal.Count > 0)
                    {
                        FactoryService.instance.constant.MqTask(FactoryService.instance.GetMqOrderCancelSuccess(this.model.info.market), Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(cancel_deal)));
                    }
                    //增加未成交的订单更新到OrderBook
                    List<(BaseOrderBook depth, string json)> depth = new List<(BaseOrderBook depth, string json)>();
                    var bids = from bid in req.data
                               where bid.side == E_OrderSide.buy && bid.type == E_OrderType.price_fixed && bid.amount_unsold > 0 && (bid.state == E_OrderState.partial || bid.state == E_OrderState.unsold)
                               group bid by new { bid.market, bid.symbol, bid.price } into g
                               select new { market = g.Key.market, symbol = g.Key.symbol, price = g.Key.price, amount_unsold = g.Sum(x => x.amount_unsold), count = g.Count(), time = g.Max(x => x.create_time) };
                    foreach (var item in bids)
                    {
                        depth.Add(depth_service.UpdateOrderBook(item.market, item.symbol, E_OrderSide.buy, item.price, item.amount_unsold, item.count, item.time)!.Value);
                    }
                    var asks = from ask in req.data
                               where ask.side == E_OrderSide.sell && ask.type == E_OrderType.price_fixed && ask.amount_unsold > 0 && (ask.state == E_OrderState.partial || ask.state == E_OrderState.unsold)
                               group ask by new { ask.market, ask.symbol, ask.price } into g
                               select new { market = g.Key.market, symbol = g.Key.symbol, price = g.Key.price, amount_unsold = g.Sum(x => x.amount_unsold), count = g.Count(), time = g.Max(x => x.create_time) };
                    foreach (var item in asks)
                    {
                        depth.Add(depth_service.UpdateOrderBook(item.market, item.symbol, E_OrderSide.sell, item.price, item.amount_unsold, item.count, item.time)!.Value);
                    }
                    depth_service.Push(depth);
                };
                return true;
            }
        });
    }

    /// <summary>
    /// 取消订单列队
    /// </summary>
    public void OrderCancel()
    {
        this.consumerTags_order_cancel = FactoryService.instance.constant.MqReceive(FactoryService.instance.GetMqOrderCancel(this.model.info.market), (e) =>
        {
            if (!this.model.run)
            {
                Thread.Sleep(1000);
                return false;
            }
            else
            {
                string json = Encoding.UTF8.GetString(e);
                CallRequest<(long uid, List<long> order_id)>? req = JsonConvert.DeserializeObject<CallRequest<(long, List<long>)>>(json);
                if (req != null && req.op == E_Op.place)
                {
                    cancel.Clear();
                    this.mutex.WaitOne();
                    if (req.op == E_Op.cancel_by_id)
                    {
                        cancel.AddRange(this.model.match_core.CancelOrder(req.data.uid, req.data.order_id));
                    }
                    else if (req.op == E_Op.cancel_by_uid)
                    {
                        cancel.AddRange(this.model.match_core.CancelOrder(req.data.uid));
                    }
                    else if (req.op == E_Op.cancel_by_clientid)
                    {
                        cancel.AddRange(this.model.match_core.CancelOrder(req.data.uid, req.data.order_id));
                    }
                    else if (req.op == E_Op.cancel_by_all)
                    {
                        cancel.AddRange(this.model.match_core.CancelOrder());
                    }
                    this.mutex.ReleaseMutex();
                    if (cancel.Count > 0)
                    {
                        FactoryService.instance.constant.MqTask(FactoryService.instance.GetMqOrderCancelSuccess(this.model.info.market), Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(cancel)));
                    }
                }
                return true;
            }
        });
    }

}