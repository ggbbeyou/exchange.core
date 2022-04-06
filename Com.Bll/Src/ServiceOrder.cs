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
    private ServiceWallet service_wallet = new ServiceWallet();
    /// <summary>
    /// 交易对服务
    /// </summary>
    /// <returns></returns>
    private ServiceMarket service_market = new ServiceMarket();
    /// <summary>
    /// 用户服务
    /// </summary>
    /// <returns></returns>
    private ServiceUser service_user = new ServiceUser();

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
    /// <param name="user_name">用户名</param>
    /// <param name="order">订单列表</param>
    /// <returns></returns>
    public ResCall<List<ResOrder>> PlaceOrder(string symbol, long uid, string user_name, List<ReqOrder> orders)
    {
        ResCall<List<ResOrder>> res = new ResCall<List<ResOrder>>();
        this.stopwatch.Restart();
        FactoryService.instance.constant.stopwatch.Restart();
        res.success = false;
        res.code = E_Res_Code.fail;
        res.op = E_Op.place;
        res.message = "";
        res.data = new List<ResOrder>();
        if (orders.Max(P => P.client_id?.Length ?? 0) > 50)
        {
            res.code = E_Res_Code.field_error;
            res.message = "client_id长度不能超过50";
            return res;
        }
        if (orders.Any(P => P.type == E_OrderType.limit && (P.price == null || P.price <= 0)))
        {
            res.code = E_Res_Code.field_error;
            res.message = "price:限价单,交易价不能为低于0";
            return res;
        }
        if (orders.Any(P => P.type == E_OrderType.limit && (P.amount == null || P.amount <= 0)))
        {
            res.code = E_Res_Code.field_error;
            res.message = "amount:限价单,交易量不能低于0";
            return res;
        }
        if (orders.Any(P => P.type == E_OrderType.market && P.side == E_OrderSide.buy && (P.total == null || P.total <= 0)))
        {
            res.code = E_Res_Code.field_error;
            res.message = "total:市价买单,交易额不能为低于0";
            return res;
        }
        if (orders.Any(P => P.type == E_OrderType.market && P.side == E_OrderSide.sell && (P.amount == null || P.amount <= 0)))
        {
            res.code = E_Res_Code.field_error;
            res.message = "amount:市价卖单,交易量不能为低于0";
            return res;
        }
        Market? info = this.service_market.GetMarketBySymbol(symbol);
        if (info == null)
        {
            res.code = E_Res_Code.no_symbol;
            res.message = "未找到该交易对";
            return res;
        }
        res.market = info.market;
        if (info.transaction == false || info.status == false)
        {
            res.code = E_Res_Code.field_error;
            res.message = "该交易对禁止下单(系统设置)";
            return res;
        }
        if (info.market_type == E_MarketType.spot && orders.Any(P => P.trade_model != E_TradeModel.cash))
        {
            res.code = E_Res_Code.field_error;
            res.message = "trade_model:现货交易对必须是现货交易模式";
            return res;
        }
        if (orders.Any(P => P.type == E_OrderType.limit && Math.Round(P.price ?? 0, info.places_price, MidpointRounding.ToNegativeInfinity) != P.price))
        {
            res.code = E_Res_Code.field_error;
            res.message = $"price:限价单交易价精度错误(交易价小数位:{info.places_price})";
            return res;
        }
        if (orders.Any(P => (P.type == E_OrderType.limit && Math.Round(P.amount ?? 0, info.places_amount, MidpointRounding.ToNegativeInfinity) != P.amount)))
        {
            res.code = E_Res_Code.field_error;
            res.message = $"amount:限价单交易量精度错误(交易量小数位:{info.places_amount})";
            return res;
        }
        if (orders.Any(P => (P.type == E_OrderType.market && P.side == E_OrderSide.buy && Math.Round(P.total ?? 0, info.places_price + info.places_amount, MidpointRounding.ToNegativeInfinity) != P.total)))
        {
            res.code = E_Res_Code.field_error;
            res.message = $"total:市价买单交易额精度错误(交易额小数位:{info.places_price + info.places_amount})";
            return res;
        }
        if (orders.Any(P => P.type == E_OrderType.limit && P.price * P.amount < info.trade_min))
        {
            res.code = E_Res_Code.field_error;
            res.message = $"price * mount:限价单交易额不能低于:{info.trade_min})";
            return res;
        }
        if (orders.Any(P => P.type == E_OrderType.market && P.side == E_OrderSide.buy && P.total < info.trade_min))
        {
            res.code = E_Res_Code.field_error;
            res.message = $"total:市价买单交易额不能低于:{info.trade_min})";
            return res;
        }
        if (orders.Any(P => P.type == E_OrderType.market && P.side == E_OrderSide.sell && P.amount < info.trade_min_market_sell))
        {
            res.code = E_Res_Code.field_error;
            res.message = $"amount:市价卖单交易量不能低于:{info.trade_min_market_sell})";
            return res;
        }
        Users? users = service_user.GetUser(uid);
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
        Vip? vip = service_user.GetVip(users.vip);
        if (vip == null)
        {
            res.code = E_Res_Code.field_error;
            res.message = "未找到该用户的vip等级信息";
            return res;
        }
        decimal coin_base = 0;
        decimal coin_quote = 0;
        List<Orders> temp_order = new List<Orders>();
        foreach (var item in orders)
        {
            Orders order = new Orders();
            order.order_id = FactoryService.instance.constant.worker.NextId();
            order.client_id = item.client_id;
            order.market = info.market;
            order.symbol = info.symbol;
            order.uid = uid;
            order.user_name = user_name;
            order.side = item.side;
            order.state = order.trigger_hanging_price > 0 ? E_OrderState.not_match : E_OrderState.unsold;
            order.type = item.type;
            order.trade_model = item.trade_model;
            order.price = null;
            order.amount = null;
            order.total = null;
            order.deal_price = 0;
            order.deal_amount = 0;
            order.deal_total = 0;
            order.unsold = 0;
            order.fee_maker = vip.fee_maker;
            order.fee_taker = vip.fee_taker;
            if (order.type == E_OrderType.limit)
            {
                order.price = item.price;
                order.amount = item.amount;
                order.total = item.price * item.amount;
                if (order.side == E_OrderSide.buy)
                {
                    order.unsold = order.total ?? 0;
                    coin_quote += order.total ?? 0;
                }
                else if (order.side == E_OrderSide.sell)
                {
                    order.unsold = item.amount ?? 0;
                    coin_base += item.amount ?? 0;
                }
            }
            else if (order.type == E_OrderType.market)
            {
                if (order.side == E_OrderSide.buy)
                {
                    order.total = item.total;
                    order.unsold = item.total ?? 0;
                    coin_quote += item.total ?? 0;
                }
                else if (order.side == E_OrderSide.sell)
                {
                    order.amount = item.amount;
                    order.unsold = item.amount ?? 0;
                    coin_base += item.amount ?? 0;
                }
            }
            order.trigger_hanging_price = item.trigger_hanging_price;
            order.trigger_cancel_price = item.trigger_cancel_price;
            order.create_time = DateTimeOffset.UtcNow;
            order.deal_last_time = null;
            order.remarks = null;
            temp_order.Add(order);
        }
        E_WalletType wallet_type = E_WalletType.main;
        if (info.market_type == E_MarketType.spot)
        {
            wallet_type = E_WalletType.spot;
        }
        if (coin_base > 0 && coin_quote > 0)
        {
            if (!service_wallet.FreezeChange(wallet_type, uid, info.coin_id_base, coin_base, info.coin_id_quote, coin_quote))
            {
                res.code = E_Res_Code.low_capital;
                res.message = "基础币种或报价币种余额不足";
                return res;
            }
        }
        else if (coin_base > 0)
        {
            if (!service_wallet.FreezeChange(wallet_type, uid, info.coin_id_base, coin_base))
            {
                res.code = E_Res_Code.low_capital;
                res.message = "基础币种余额不足";
                return res;
            }
        }
        else if (coin_quote > 0)
        {
            if (!service_wallet.FreezeChange(wallet_type, uid, info.coin_id_quote, coin_quote))
            {
                res.code = E_Res_Code.low_capital;
                res.message = "报价币种余额不足";
                return res;
            }
        }
        FactoryService.instance.constant.stopwatch.Stop();
        FactoryService.instance.constant.logger.LogTrace($"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};{info.symbol}:挂单=>校验/冻结资金{temp_order.Count}条挂单记录");
        FactoryService.instance.constant.stopwatch.Restart();
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                (List<OrderBuy> buy, List<OrderSell> sell) aaa = ConvertOrder(temp_order);
                db.OrderBuy.AddRange(aaa.buy);
                db.OrderSell.AddRange(aaa.sell);
                db.SaveChanges();
            }
        }
        FactoryService.instance.constant.stopwatch.Stop();
        FactoryService.instance.constant.logger.LogTrace($"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};{info.symbol}:挂单=>插入{temp_order.Count}条订单到DB");
        FactoryService.instance.constant.stopwatch.Restart();
        ReqCall<List<Orders>> call_req = new ReqCall<List<Orders>>();
        call_req.op = E_Op.place;
        call_req.market = info.market;
        call_req.data = temp_order;
        FactoryService.instance.constant.MqSend(FactoryService.instance.GetMqOrderPlace(info.market), Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(call_req)));
        FactoryService.instance.constant.stopwatch.Stop();
        FactoryService.instance.constant.logger.LogTrace($"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};{info.symbol}:挂单=>插入{call_req.data.Count}条订单到Mq");
        res.op = E_Op.place;
        res.success = true;
        res.code = E_Res_Code.ok;
        res.market = info.market;
        res.message = "挂单成功";
        res.data.AddRange(temp_order);
        this.stopwatch.Stop();
        FactoryService.instance.constant.logger.LogTrace($"计算耗时:{this.stopwatch.Elapsed.ToString()};{info.symbol}:挂单=>总耗时.{call_req.data.Count}条订单");
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
    public bool UpdateOrder(List<Orders> data)
    {
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                try
                {
                    (List<OrderBuy> buy, List<OrderSell> sell) orders = ConvertOrder(data);
                    db.OrderBuy.UpdateRange(orders.buy);
                    db.OrderSell.UpdateRange(orders.sell);
                    db.SaveChanges();
                }
                catch (System.Exception ex)
                {
                    FactoryService.instance.constant.logger.LogError($"更新订单失败:{ex.Message}");
                    return false;
                }
                return true;
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
                List<Orders> orders = new List<Orders>();
                orders.AddRange(db.OrderBuy.Where(P => P.side == E_OrderSide.buy && P.market == market && (P.state == E_OrderState.unsold || P.state == E_OrderState.partial)).AsNoTracking().ToList());
                orders.AddRange(db.OrderSell.Where(P => P.side == E_OrderSide.sell && P.market == market && (P.state == E_OrderState.unsold || P.state == E_OrderState.partial)).AsNoTracking().ToList());
                return orders.OrderBy(P => P.create_time).ToList();
            }
        }
    }

    /// <summary>
    /// 类型转换
    /// </summary>
    /// <param name="orders"></param>
    /// <returns></returns>
    public (List<OrderBuy> buy, List<OrderSell> sell) ConvertOrder(List<Orders> orders)
    {
        List<OrderBuy> buys = new List<OrderBuy>();
        foreach (var item in orders.Where(P => P.side == E_OrderSide.buy))
        {
            buys.Add(new OrderBuy()
            {
                order_id = item.order_id,
                client_id = item.client_id,
                market = item.market,
                symbol = item.symbol,
                uid = item.uid,
                user_name = item.user_name,
                side = item.side,
                state = item.state,
                type = item.type,
                trade_model = item.trade_model,
                price = item.price,
                amount = item.amount,
                total = item.total,
                deal_price = item.deal_price,
                deal_amount = item.deal_amount,
                deal_total = item.deal_total,
                unsold = item.unsold,
                complete_thaw = item.complete_thaw,
                fee_maker = item.fee_maker,
                fee_taker = item.fee_taker,
                trigger_hanging_price = item.trigger_hanging_price,
                trigger_cancel_price = item.trigger_cancel_price,
                create_time = item.create_time,
                deal_last_time = item.deal_last_time,
                remarks = item.remarks,
            });
        }
        List<OrderSell> sells = new List<OrderSell>();
        foreach (var item in orders.Where(P => P.side == E_OrderSide.sell))
        {
            sells.Add(new OrderSell()
            {
                order_id = item.order_id,
                client_id = item.client_id,
                market = item.market,
                symbol = item.symbol,
                uid = item.uid,
                user_name = item.user_name,
                side = item.side,
                state = item.state,
                type = item.type,
                trade_model = item.trade_model,
                price = item.price,
                amount = item.amount,
                total = item.total,
                deal_price = item.deal_price,
                deal_amount = item.deal_amount,
                deal_total = item.deal_total,
                unsold = item.unsold,
                complete_thaw = item.complete_thaw,
                fee_maker = item.fee_maker,
                fee_taker = item.fee_taker,
                trigger_hanging_price = item.trigger_hanging_price,
                trigger_cancel_price = item.trigger_cancel_price,
                create_time = item.create_time,
                deal_last_time = item.deal_last_time,
                remarks = item.remarks,
            });
        }
        return (buys, sells);
    }

}