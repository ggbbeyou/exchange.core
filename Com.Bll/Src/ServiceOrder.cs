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
using System.Linq.Expressions;
using Com.Api.Sdk.Models;
using Microsoft.EntityFrameworkCore;

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
    public Res<List<ResOrder>> PlaceOrder(string symbol, long uid, string user_name, string ip, List<ReqOrder> orders)
    {
        Res<List<ResOrder>> res = new Res<List<ResOrder>>();
        this.stopwatch.Restart();
        FactoryService.instance.constant.stopwatch.Restart();

        res.code = E_Res_Code.fail;
        res.msg = "";
        res.data = new List<ResOrder>();
        if (orders.Max(P => P.client_id?.Length ?? 0) > 50)
        {
            res.code = E_Res_Code.length_too_long;
            res.msg = "client_id长度不能超过50";
            return res;
        }
        if (orders.Any(P => P.type == E_OrderType.limit && (P.price == null || P.price <= 0)))
        {
            res.code = E_Res_Code.limit_price_error;
            res.msg = "price:限价单,交易价不能为低于0";
            return res;
        }
        if (orders.Any(P => P.type == E_OrderType.limit && (P.amount == null || P.amount <= 0)))
        {
            res.code = E_Res_Code.limit_amount_error;
            res.msg = "amount:限价单,交易量不能低于0";
            return res;
        }
        if (orders.Any(P => P.type == E_OrderType.market && P.side == E_OrderSide.buy && (P.total == null || P.total <= 0)))
        {
            res.code = E_Res_Code.market_total_error;
            res.msg = "total:市价买单,交易额不能为低于0";
            return res;
        }
        if (orders.Any(P => P.type == E_OrderType.market && P.side == E_OrderSide.sell && (P.amount == null || P.amount <= 0)))
        {
            res.code = E_Res_Code.market_amount_error;
            res.msg = "amount:市价卖单,交易量不能为低于0";
            return res;
        }
        Market? info = this.service_market.GetMarketBySymbol(symbol);
        if (info == null)
        {
            res.code = E_Res_Code.symbol_not_found;
            res.msg = "未找到该交易对";
            return res;
        }
        if (info.transaction == false || info.status == false)
        {
            res.code = E_Res_Code.system_disable_place_order;
            res.msg = "该交易对禁止下单(系统设置)";
            return res;
        }
        if (info.market_type == E_MarketType.spot && orders.Any(P => P.trade_model != E_TradeModel.cash))
        {
            res.code = E_Res_Code.trade_model_error;
            res.msg = "trade_model:现货交易对必须是现货交易模式";
            return res;
        }
        if (orders.Any(P => P.type == E_OrderType.limit && Math.Round(P.price ?? 0, info.places_price, MidpointRounding.ToNegativeInfinity) != P.price))
        {
            res.code = E_Res_Code.limit_price_error;
            res.msg = $"price:限价单交易价精度错误(交易价小数位:{info.places_price})";
            return res;
        }
        if (orders.Any(P => (P.type == E_OrderType.limit && Math.Round(P.amount ?? 0, info.places_amount, MidpointRounding.ToNegativeInfinity) != P.amount)))
        {
            res.code = E_Res_Code.limit_amount_error;
            res.msg = $"amount:限价单交易量精度错误(交易量小数位:{info.places_amount})";
            return res;
        }
        if (orders.Any(P => (P.type == E_OrderType.market && P.side == E_OrderSide.buy && Math.Round(P.total ?? 0, info.places_price + info.places_amount, MidpointRounding.ToNegativeInfinity) != P.total)))
        {
            res.code = E_Res_Code.market_total_error;
            res.msg = $"total:市价买单交易额精度错误(交易额小数位:{info.places_price + info.places_amount})";
            return res;
        }
        if (orders.Any(P => P.type == E_OrderType.limit && P.price * P.amount < info.trade_min))
        {
            res.code = E_Res_Code.market_amount_error;
            res.msg = $"price * mount:限价单交易额不能低于:{info.trade_min})";
            return res;
        }
        if (orders.Any(P => P.type == E_OrderType.market && P.side == E_OrderSide.buy && P.total < info.trade_min))
        {
            res.code = E_Res_Code.market_total_error;
            res.msg = $"total:市价买单交易额不能低于:{info.trade_min})";
            return res;
        }
        if (orders.Any(P => P.type == E_OrderType.market && P.side == E_OrderSide.sell && P.amount < info.trade_min_market_sell))
        {
            res.code = E_Res_Code.market_amount_error;
            res.msg = $"amount:市价卖单交易量不能低于:{info.trade_min_market_sell})";
            return res;
        }
        Users? users = service_user.GetUser(uid);
        if (users == null)
        {
            res.code = E_Res_Code.user_not_found;
            res.msg = "未找到该用户";
            return res;
        }
        if (users.disabled || !users.transaction)
        {
            res.code = E_Res_Code.user_disable_place_order;
            res.msg = "用户禁止交易";
            return res;
        }
        Vip? vip = service_user.GetVip(users.vip);
        if (vip == null)
        {
            res.code = E_Res_Code.vip_not_found;
            res.msg = "未找到该用户的vip等级信息";
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
                res.code = E_Res_Code.available_not_enough;
                res.msg = "基础币种或报价币种余额不足";
                return res;
            }
        }
        else if (coin_base > 0)
        {
            if (!service_wallet.FreezeChange(wallet_type, uid, info.coin_id_base, coin_base))
            {
                res.code = E_Res_Code.available_not_enough;
                res.msg = "基础币种余额不足";
                return res;
            }
        }
        else if (coin_quote > 0)
        {
            if (!service_wallet.FreezeChange(wallet_type, uid, info.coin_id_quote, coin_quote))
            {
                res.code = E_Res_Code.available_not_enough;
                res.msg = "报价币种余额不足";
                return res;
            }
        }
        FactoryService.instance.constant.stopwatch.Stop();
        FactoryService.instance.constant.logger.LogTrace($"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};{info.symbol}:挂单=>校验/冻结资金{temp_order.Count}条挂单记录");
        FactoryService.instance.constant.stopwatch.Restart();
        int dbcount = 0;
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {
                (List<OrderBuy> buy, List<OrderSell> sell) aaa = ConvertOrder(temp_order);
                db.OrderBuy.AddRange(aaa.buy);
                db.OrderSell.AddRange(aaa.sell);
                dbcount = db.SaveChanges();
            }
        }
        if (dbcount <= 0)
        {
            if (coin_base > 0 && coin_quote > 0)
            {
                if (!service_wallet.FreezeChange(wallet_type, uid, info.coin_id_base, -coin_base, info.coin_id_quote, -coin_quote))
                {
                    res.code = E_Res_Code.db_error;
                    res.msg = "挂单失败,并且解除冻结出错,基础币种或报价币种余额不足";
                    return res;
                }
            }
            else if (coin_base > 0)
            {
                if (!service_wallet.FreezeChange(wallet_type, uid, info.coin_id_base, -coin_base))
                {
                    res.code = E_Res_Code.db_error;
                    res.msg = "挂单失败,并且解除冻结出错,基础币种余额不足";
                    return res;
                }
            }
            else if (coin_quote > 0)
            {
                if (!service_wallet.FreezeChange(wallet_type, uid, info.coin_id_quote, -coin_quote))
                {
                    res.code = E_Res_Code.db_error;
                    res.msg = "挂单失败,并且解除冻结出错,报价币种余额不足";
                    return res;
                }
            }
            res.code = E_Res_Code.db_error;
            res.msg = "挂单失败";
            return res;
        }
        FactoryService.instance.constant.stopwatch.Stop();
        FactoryService.instance.constant.logger.LogTrace($"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};{info.symbol}:挂单({ip})=>插入{dbcount}条订单到DB");
        FactoryService.instance.constant.stopwatch.Restart();
        ReqCall<List<Orders>> call_req = new ReqCall<List<Orders>>();
        call_req.op = E_Op.place;
        call_req.market = info.market;
        call_req.data = temp_order;
        FactoryService.instance.constant.mq_helper.MqSend(FactoryService.instance.GetMqOrderPlace(info.market), Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(call_req)));
        FactoryService.instance.constant.stopwatch.Stop();
        FactoryService.instance.constant.logger.LogTrace($"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};{info.symbol}:挂单=>插入{call_req.data.Count}条订单到Mq");

        res.code = E_Res_Code.ok;
        res.msg = "挂单成功";
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
    public Res<bool> CancelOrder(string symbol, long uid, int type, List<long> order)
    {
        Res<bool> res = new Res<bool>();

        res.code = E_Res_Code.fail;
        res.data = false;
        res.msg = null;
        E_Op op;
        switch (type)
        {
            case 1:
                op = E_Op.cancel_by_all;
                break;
            case 2:
                op = E_Op.cancel_by_uid;
                break;
            case 3:
                op = E_Op.cancel_by_id;
                break;
            case 4:
                op = E_Op.cancel_by_clientid;
                break;
            default:
                return res;
        }
        Market? info = this.service_market.GetMarketBySymbol(symbol);
        if (info == null)
        {
            res.code = E_Res_Code.symbol_not_found;
            res.msg = "未找到该交易对";
            return res;
        }
        if (info.transaction == false || info.status == false)
        {
            res.code = E_Res_Code.system_disable_place_order;
            res.msg = "该交易对禁止下单(系统设置)";
            return res;
        }
        if (uid > 0)
        {
            Users? users = service_user.GetUser(uid);
            if (users == null)
            {
                res.code = E_Res_Code.user_not_found;
                res.msg = "未找到该用户";
                return res;
            }
            if (users.disabled || !users.transaction)
            {
                res.code = E_Res_Code.permission_no;
                res.msg = "用户禁止撤单";
                return res;
            }
        }
        ReqCall<(long, List<long>)> req = new ReqCall<(long, List<long>)>();
        req.op = op;
        req.market = info.market;
        req.data = (uid, order);
        res.data = FactoryService.instance.constant.mq_helper.MqSend(FactoryService.instance.GetMqOrderPlace(info.market), Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
        if (res.data)
        {
            res.code = E_Res_Code.ok;
        }
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
    /// 更新订单
    /// </summary>
    /// <param name="data"></param>
    public bool UpdateOrder1(List<Orders> data)
    {

        
        return false;
    }

    /// <summary>
    /// DB未完成挂单重复推送到mq和redis
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
            FactoryService.instance.constant.mq_helper.MqSend(FactoryService.instance.GetMqOrderPlace(market), Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(call_req)));
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


    /// <summary>
    /// 按用户去查询订单(给api使用)
    /// </summary>
    /// <param name="uid">用户id</param>
    /// <param name="symbol">交易对</param>
    /// <param name="order_id">订单id列表</param>
    /// <param name="state">订单状态</param>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    /// <param name="skip">跳过多少行</param>
    /// <param name="take">提取多少行</param>
    /// <returns></returns>
    public Res<List<ResOrder>> GetOrder(long uid, string? symbol = null, List<long>? order_id = null, List<E_OrderState>? state = null, DateTimeOffset? start = null, DateTimeOffset? end = null, int skip = 0, int take = 50)
    {
        Res<List<ResOrder>> res = new Res<List<ResOrder>>();

        res.code = E_Res_Code.ok;
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {

                var query = (from buy in db.OrderBuy
                             select new ResOrder
                             {
                                 uid = buy.uid,
                                 market = buy.market,
                                 order_id = buy.order_id,
                                 create_time = buy.create_time,
                                 client_id = buy.client_id,
                                 symbol = buy.symbol,
                                 side = buy.side,
                                 type = buy.type,
                                 trade_model = buy.trade_model,
                                 price = buy.price,
                                 amount = buy.amount,
                                 total = buy.total,
                                 trigger_hanging_price = buy.trigger_hanging_price,
                                 trigger_cancel_price = buy.trigger_cancel_price,
                             })
                .Union(from sell in db.OrderSell
                       select new ResOrder
                       {
                           uid = sell.uid,
                           market = sell.market,
                           order_id = sell.order_id,
                           create_time = sell.create_time,
                           client_id = sell.client_id,
                           symbol = sell.symbol,
                           side = sell.side,
                           type = sell.type,
                           trade_model = sell.trade_model,
                           price = sell.price,
                           amount = sell.amount,
                           total = sell.total,
                           trigger_hanging_price = sell.trigger_hanging_price,
                           trigger_cancel_price = sell.trigger_cancel_price,
                       })
                .Where(P => P.uid == uid).WhereIf(symbol != null, P => P.symbol == symbol).WhereIf(order_id != null && order_id.Count > 0, P => order_id!.Contains(P.order_id)).WhereIf(state != null, P => state!.Contains(P.state)).WhereIf(start != null, P => P.create_time >= start).WhereIf(end != null, P => P.create_time <= end)
                .OrderByDescending(P => P.create_time)
                .Skip(skip)
                .Take(take);
                res.data = query.ToList();
            }
        }
        return res;
    }

    /// <summary>
    /// 按交易对来订单查询(给后台使用)
    /// </summary>
    /// <param name="symbol">交易对</param>
    /// <param name="user_name">用户名</param>
    /// <param name="state">订单状态</param>
    /// <param name="order_id">订单id</param>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    /// <param name="skip">跳过多少行</param>
    /// <param name="take">提取多少行</param>
    /// <returns></returns>
    public Res<List<Orders>> GetOrder(string symbol, string? user_name, E_OrderState? state = null, long? order_id = null, DateTimeOffset? start = null, DateTimeOffset? end = null, int skip = 0, int take = 50)
    {
        Res<List<Orders>> res = new Res<List<Orders>>();
        res.code = E_Res_Code.ok;
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
            {

                var query = (from buy in db.OrderBuy
                             select new Orders
                             {
                                 user_name = buy.user_name,
                                 deal_price = buy.deal_price,
                                 deal_amount = buy.deal_amount,
                                 deal_total = buy.deal_total,
                                 unsold = buy.unsold,
                                 complete_thaw = buy.complete_thaw,
                                 fee_maker = buy.fee_maker,
                                 fee_taker = buy.fee_taker,
                                 deal_last_time = buy.deal_last_time,
                                 remarks = buy.remarks,
                                 uid = buy.uid,
                                 state = buy.state,
                                 market = buy.market,
                                 order_id = buy.order_id,
                                 create_time = buy.create_time,
                                 client_id = buy.client_id,
                                 symbol = buy.symbol,
                                 side = buy.side,
                                 type = buy.type,
                                 trade_model = buy.trade_model,
                                 price = buy.price,
                                 amount = buy.amount,
                                 total = buy.total,
                                 trigger_hanging_price = buy.trigger_hanging_price,
                                 trigger_cancel_price = buy.trigger_cancel_price,
                             })
                .Union(from sell in db.OrderSell
                       select new Orders
                       {
                           user_name = sell.user_name,
                           deal_price = sell.deal_price,
                           deal_amount = sell.deal_amount,
                           deal_total = sell.deal_total,
                           unsold = sell.unsold,
                           complete_thaw = sell.complete_thaw,
                           fee_maker = sell.fee_maker,
                           fee_taker = sell.fee_taker,
                           deal_last_time = sell.deal_last_time,
                           remarks = sell.remarks,
                           uid = sell.uid,
                           state = sell.state,
                           market = sell.market,
                           order_id = sell.order_id,
                           create_time = sell.create_time,
                           client_id = sell.client_id,
                           symbol = sell.symbol,
                           side = sell.side,
                           type = sell.type,
                           trade_model = sell.trade_model,
                           price = sell.price,
                           amount = sell.amount,
                           total = sell.total,
                           trigger_hanging_price = sell.trigger_hanging_price,
                           trigger_cancel_price = sell.trigger_cancel_price,
                       })
                .Where(P => P.symbol == symbol)
                .WhereIf(symbol != null, P => P.symbol == symbol).WhereIf(user_name != null, P => P.user_name == user_name).WhereIf(state != null, P => P.state == state).WhereIf(order_id != null, P => P.order_id == order_id).WhereIf(start != null, P => P.create_time >= start).WhereIf(end != null, P => P.create_time <= end)
                .OrderByDescending(P => P.create_time)
                .Skip(skip)
                .Take(take);
                res.data = query.ToList();
                return res;
            }
        }
    }

}