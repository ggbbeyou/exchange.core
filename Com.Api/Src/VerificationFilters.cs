
/*
   * 第一步：登录系统，获取appkey
   * 第二步：http请求头headers加入字段api_key,sign,timestamp三个字段，api_key：用户名，sign：签名值（后面详细解说），timestamp：当前时间戳 ，如：2017/12/12 17:11:9 值为： 1513069869
   * 第三步：把业务请求的参数传入到http消息体Body(x-www-form-urlencoded)
   * 第四步：计算sign值,对除签名外的所有请求参数按 参数名+值 做的升序排列,value值无需编码
   * 整个流程示例如下：
   *      appkey=D9U7YY5D7FF2748AED89E90HJ88881E6 (此参数不需要排序，而是加在文本开头和结尾处)
   * headers参数如下
   *      api_key=user1
   *      sign=???
   *      timestamp=1513069265
   * body参数如下
   *      par1=52.8
   *      par2=这是一个测试参数
   *      par3=852
   * 
   * 1）按 参数名+值 以升序排序，结果：api_keyuser1par152.8par2这是一个测试参数par3852timestamp1513069265
   * 2）在本文开头和结尾加上登录时获取的appkey 结果为：D9U7YY5D7FF2748AED89E90HJ88881E6api_keyuser1par152.8par2这是一个测试参数par3852timestamp1513069265D9U7YY5D7FF2748AED89E90HJ88881E6
   * 3）对此文本进行md5 32位 大写 加密，此时就是sign值  结果为：B44C81F3DF4D5E8A614C84977D33E8D2  
   */




using Com.Api.Sdk.Enum;
using Com.Api.Sdk.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

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
        //参数判断
        if (!context.HttpContext.Request.Headers.ContainsKey("api_key"))
        {
            res.success = false;
            res.code = E_Res_Code.no_permission;
            res.message = "缺少api_key参数!";
            JsonResult json = new JsonResult(res);
            context.Result = json;
        }
        else if (!context.HttpContext.Request.Headers.ContainsKey("sign"))
        {
            res.code = E_Res_Code.no_permission;
            res.message = "缺少sign参数!";
            JsonResult json = new JsonResult(res);
            context.Result = json;
        }
        else if (!context.HttpContext.Request.Headers.ContainsKey("timestamp"))
        {
            res.code = E_Res_Code.no_permission;
            res.message = "缺少timestamp参数!";
            JsonResult json = new JsonResult(res);
            context.Result = json;
        }
        else
        {
            string api_key = context.HttpContext.Request.Headers["api_key"];
            string sign = context.HttpContext.Request.Headers["sign"];
            string timestamp = context.HttpContext.Request.Headers["timestamp"];
            DateTimeOffset requestTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(timestamp));
            // 接口过期
            int apiExpiry = 20;
            if (requestTime.AddSeconds(apiExpiry) < DateTime.Now)
            {
                res.code = E_Res_Code.request_overtime;
                res.message = "接口过期!";
                JsonResult json = new JsonResult(res);
                context.Result = json;
            }
            else
            {
                //从数据库或缓存查找对应的appkey,
                string appkey = "fdsafdsafdsafasdfasdf";

                if (!string.IsNullOrEmpty(appkey))
                {
                    res.code = -4;
                    res.message = "api_key不存在!";
                    JsonResult json = new JsonResult(res);
                    context.Result = json;
                    return;
                }
                //是否合法判断
                SortedDictionary<string, string> sortedDictionary = new SortedDictionary<string, string>();
                sortedDictionary.Add("api_key", api_key);
                sortedDictionary.Add("timestamp", timestamp);
                //获取post数据,并排序
                Stream stream = context.HttpContext.Request.Body;
                byte[] buffer = new byte[context.HttpContext.Request.ContentLength.Value];
                stream.Read(buffer, 0, buffer.Length);
                string content = Encoding.UTF8.GetString(buffer);
                context.HttpContext.Request.Body = new MemoryStream(buffer);
                if (!String.IsNullOrEmpty(content))
                {
                    string postdata = System.Web.HttpUtility.UrlDecode(content);
                    string[] posts = postdata.Split(new char[] { '&' });
                    foreach (var item in posts)
                    {
                        string[] post = item.Split(new char[] { '=' });
                        sortedDictionary.Add(post[0], post[1]);
                    }
                }
                //拼接参数，并在开头和结尾加上key
                StringBuilder sb = new StringBuilder(appkey);
                foreach (var item in sortedDictionary)
                {
                    sb.Append(item.Key).Append(item.Value);
                }
                sb.Append(appkey);
                if (sign != CryptographyHelper.Md5_Encryption(sb.ToString()))
                {
                    res.code = -2;
                    res.message = "签名不合法!";
                    JsonResult json = new JsonResult(res);
                    context.Result = json;
                }
            }
        }
    }
}
