using System;
using System.Collections.Generic;
using System.Text;
using Com.Bll;
using Com.Db;
using Com.Db.Enum;
using Com.Db.Model;
using Com.Service.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
                Thread.Sleep(1000 * 10);
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
                    FactoryService.instance.constant.stopwatch.Restart();
                    foreach (Orders item in req.data)
                    {
                        this.mutex.WaitOne();
                        List<Deal> match = this.model.match_core.Match(item);
                        deal.AddRange(match);
                        if (match.Count > 0)
                        {
                            cancel_deal.AddRange(this.model.match_core.CancelOrder(match.Last().price));
                        }
                        this.mutex.ReleaseMutex();
                    }
                    FactoryService.instance.constant.stopwatch.Stop();
                    FactoryService.instance.constant.logger.LogTrace($"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};撮合订单{req.data.Count}条");
                    if (deal.Count() > 0)
                    {
                        FactoryService.instance.constant.MqTask(FactoryService.instance.GetMqOrderDeal(this.model.info.market), Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(deal)));
                    }
                    if (cancel_deal.Count > 0)
                    {
                        FactoryService.instance.constant.MqTask(FactoryService.instance.GetMqOrderCancelSuccess(this.model.info.market), Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(cancel_deal)));
                    }
                    if (deal.Count() > 0 || cancel_deal.Count > 0)
                    {
                        FactoryService.instance.constant.stopwatch.Restart();
                        (List<BaseOrderBook> bid, List<BaseOrderBook> ask) orderbook = this.model.match_core.GetOrderBook();
                        Dictionary<E_WebsockerChannel, Depth> depths = depth_service.ConvertDepth(this.model.info.market, this.model.info.symbol, orderbook);
                        depth_service.Push(depths);
                        FactoryService.instance.constant.stopwatch.Stop();
                        FactoryService.instance.constant.logger.LogTrace($"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};推送深度行情");
                    }
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
                Thread.Sleep(1000 * 10);
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
                        // (List<BaseOrderBook> bid, List<BaseOrderBook> ask) orderbook = this.model.match_core.GetOrderBook();
                        // depth_service.ConvertDepth(orderbook);
                    }
                }
                return true;
            }
        });
    }

}