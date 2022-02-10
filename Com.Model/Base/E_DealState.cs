using System;

namespace Com.Model.Base;

    /// <summary>
    /// 成交状态
    /// </summary>
    public enum E_DealState
    {
        /// <summary>
        /// 未成交
        /// </summary>
        unsold = 0,
        /// <summary>
        /// 部分成交
        /// </summary>
        partial = 1,
        /// <summary>
        /// 完全成交
        /// </summary>
        completed = 2,
        /// <summary>
        /// 撤单
        /// </summary>
        cancel = 3
    }