using System;

namespace Com.Model.Base
{
    /// <summary>
    /// 成交单
    /// </summary>
    public struct Deal
    {
        /// <summary>
        /// 成交id
        /// </summary>
        /// <value></value>
        public string id { get; set; }
        /// <summary>
        /// 交易对
        /// </summary>
        /// <value></value>
        public string name { get; set; }
        /// <summary>
        /// 用户ID
        /// </summary>
        /// <value></value>
        public string uid { get; set; }
        /// <summary>
        /// 成交价
        /// </summary>
        /// <value></value>
        public decimal price { get; set; }
        /// <summary>
        /// 成交量
        /// </summary>
        /// <value></value>
        public decimal amount { get; set; }
        /// <summary>
        /// 成交总额
        /// </summary>
        /// <value></value>
        public decimal total { get; set; }
        /// <summary>
        /// 成交时间
        /// </summary>
        /// <value></value>
        public DateTimeOffset time { get; set; }
        /// <summary>
        /// 买订单
        /// </summary>
        /// <value></value>
        public Order Bid { get; set; }
        /// <summary>
        /// 卖订单
        /// </summary>
        /// <value></value>
        public Order Ask { get; set; }
    }
}
