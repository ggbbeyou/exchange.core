using Com.Common;
using Com.Db;
using Com.Model;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Com.Bll;

/// <summary>
/// 订单服务
/// </summary>
public class OrderService
{
    /// <summary>
    /// 单例类的实例
    /// </summary>
    /// <returns></returns>
    public static readonly OrderService instance = new OrderService();
    /// <summary>
    /// 常用接口
    /// </summary>
    private FactoryConstant constant = null!;

    /// <summary>
    /// private构造方法
    /// </summary>
    private OrderService()
    {

    }

    /// <summary>
    /// 初始化方法
    /// </summary>
    /// <param name="constant"></param>
    public void Init(FactoryConstant constant)
    {
        this.constant = constant;

    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="market"></param>
    /// <param name="uid"></param>
    /// <param name="order"></param>
    /// <returns></returns>
    public async Task<List<BaseOrder>> PlaceOrder(string market, long uid, List<BaseOrder> order)
    {
        List<BaseOrder> result = new List<BaseOrder>();
        foreach (var item in order)
        {
            BaseOrder orderResult = new BaseOrder();
            // orderResult.order_id = item.Id;
            // orderResult.Uid = item.Uid;
            // orderResult.Market = item.Market;
            // orderResult.Price = item.Price;
            // orderResult.Amount = item.Amount;
            // orderResult.Type = item.Type;
            // orderResult.Status = OrderStatus.Failed;
            // orderResult.CreateTime = DateTime.Now;
            // orderResult.UpdateTime = DateTime.Now;
            result.Add(orderResult);
        }
        return result;
    }

}