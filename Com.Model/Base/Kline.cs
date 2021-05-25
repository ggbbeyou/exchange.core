using System;

namespace Com.Model.Base
{
    /// <summary>
    /// K线
    /// </summary>
    public class Kline
    {
        /// <summary>
        /// 交易对
        /// </summary>
        /// <value></value>
        public string name { get; set; }
        /// <summary>
        /// 成交量
        /// </summary>
        /// <value></value>
        public decimal amount { get; set; }
        /// <summary>
        /// 成交笔数
        /// </summary>
        /// <value></value>
        public decimal count { get; set; }
        /// <summary>
        /// 成交总金额
        /// </summary>
        /// <value></value>
        public decimal total { get; set; }
        /// <summary>
        /// 开盘价
        /// </summary>
        /// <value></value>
        public decimal open { get; set; }
        /// <summary>
        /// 收盘价（当K线为最晚的一根时，是最新成交价）
        /// </summary>
        /// <value></value>
        public decimal close { get; set; }
        /// <summary>
        /// 最低价
        /// </summary>
        /// <value></value>
        public decimal low { get; set; }
        /// <summary>
        /// 最高价
        /// </summary>
        /// <value></value>
        public decimal high { get; set; }
        /// <summary>
        /// 变更时间
        /// </summary>
        /// <value></value>
        public DateTimeOffset last_time { get; set; }
    }
}
