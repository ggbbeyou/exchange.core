using System;

namespace Com.Model.Base
{
    /// <summary>
    /// 订单表
    /// </summary>
    public struct Order
    {
        /// <summary>
        /// 订单id
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
        /// 挂单价
        /// </summary>
        /// <value></value>
        public decimal price { get; set; }
        /// <summary>
        /// 挂单量
        /// </summary>
        /// <value></value>
        public decimal amount { get; set; }
        /// <summary>
        /// 订单总额
        /// </summary>
        /// <value></value>
        public decimal total { get; set; }
        /// <summary>
        /// 挂单时间
        /// </summary>
        /// <value></value>
        public DateTimeOffset time { get; set; }
        /// <summary>
        /// 未成交挂单量/撤单量
        /// </summary>
        /// <value></value>
        public decimal amount_unsold { get; set; }
        /// <summary>
        /// 已成交挂单量
        /// </summary>
        /// <value></value>
        public decimal amount_done { get; set; }
        /// <summary>
        /// 交易方向
        /// </summary>
        /// <value></value>
        public E_Direction direction { get; set; }
        /// <summary>
        /// 订单类型
        /// </summary>
        /// <value></value>
        public E_OrderType type { get; set; }
        /// <summary>
        /// 附加数据
        /// </summary>
        /// <value></value>
        public string data { get; set; }

    }
}
