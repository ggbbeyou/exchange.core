using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Com.Db;

/// <summary>
/// 区块钱包基础信息
/// </summary>
public class BlockWallet
{
    /// <summary>
    /// 钱包id
    /// </summary>
    /// <value></value>
    public long block_wallet_id { get; set; }
    /// <summary>
    /// 钱包地址
    /// </summary>
    /// <value></value>
    public string address { get; set; } = null!;
    /// <summary>
    /// 钱包密钥
    /// </summary>
    /// <value></value>
    public string secret { get; set; } = null!;
    /// <summary>
    /// 支付链id
    /// </summary>
    /// <value></value>
    public long chain { get; set; }
    /// <summary>
    /// 用户id
    /// </summary>
    /// <value></value>
    public long user_id { get; set; }
    /// <summary>
    /// 用户名
    /// </summary>
    /// <value></value>
    public string user_name { get; set; } = null!;
    /// <summary>
    /// 币id
    /// </summary>
    /// <value></value>
    public long coin_id { get; set; }
    /// <summary>
    /// 币名称
    /// </summary>
    /// <value></value>
    public string coin_name { get; set; } = null!;
    /// <summary>
    /// 余额
    /// </summary>
    /// <value></value>
    public decimal balance { get; set; }

}