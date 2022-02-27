using System.Linq.Expressions;
using Com.Common;
using Com.Db;
using Com.Model;
using Com.Model.Enum;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace Com.Bll;
public class DealHelper
{
    /// <summary>
    /// 常用接口
    /// </summary>
    public FactoryConstant constant = null!;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="constant"></param>
    public DealHelper(FactoryConstant constant)
    {
        this.constant = constant;
    }

    /// <summary>
    /// 获取交易记录
    /// </summary>
    /// <param name="market"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public List<Deal> GetDeals(string market, DateTimeOffset? start, DateTimeOffset? end)
    {
        Expression<Func<Deal, bool>> predicate = P => P.market == market;
        if (start != null)
        {
            predicate = predicate.And(P => start <= P.time);
        }
        if (end != null)
        {
            predicate = predicate.And(P => P.time <= end);
        }
        return this.constant.db.Deal.Where(predicate).OrderBy(P => P.time).ToList();
    }

}