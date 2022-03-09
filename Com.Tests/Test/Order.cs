using Com.Bll;
using Com.Common;
using Com.Model;
using Com.Model.Enum;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Snowflake;

namespace Com.Tests;

/// <summary>
/// 网站后台服务
/// </summary>
public class Order
{

    /// <summary>
    /// 单例类的实例
    /// </summary>
    /// <returns></returns>
    public static readonly Order instance = new Order();
    /// <summary>
    /// 雪花算法
    /// </summary>
    /// <returns></returns>
    public readonly IdWorker worker = new IdWorker(1, 1);
    public readonly Random random = new Random();


    public void PlaceOrder()
    {
        string market = "btc/usdt";
        List<MatchOrder> orders = new List<MatchOrder>();
        for (int i = 0; i < 10; i++)
        {
            MatchOrder order = new MatchOrder();
            MatchOrder orderResult = new MatchOrder();
            orderResult.order_id = worker.NextId();
            orderResult.client_id = null;
            orderResult.market = market;
            orderResult.uid = 1;
            orderResult.price = (decimal)random.NextDouble();
            orderResult.amount = (decimal)random.NextDouble();
            orderResult.total = orderResult.price * orderResult.amount;
            orderResult.create_time = DateTimeOffset.UtcNow;
            orderResult.amount_unsold = 0;
            orderResult.amount_done = orderResult.amount;
            orderResult.deal_last_time = null;
            orderResult.side = i % 2 == 0 ? E_OrderSide.buy : E_OrderSide.sell;
            orderResult.state = E_OrderState.unsold;
            orderResult.type = i % 2 == 0 ? E_OrderType.price_fixed : E_OrderType.price_market;
            orderResult.data = null;
            orderResult.remarks = null;
            orders.Add(order);
        }
        Res<List<MatchOrder>> res = OrderService.instance.PlaceOrder(market, orders);
    }

}