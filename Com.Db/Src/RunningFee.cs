using System;
using System.ComponentModel.DataAnnotations.Schema;
using Com.Api.Sdk;
using Com.Api.Sdk.Enum;
using Com.Api.Sdk.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Com.Db;

/// <summary>
/// 计账钱包流水(手续费流水)
/// 注:此表数据量超大,请使用数据库表分区功能
/// </summary>
public class RunningFee : Running
{
}