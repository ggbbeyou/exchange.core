using System.Linq.Expressions;
using Com.Db;
using Com.Db.Enum;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Com.Bll;

/// <summary>
/// 订单:交易记录
/// </summary>
public class OrdersDb
{
    /// <summary>
    /// 数据库
    /// </summary>
    public DbContextEF db = null!;

    /// <summary>
    /// 初始化
    /// </summary>
    public OrdersDb()
    {
        var scope = FactoryService.instance.constant.provider.CreateScope();
        this.db = scope.ServiceProvider.GetService<DbContextEF>()!;
    }

    /// <summary>
    /// 获取交易记录
    /// </summary>
    /// <param name="market">交易对</param>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    /// <returns></returns>
    // public List<Deal> GetDeals(long market, DateTimeOffset? start, DateTimeOffset? end)
    // {
    //     Expression<Func<Deal, bool>> predicate = P => P.market == market;
    //     if (start != null)
    //     {
    //         predicate = predicate.And(P => start <= P.time);
    //     }
    //     if (end != null)
    //     {
    //         predicate = predicate.And(P => P.time <= end);
    //     }
    //     return this.db.Deal.Where(predicate).OrderBy(P => P.time).ToList();
    // }



    /// <summary>
    /// 添加或保存
    /// </summary>
    /// <param name="deals"></param>
    public int AddOrUpdateOrder(List<Orders> deals)
    {
        List<Orders> temp = this.db.Orders.Where(P => deals.Select(Q => Q.order_id).Contains(P.order_id)).ToList();
        foreach (var deal in deals)
        {
            var temp_deal = temp.FirstOrDefault(P => P.order_id == deal.order_id);
            if (temp_deal != null)
            {
                temp_deal.amount_unsold = deal.amount_unsold;
                temp_deal.amount_done = deal.amount_done;
                temp_deal.deal_last_time = deal.deal_last_time;
                temp_deal.state = deal.state;
                temp_deal.trigger_cancel_price = deal.trigger_cancel_price;
            }
            else
            {
                this.db.Orders.Add(deal);
            }
        }
        return this.db.SaveChanges();
    }

}