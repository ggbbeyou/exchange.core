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
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Data.Entity;
using System.Linq.Expressions;
using LinqKit;
using om.Api.Sdk.Models;

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
    /// 钱包服务
    /// </summary>
    /// <returns></returns>
    public WalletService wallet_service = new WalletService();
    /// <summary>
    /// 交易对服务
    /// </summary>
    /// <returns></returns>
    public MarketInfoService market_service = new MarketInfoService();
    /// <summary>
    /// 用户服务
    /// </summary>
    /// <returns></returns>
    public UserService user_service = new UserService();

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
    /// <param name="symbol">交易对</param>
    /// <param name="uid">用户id</param>
    /// <param name="order">订单列表</param>
    /// <returns></returns>
    public CallResponse<List<Orders>> PlaceOrder(string symbol, long uid, List<PlaceOrder> orders)
    {
        CallResponse<List<Orders>> res = new CallResponse<List<Orders>>();
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
        MarketInfo? info = this.market_service.GetMarketBySymbol(symbol);
        if (info == null)
        {
            res.code = E_Res_Code.no_symbol;
            res.message = "未找到该交易对";
            return res;
        }
        res.market = info.market;
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
        List<PlaceOrder> buy_market = orders.Where(P => P.side == E_OrderSide.buy && P.type == E_OrderType.price_market).ToList();
        List<PlaceOrder> buy_limit = orders.Where(P => P.side == E_OrderSide.buy && P.type == E_OrderType.price_limit).ToList();
        List<PlaceOrder> sell_market = orders.Where(P => P.side == E_OrderSide.sell && P.type == E_OrderType.price_market).ToList();
        List<PlaceOrder> sell_limit = orders.Where(P => P.side == E_OrderSide.sell && P.type == E_OrderType.price_limit).ToList();
        if (buy_market.Exists(P => P.total == null || P.total < 0))
        {
            res.code = E_Res_Code.field_error;
            res.message = "市价买单,总额不能小于0";
            return res;
        }
        if (sell_market.Exists(P => P.amount == null || P.amount < 0))
        {
            res.code = E_Res_Code.field_error;
            res.message = "市价卖单,量不能小于0";
            return res;
        }
        if (buy_limit.Exists(P => P.price == null || P.price < 0 || P.amount == null || P.amount <= 0) || sell_limit.Exists(P => P.price == null || P.price < 0 || P.amount == null || P.amount <= 0))
        {
            res.code = E_Res_Code.field_error;
            res.message = "限价单,价格和量都不能为小于0";
            return res;
        }
        decimal coin_base = 0;
        decimal coin_quote = 0;
        decimal fee_base = 0;
        decimal fee_quote = 0;
        decimal rate_market_buy = info.rate_market_buy * (1 + vip.rate_market);
        decimal rate_market_sell = info.rate_market_sell * (1 + vip.rate_market);
        decimal rate_limit_buy = info.fee_limit_buy * (1 + vip.rate_limit);
        decimal rate_limit_sell = info.fee_limit_sell * (1 + vip.rate_limit);
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
                order.price = 0;
                if (order.side == E_OrderSide.buy)
                {
                    order.amount = 0;
                    order.total = item.total ?? 0;
                    order.amount_unsold = item.total ?? 0;
                    order.fee_rate = rate_market_buy;
                    coin_quote += order.total;
                    fee_quote += order.total * order.fee_rate;
                }
                else if (order.side == E_OrderSide.sell)
                {
                    order.amount = item.amount ?? 0;
                    order.total = 0;
                    order.amount_unsold = item.amount ?? 0;
                    order.fee_rate = rate_market_sell;
                    coin_base += order.amount;
                    fee_base += order.amount * order.fee_rate;
                }
            }
            else if (order.type == E_OrderType.price_limit)
            {
                order.price = item.price ?? 0;
                order.amount = item.amount ?? 0;
                order.total = order.price * order.amount;
                order.amount_unsold = order.amount;
                if (order.side == E_OrderSide.buy)
                {
                    order.fee_rate = rate_limit_buy;
                    coin_quote += order.total;
                    fee_quote += order.total * order.fee_rate;
                }
                else if (order.side == E_OrderSide.sell)
                {
                    order.fee_rate = rate_limit_sell;
                    coin_base += order.amount;
                    fee_base += order.amount * order.fee_rate;
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
        if (coin_base > 0 && coin_quote > 0)
        {
            if (!wallet_service.FreezeChange(E_WalletType.main, uid, info.coin_id_base, coin_base + fee_base, info.coin_id_quote, coin_quote + fee_quote))
            {
                res.code = E_Res_Code.low_capital;
                res.message = "基础币种余额不足";
                return res;
            }
        }
        else if (coin_base > 0)
        {
            if (!wallet_service.FreezeChange(E_WalletType.main, uid, info.coin_id_base, coin_base + fee_base))
            {
                res.code = E_Res_Code.low_capital;
                res.message = "基础币种余额不足";
                return res;
            }
        }
        else if (coin_quote > 0)
        {
            if (!wallet_service.FreezeChange(E_WalletType.main, uid, info.coin_id_quote, coin_quote + fee_quote))
            {
                res.code = E_Res_Code.low_capital;
                res.message = "报价币种余额不足";
                return res;
            }
        }
        FactoryService.instance.constant.stopwatch.Stop();
        FactoryService.instance.constant.logger.LogTrace($"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};校验/冻结资金{res.data.Count}条挂单记录");
        FactoryService.instance.constant.stopwatch.Restart();
        db.Orders.AddRange(res.data);
        db.SaveChanges();
        FactoryService.instance.constant.stopwatch.Stop();
        FactoryService.instance.constant.logger.LogTrace($"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};插入{res.data.Count}条订单到DB");
        FactoryService.instance.constant.stopwatch.Restart();
        CallRequest<List<Orders>> call_req = new CallRequest<List<Orders>>();
        call_req.op = E_Op.place;
        call_req.market = info.market;
        call_req.data = res.data;
        FactoryService.instance.constant.MqSend(FactoryService.instance.GetMqOrderPlace(info.market), Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(call_req)));
        FactoryService.instance.constant.stopwatch.Stop();
        FactoryService.instance.constant.logger.LogTrace($"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};插入{call_req.data.Count}条订单到Mq");
        res.op = E_Op.place;
        res.success = true;
        res.code = E_Res_Code.ok;
        res.market = info.market;
        res.message = "挂单成功";
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
    public void UpdateOrder(List<Orders> data)
    {
        using (var scope = FactoryService.instance.constant.provider.CreateScope())
        {
            DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!;
            db.Orders.UpdateRange(data);
            db.SaveChanges();
        }
    }




}