using Com.Db;
using Com.Api.Sdk.Enum;

using Newtonsoft.Json;
using System.Text;
using RabbitMQ.Client;
using StackExchange.Redis;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Data.Entity;
using System.Linq.Expressions;
using LinqKit;
using Com.Api.Sdk.Models;

namespace Com.Bll;

/// <summary>
/// Service:订单
/// </summary>
public class ServiceOrder
{
    /// <summary>
    /// 秒表
    /// </summary>
    /// <returns></returns>
    private Stopwatch stopwatch = new Stopwatch();
    /// <summary>
    /// 钱包服务
    /// </summary>
    /// <returns></returns>
    private ServiceWallet wallet_service = new ServiceWallet();
    /// <summary>
    /// 交易对服务
    /// </summary>
    /// <returns></returns>
    private ServiceMarket market_service = new ServiceMarket();
    /// <summary>
    /// 用户服务
    /// </summary>
    /// <returns></returns>
    private ServiceUser user_service = new ServiceUser();

    /// <summary>
    /// 初始化
    /// </summary>
    public ServiceOrder()
    {
    }

    /// <summary>
    /// 挂单总入口
    /// </summary>
    /// <param name="symbol">交易对</param>
    /// <param name="uid">用户id</param>
    /// <param name="order">订单列表</param>
    /// <returns></returns>
    public ResCall<List<Orders>> PlaceOrder(string symbol, long uid, List<ReqOrder> orders)
    {
        ResCall<List<Orders>> res = new ResCall<List<Orders>>();
        this.stopwatch.Restart();
        FactoryService.instance.constant.stopwatch.Restart();
        res.success = false;
        res.code = E_Res_Code.fail;
        res.op = E_Op.place;
        res.message = "";
        res.data = new List<Orders>();
        if (orders.Max(P => P.client_id?.Length ?? 0) > 50)
        {
            res.code = E_Res_Code.field_error;
            res.message = "client_id长度不能超过50";
            return res;
        }
        Market? info = this.market_service.GetMarketBySymbol(symbol);
        if (info == null)
        {
            res.code = E_Res_Code.no_symbol;
            res.message = "未找到该交易对";
            return res;
        }
        res.market = info.market;
        List<ReqOrder> market_buy = orders.Where(P => P.side == E_OrderSide.buy && P.type == E_OrderType.price_market).ToList();
        if (market_buy.Exists(P => P.amount == null || P.amount < 0))
        {
            res.code = E_Res_Code.field_error;
            res.message = "市价买单,总额(amount)不能小于0";
            return res;
        }
        if (market_buy.Exists(P => Math.Round(P.amount ?? 0, info.price_places, MidpointRounding.ToNegativeInfinity) != P.amount))
        {
            res.code = E_Res_Code.field_error;
            res.message = $"市价买单,总额(amount)精度不对,总额小数位:{info.price_places}";
            return res;
        }
        List<ReqOrder> market_sell = orders.Where(P => P.side == E_OrderSide.sell && P.type == E_OrderType.price_market).ToList();
        if (market_sell.Exists(P => P.amount == null || P.amount < 0))
        {
            res.code = E_Res_Code.field_error;
            res.message = "市价卖单,量不能小于0";
            return res;
        }
        if (orders.Where(P => P.side == E_OrderSide.buy && P.type == E_OrderType.price_limit).ToList().Exists(P => P.price == null || P.price < 0 || P.amount == null || P.amount <= 0) ||
         orders.Where(P => P.side == E_OrderSide.sell && P.type == E_OrderType.price_limit).ToList().Exists(P => P.price == null || P.price < 0 || P.amount == null || P.amount <= 0))
        {
            res.code = E_Res_Code.field_error;
            res.message = "限价单,价格和量都不能为小于0";
            return res;
        }
        if (orders.Where(P => P.type == E_OrderType.price_limit).ToList().Exists(P => Math.Round(P.price ?? 0, info.price_places, MidpointRounding.ToNegativeInfinity) != P.price))
        {
            res.code = E_Res_Code.field_error;
            res.message = $"限价单价格精度不对,价格小数位:{info.price_places}";
            return res;
        }
        if (orders.Where(P => P.type == E_OrderType.price_limit).ToList().Exists(P => Math.Round((P.amount ?? 0) / info.amount_multiple, 0, MidpointRounding.ToNegativeInfinity) * info.amount_multiple != P.amount))
        {
            res.code = E_Res_Code.field_error;
            res.message = $"限价单量必须是{info.amount_multiple}的整数倍";
            return res;
        }
        if (orders.Where(P => P.side == E_OrderSide.sell).ToList().Exists(P => Math.Round((P.amount ?? 0) / info.amount_multiple, 0, MidpointRounding.ToNegativeInfinity) * info.amount_multiple != P.amount))
        {
            res.code = E_Res_Code.field_error;
            res.message = $"卖单量必须是{info.amount_multiple}的整数倍";
            return res;
        }
        Users? users = user_service.GetUser(uid);
        if (users == null)
        {
            res.code = E_Res_Code.no_user;
            res.message = "未找到该用户";
            return res;
        }
        if (users.disabled || !users.transaction)
        {
            res.code = E_Res_Code.no_permission;
            res.message = "用户禁止下单";
            return res;
        }
        Vip? vip = user_service.GetVip(users.vip);
        if (vip == null)
        {
            vip = new Vip();
        }
        decimal? coin_base = 0;
        decimal? coin_quote = 0;
        decimal? fee_base = 0;
        decimal? fee_quote = 0;
        decimal rate_market_buy = info.fee_market_buy * (1 + vip.fee_market);
        decimal rate_market_sell = info.fee_market_sell * (1 + vip.fee_market);
        decimal rate_limit_buy = info.fee_limit_buy * (1 + vip.fee_limit);
        decimal rate_limit_sell = info.fee_limit_sell * (1 + vip.fee_limit);
        foreach (var item in orders)
        {
            Orders order = new Orders();
            order.order_id = FactoryService.instance.constant.worker.NextId();
            order.client_id = item.client_id;
            order.market = info.market;
            order.symbol = info.symbol;
            order.uid = uid;
            order.side = item.side;
            order.state = E_OrderState.unsold;
            order.type = item.type;
            if (order.type == E_OrderType.price_market)
            {
                order.price = null;
                if (order.side == E_OrderSide.buy)
                {
                    order.amount = null;
                    order.total = null;
                    order.amount_unsold = item.amount ?? 0;
                    order.fee_rate = rate_market_buy;
                    coin_quote += item.amount;
                    fee_quote += rate_market_buy * item.amount;
                }
                else if (order.side == E_OrderSide.sell)
                {
                    order.amount = item.amount;
                    order.total = null;
                    order.amount_unsold = item.amount ?? 0;
                    order.fee_rate = rate_market_sell;
                    coin_base += item.amount;
                    fee_base += rate_market_sell * item.amount;
                }
            }
            else if (order.type == E_OrderType.price_limit)
            {
                order.price = item.price;
                order.amount = item.amount;
                order.total = item.price * item.amount;
                if (order.side == E_OrderSide.buy)
                {
                    order.amount_unsold = order.total ?? 0;
                    order.fee_rate = rate_limit_buy;
                    coin_quote += order.total;
                    fee_quote += rate_limit_buy * order.total;
                }
                else if (order.side == E_OrderSide.sell)
                {
                    order.amount_unsold = item.amount ?? 0;
                    order.fee_rate = rate_limit_sell;
                    coin_base += item.amount;
                    fee_base += rate_limit_sell * item.amount;
                }
            }
            order.amount_done = 0;
            order.trigger_hanging_price = 0;
            order.trigger_cancel_price = 0;
            order.create_time = DateTimeOffset.UtcNow;
            order.deal_last_time = null;
            order.data = null;
            order.remarks = null;
            res.data.Add(order);
        }
        if (coin_base != null && coin_quote != null && coin_base > 0 && coin_quote > 0)
        {
            if (!wallet_service.FreezeChange(E_WalletType.main, uid, info.coin_id_base, coin_base + fee_base ?? 0, info.coin_id_quote, coin_quote + fee_quote ?? 0))
            {
                res.code = E_Res_Code.low_capital;
                res.message = "基础币种或报价币种余额不足";
                return res;
            }
        }
        else if (coin_base != null && coin_base > 0)
        {
            if (!wallet_service.FreezeChange(E_WalletType.main, uid, info.coin_id_base, coin_base + fee_base ?? 0))
            {
                res.code = E_Res_Code.low_capital;
                res.message = "基础币种余额不足";
                return res;
            }
        }
        else if (coin_quote != null && coin_quote > 0)
        {
            if (!wallet_service.FreezeChange(E_WalletType.main, uid, info.coin_id_quote, coin_quote + fee_quote ?? 0))
            {
                res.code = E_Res_Code.low_capital;
                res.message = "报价币种余额不足";
                return res;
            }
        }
        FactoryService.instance.constant.stopwatch.Stop();
        FactoryService.instance.constant.logger.LogTrace($"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};挂单=>校验/冻结资金{res.data.Count}条挂单记录");
        FactoryService.instance.constant.stopwatch.Restart();
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                db.Orders.AddRange(res.data);
                db.SaveChanges();
            }
        }
        FactoryService.instance.constant.stopwatch.Stop();
        FactoryService.instance.constant.logger.LogTrace($"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};挂单=>插入{res.data.Count}条订单到DB");
        FactoryService.instance.constant.stopwatch.Restart();
        ReqCall<List<Orders>> call_req = new ReqCall<List<Orders>>();
        call_req.op = E_Op.place;
        call_req.market = info.market;
        call_req.data = res.data;
        FactoryService.instance.constant.MqSend(FactoryService.instance.GetMqOrderPlace(info.market), Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(call_req)));
        FactoryService.instance.constant.stopwatch.Stop();
        FactoryService.instance.constant.logger.LogTrace($"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};挂单=>插入{call_req.data.Count}条订单到Mq");
        res.op = E_Op.place;
        res.success = true;
        res.code = E_Res_Code.ok;
        res.market = info.market;
        res.message = "挂单成功";
        this.stopwatch.Stop();
        FactoryService.instance.constant.logger.LogTrace($"计算耗时:{this.stopwatch.Elapsed.ToString()};挂单=>总耗时.{call_req.data.Count}条订单");
        return res;
    }

    /// <summary>
    /// 取消挂单总入口
    /// </summary>  
    /// <param name="market">交易对</param>
    /// <param name="uid">用户id</param>
    /// <param name="type">1:按交易对全部撤单,2:按交易对和用户全部撤单,3:按用户和订单id撤单,4:按用户和用户订单id撤单</param>
    /// <param name="order">订单列表</param>
    /// <returns></returns>
    public ResCall<KeyValuePair<long, List<long>>> CancelOrder(long market, long uid, int type, List<long> order)
    {
        ReqCall<KeyValuePair<long, List<long>>> req = new ReqCall<KeyValuePair<long, List<long>>>();
        req.op = E_Op.place;
        req.market = market;
        req.data = new KeyValuePair<long, List<long>>(uid, order);
        FactoryService.instance.constant.MqSend(FactoryService.instance.GetMqOrderPlace(market), Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
        ResCall<KeyValuePair<long, List<long>>> res = new ResCall<KeyValuePair<long, List<long>>>();
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
    public void UpdateOrder(List<Orders> data)
    {
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                db.Orders.UpdateRange(data);
                db.SaveChanges();
            }
        }
    }

    /// <summary>
    /// DB未完成挂单重要推送到mq和redis
    /// </summary>
    /// <param name="market"></param>
    /// <returns></returns>
    public bool PushOrderToMqRedis(long market)
    {
        List<Orders> orders = GetNoCompletedOrders(market);
        if (orders.Count > 0)
        {
            ReqCall<List<Orders>> call_req = new ReqCall<List<Orders>>();
            call_req.op = E_Op.place;
            call_req.market = market;
            call_req.data = orders;
            FactoryService.instance.constant.MqSend(FactoryService.instance.GetMqOrderPlace(market), Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(call_req)));
        }
        return true;
    }

    /// <summary>
    /// 获取未成交订单
    /// </summary>
    /// <param name="market"></param>
    /// <returns></returns>
    private List<Orders> GetNoCompletedOrders(long market)
    {
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                List<Orders> orders = db.Orders.Where(P => P.market == market && (P.state == E_OrderState.unsold || P.state == E_OrderState.partial)).OrderBy(P => P.create_time).ToList();
                return orders;
            }
        }
    }




}