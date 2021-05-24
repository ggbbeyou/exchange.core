using System;
using Snowflake;

namespace Com.Matching
{
    /// <summary>
    /// 工具类
    /// </summary>
    public static class Util
    {
        public static IdWorker worker = new IdWorker(1, 1);

        /// <summary>
        /// 获取最新成交价
        /// </summary>
        /// <param name="bid">买入价</param>
        /// <param name="ask">卖出价</param>
        /// <param name="last">最后价格</param>
        /// <returns>最新价</returns>
        public static decimal GetNewPrice(decimal bid, decimal ask, decimal last)
        {
            if(bid==0||ask==0||last==0)
            {
                return 0;
            }
            if (bid < ask)
            {
                return 0;
            }
            if (bid <= last)
            {
                return bid;
            }
            else if (ask >= last)
            {
                return ask;
            }
            else if (ask < last && last < bid)
            {
                return last;
            }
            return 0;
        }

        public static long a()
        {

            return worker.NextId();
        }
    }
}
