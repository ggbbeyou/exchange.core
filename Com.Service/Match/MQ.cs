using System;
using System.Collections.Generic;
using System.Text;
using Com.Bll;
using Com.Db;
using Com.Api.Sdk.Enum;

using Com.Service.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Snowflake;
using Com.Api.Sdk.Models;
using Com.Bll.Models;

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
    /// 互斥锁
    /// </summary>
    /// <returns></returns>
    private Mutex mutex = new Mutex(false);
    /// <summary>
    /// 上次深度行情
    /// </summary>
    (List<OrderBook> bid, List<OrderBook> ask) orderbook_old;
    /// <summary>
    /// 临时变量
    /// </summary>
    /// <typeparam name="Orders"></typeparam>
    /// <returns></returns>
    private List<Orders> orders = new List<Orders>();
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
    private List<Orders> cancel = new List<Orders>();

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="model">撮合核心</param>
    public MQ(MatchModel model)
    {
        this.model = model;
    }

    /// <summary>
    /// 接收订单列队
    /// </summary>
    /// <returns>队列标识</returns>
    public (string queue_name, string consume_tag) OrderReceive()
    {
        string queue_name = FactoryService.instance.GetMqOrderPlace(this.model.info.market);
        string consume_tag = FactoryService.instance.constant.MqReceive(queue_name, (e) =>
        {
            string json = Encoding.UTF8.GetString(e);
            ReqCall<string>? reqCall = JsonConvert.DeserializeObject<ReqCall<string>>(json);
            if (reqCall != null)
            {
                if (reqCall.op == E_Op.place)
                {
                    ReqCall<List<Orders>>? req = JsonConvert.DeserializeObject<ReqCall<List<Orders>>>(json);
                    if (req != null && req.op == E_Op.place && req.data != null && req.data.Count > 0)
                    {
                        orders.Clear();
                        deal.Clear();
                        cancel.Clear();
                        FactoryService.instance.constant.stopwatch.Restart();
                        foreach (Orders item in req.data)
                        {
                            (List<Orders> orders, List<Deal> deals, List<Orders> cancels) match = this.model.match_core.Match(item);
                            if (match.orders.Count == 0 && match.deals.Count == 0 && match.cancels.Count == 0)
                            {
                                continue;
                            }
                            deal.AddRange(match.deals);
                            foreach (var item1 in match.orders)
                            {
                                if (!orders.Exists(P => P.order_id == item1.order_id))
                                {
                                    orders.Add(item1);
                                }
                            }
                            cancel.AddRange(match.cancels);
                        }
                        FactoryService.instance.constant.stopwatch.Stop();
                        FactoryService.instance.constant.logger.LogTrace(this.model.eventId, $"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};{this.model.eventId.Name}:撮合订单{req.data.Count}条");
                        DepthChange(orders, deal, cancel);
                    };
                }
                else
                {
                    orders.Clear();
                    deal.Clear();
                    cancel.Clear();
                    ReqCall<(long uid, List<long> order_id)>? req = JsonConvert.DeserializeObject<ReqCall<(long, List<long>)>>(json);
                    if (req != null)
                    {
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
                        if (cancel.Count > 0)
                        {
                            DepthChange(orders, deal, cancel);
                        }
                    }
                }
            }
            return true;
        });
        return (queue_name, consume_tag);
    }

    /// <summary>
    /// 深度变更
    /// </summary>
    public void DepthChange(List<Orders> orders, List<Deal> deal, List<Orders> cancel)
    {
        if (orders.Count() > 0 || deal.Count() > 0 || cancel.Count() > 0)
        {
            Processing process = new Processing() { no = FactoryService.instance.constant.worker.NextId(), match = true };
            FactoryService.instance.constant.redis.HashSet(FactoryService.instance.GetRedisProcess(), process.no, JsonConvert.SerializeObject(process));
            FactoryService.instance.constant.MqTask(FactoryService.instance.GetMqOrderDeal(this.model.info.market), Encoding.UTF8.GetBytes(JsonConvert.SerializeObject((process.no, orders, deal, cancel))));
        }
        if (deal.Count() > 0 || cancel.Count > 0)
        {
            FactoryService.instance.constant.stopwatch.Restart();
            (List<OrderBook> bid, List<OrderBook> ask) orderbook = this.model.match_core.GetOrderBook();
            Dictionary<E_WebsockerChannel, ResDepth> depths = ServiceDepth.instance.ConvertDepth(this.model.info.market, this.model.info.symbol, orderbook);
            ServiceDepth.instance.Push(this.model.info.market, depths, true);
            (List<(int index, OrderBook orderbook)> bid, List<(int index, OrderBook orderbook)> ask) diff = ServiceDepth.instance.DiffOrderBook(this.orderbook_old, orderbook);
            Dictionary<E_WebsockerChannel, ResDepth> depths_diff = ServiceDepth.instance.ConvertDepth(this.model.info.market, this.model.info.symbol, diff);
            foreach (var item in depths_diff)
            {
                if (item.Key == E_WebsockerChannel.books10_inc)
                {
                    if (depths.ContainsKey(E_WebsockerChannel.books10))
                    {
                        item.Value.total_bid = depths[E_WebsockerChannel.books10].total_bid;
                        item.Value.total_ask = depths[E_WebsockerChannel.books10].total_ask;
                    }
                }
                else if (item.Key == E_WebsockerChannel.books50_inc)
                {
                    if (depths.ContainsKey(E_WebsockerChannel.books50))
                    {
                        item.Value.total_bid = depths[E_WebsockerChannel.books50].total_bid;
                        item.Value.total_ask = depths[E_WebsockerChannel.books50].total_ask;
                    }
                }
                else if (item.Key == E_WebsockerChannel.books200_inc)
                {
                    if (depths.ContainsKey(E_WebsockerChannel.books200))
                    {
                        item.Value.total_bid = depths[E_WebsockerChannel.books200].total_bid;
                        item.Value.total_ask = depths[E_WebsockerChannel.books200].total_ask;
                    }
                }
            }
            ServiceDepth.instance.Push(this.model.info.market, depths_diff, false);
            this.orderbook_old = orderbook;
            FactoryService.instance.constant.stopwatch.Stop();
            FactoryService.instance.constant.logger.LogTrace(this.model.eventId, $"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};{this.model.eventId.Name}:推送深度行情");
        }
    }

}