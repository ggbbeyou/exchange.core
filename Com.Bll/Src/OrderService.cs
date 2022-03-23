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
            return res;
        }
        res.market = info.market;
        Users? users = user_service.GetUser(uid);
        if (users == null)
        {
            res.code = E_Res_Code.no_user;
            return res;
        }
        if (users.disabled || !users.transaction)
        {
            res.code = E_Res_Code.no_permission;
            return res;
        }
        Vip? vip = user_service.GetVip(users.vip);
        if (vip == null)
        {
            vip = new Vip();
        }
        List<PlaceOrder> buy_market = orders.Where(P => P.side == E_OrderSide.buy && P.type == E_OrderType.price_market).ToList();
        List<PlaceOrder> buy_limit = orders.Where(P => P.side == E_OrderSide.buy && P.type == E_OrderType.price_fixed).ToList();
        List<PlaceOrder> sell_market = orders.Where(P => P.side == E_OrderSide.sell && P.type == E_OrderType.price_market).ToList();
        List<PlaceOrder> sell_limit = orders.Where(P => P.side == E_OrderSide.sell && P.type == E_OrderType.price_fixed).ToList();
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
            res.message = "限价单,价格和量不能为小于0";
            return res;
        }
        decimal rate_buy_market = 0;
        decimal rate_sell_market = 0;
        decimal rate_buy_limit = 0;
        decimal rate_sell_limit = 0;





        decimal freeze_base = orders.Where(P => P.side == E_OrderSide.sell).Sum(P => (decimal?)P.amount) ?? 0;
        decimal freeze_quote = buy_limit.Sum(P => (decimal?)P.price * P.amount) ?? 0 + buy_market.Sum(P => P.total) ?? 0;



        foreach (var item in orders.Where(P => P.side == E_OrderSide.buy && P.type == E_OrderType.price_market))
        {
            Orders orderResult = new Orders();
            orderResult.order_id = FactoryService.instance.constant.worker.NextId();
            orderResult.market = info.market;
            orderResult.symbol = info.symbol;
            orderResult.client_id = item.client_id;
            orderResult.uid = uid;
            orderResult.price = item.price ?? 0;
            orderResult.amount = item.amount;
            orderResult.total = item.price ?? 0 * item.amount;
            orderResult.create_time = DateTimeOffset.UtcNow;
            orderResult.amount_unsold = item.amount;
            orderResult.amount_done = 0;
            orderResult.deal_last_time = null;
            orderResult.side = item.side;
            orderResult.state = E_OrderState.unsold;
            orderResult.type = item.type;
            orderResult.fee_rate = info.rate_market_buy * (1 + vip.rate_market);
            orderResult.trigger_hanging_price = 0;
            orderResult.trigger_cancel_price = 0;
            orderResult.data = null;
            orderResult.remarks = null;
            res.data.Add(orderResult);
            freeze_quote +=
        }
        foreach (var item in orders.Where(P => P.side == E_OrderSide.buy && P.type == E_OrderType.price_fixed))
        {

        }
        foreach (var item in orders.Where(P => P.side == E_OrderSide.sell && P.type == E_OrderType.price_market))
        {

        }
        foreach (var item in orders.Where(P => P.side == E_OrderSide.sell && P.type == E_OrderType.price_fixed))
        {

        }

        freeze_quote += orders.Sum(P => P.amount);



        FactoryService.instance.constant.stopwatch.Restart();
        db.Orders.AddRange(order);
        db.SaveChanges();
        FactoryService.instance.constant.stopwatch.Stop();
        FactoryService.instance.constant.logger.LogTrace($"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};插入{order.Count}条订单到DB");

        req.op = E_Op.place;
        req.market = info.market;
        req.data = order;
        FactoryService.instance.constant.stopwatch.Restart();
        FactoryService.instance.constant.MqSend(FactoryService.instance.GetMqOrderPlace(info.market), Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
        FactoryService.instance.constant.stopwatch.Stop();
        FactoryService.instance.constant.logger.LogTrace($"计算耗时:{FactoryService.instance.constant.stopwatch.Elapsed.ToString()};插入{order.Count}条订单到Mq");
        CallResponse<List<Orders>> res = new CallResponse<List<Orders>>();
        res.op = E_Op.place;
        res.success = true;
        res.code = E_Res_Code.ok;
        res.market = info.market;
        res.message = null;
        res.data = order;
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