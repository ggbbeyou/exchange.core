using System.Text;
using Com.Db;
using Com.Db.Enum;
using Com.Db.Model;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace Com.Bll;

/// <summary>
/// 服务工厂
/// </summary>
public class FactoryApi
{
    /// <summary>
    /// 单例类的实例
    /// </summary>
    /// <returns></returns>
    public static readonly FactoryApi instance = new FactoryApi();
    /// <summary>
    /// 常用接口
    /// </summary>
    // public FactoryConstant constant = null!;


    /// <summary>
    /// private构造方法
    /// </summary>
    private FactoryApi()
    {

    }

    /// <summary>
    /// 初始化方法
    /// </summary>
    /// <param name="constant"></param>
    // public void Init(FactoryConstant constant)
    // {
    //     this.constant = constant;
    // }

    

}