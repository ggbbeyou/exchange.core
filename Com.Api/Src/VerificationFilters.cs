
/*
   * 第一步：登录系统，创建api账户,保存api_key和api_secret
   * 第二步：计算api_sign, 业务请求参数按 参数名+值 以升序排序，拼接成字符串(data),api_sign=md5(api_key+data+api_secret+api_timestamp)
   * 第三步：http请求头headers加入字段api_key,api_sign,api_timestamp三个字段，把业务请求的参数传入到http消息体Body(x-www-form-urlencoded)
   * 整个流程示例如下：
   *      api_key=AAA
   *      api_sign=BBB
   *      api_timestamp=1513069869000
   * body参数如下
   *      par1=52.8
   *      par2=我是中文
   * body参数转换成data=par152.8par2我是中文
   *      api_sign=md5(AAApar152.8par2我是中文BBB1513069869000)=20a94c35c9d47a33bec1980eb398fafa
   * headers参数如下
   *      api_key=AAA
   *      sign=20a94c35c9d47a33bec1980eb398fafa
   *      timestamp=1513069869000
   */




using System.Text;
using Com.Api.Sdk.Enum;
using Com.Api.Sdk.Models;
using Com.Bll;
using Com.Bll.Util;
using Com.Db;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Com.Api.Src;

/// <summary>
/// 
/// </summary>
public class VerificationFilters : Attribute, IAuthorizationFilter
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        Res<bool> res = new Res<bool>();
        res.success = false;
        res.code = E_Res_Code.signature_error;
        res.message = "签名错误!";
        res.data = false;
        //参数判断
        string key = "";
        string sign = "";
        string timestamp = "";
        if (!context.HttpContext.Request.Headers.ContainsKey("api_key"))
        {
            res.code = E_Res_Code.not_found_api_key;
            res.message = "缺少api_key参数!";
            context.Result = new JsonResult(res);
            return;
        }
        else
        {
            key = context.HttpContext.Request.Headers["api_key"];
        }
        if (!context.HttpContext.Request.Headers.ContainsKey("api_sign"))
        {
            res.code = E_Res_Code.not_found_api_sign;
            res.message = "缺少api_sign参数!";
            context.Result = new JsonResult(res);
            return;
        }
        else
        {
            sign = context.HttpContext.Request.Headers["api_sign"];
        }
        if (!context.HttpContext.Request.Headers.ContainsKey("api_timestamp"))
        {
            res.code = E_Res_Code.not_found_api_timestamp;
            res.message = "缺少api_timestamp参数!";
            context.Result = new JsonResult(res);
            return;
        }
        else
        {
            timestamp = context.HttpContext.Request.Headers["api_timestamp"];
            long tempstamp_temp = 0;
            if (long.TryParse(timestamp, out tempstamp_temp))
            {
                DateTimeOffset requestTime = DateTimeOffset.FromUnixTimeMilliseconds(tempstamp_temp);
                int apiExpiry = 20;
                if (requestTime.AddHours(apiExpiry) <= DateTimeOffset.UtcNow)
                {
                    res.code = E_Res_Code.request_overtime;
                    res.message = "请求超时!";
                    context.Result = new JsonResult(res);
                    return;
                }
            }
            else
            {
                res.code = E_Res_Code.not_found_api_timestamp;
                res.message = "缺少api_timestamp参数!";
                context.Result = new JsonResult(res);
                return;
            }
        }
        //从数据库或缓存查找对应的appkey,             
        UsersApi? userapi = null;
        RedisValue rv = FactoryService.instance.constant.redis.HashGet(FactoryService.instance.GetRedisApiKey(), key);
        if (rv.IsNullOrEmpty)
        {
            using (var scope = FactoryService.instance.constant.provider.CreateScope())
            {
                using (DbContextEF db = scope.ServiceProvider.GetService<DbContextEF>()!)
                {
                    userapi = db.UsersApi.SingleOrDefault(P => P.api_key == key);
                    if (userapi != null)
                    {
                        FactoryService.instance.constant.redis.HashSet(FactoryService.instance.GetRedisApiKey(), key, JsonConvert.SerializeObject(userapi));
                    }
                }
            }
        }
        else
        {
            userapi = JsonConvert.DeserializeObject<UsersApi>(rv);
        }
        if (userapi == null)
        {
            res.code = E_Res_Code.not_found_api_key;
            res.message = "api_key错误!";
            context.Result = new JsonResult(res);
            return;
        }
        //判断白名单ip
        if (!string.IsNullOrWhiteSpace(userapi.white_list_ip))
        {
            res.code = E_Res_Code.not_white_ip;
            res.message = "不是白名单仿问";
            context.Result = new JsonResult(res);
            return;
        }
        else if (userapi.create_time.AddDays(10) < DateTimeOffset.UtcNow)
        {
            res.code = E_Res_Code.not_white_ip;
            res.message = "不是白名单仿问";
            context.Result = new JsonResult(res);
            return;
        }
        //是否合法判断
        SortedDictionary<string, string> sortedDictionary = new SortedDictionary<string, string>();
        foreach (var item in context.HttpContext.Request.Form)
        {
            sortedDictionary.Add(item.Key, item.Value);
        }
        //拼接参数，并在开头和结尾加上key
        StringBuilder sb = new StringBuilder(userapi.api_key);
        foreach (var item in sortedDictionary)
        {
            sb.Append(item.Key).Append(item.Value);
        }
        sb.Append(userapi.api_secret);
        sb.Append(timestamp);
        if (sign == Encryption.MD5Encrypt(sb.ToString()))
        {
            return;
        }
        else
        {
            res.code = E_Res_Code.signature_error;
            res.message = "签名错误!";
            context.Result = new JsonResult(res);
        }
    }
}