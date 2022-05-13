using System;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace Com.Api.Sdk;

/// <summary>
/// 
/// </summary>
public class Api
{
    // /// <summary> 
    // /// 请求web数据
    // /// </summary>
    // /// <param name="url">接口地址</param>
    // /// <param name="input">输入数据</param>
    // /// <typeparam name="T">输出类型</typeparam>
    // /// <returns>返回数据</returns>
    // public AjaxResult<T> GetPost<T>(string url, object input = null, Method method = Method.POST)
    // {
    //     RestRequest request = new RestRequest(url, method);
    //     request.AddHeader("X-IDCM-APIKEY", this.api_key);
    //     string json = "";
    //     try
    //     {
    //         if (input != null)
    //         {
    //             json = JsonConvert.SerializeObject(input);
    //             if (input is Dictionary<string, string>)
    //             {
    //                 foreach (var item in input as Dictionary<string, string>)
    //                 {
    //                     request.AddParameter(item.Key, item.Value);
    //                 }
    //             }
    //             else
    //             {
    //                 request.AddParameter("application/json", json, ParameterType.RequestBody);
    //                 this.logger.LogTrace(eventId, url + ":" + json);
    //             }
    //         }
    //         IRestResponse<AjaxResult<T>> asyncHandle = client.Execute<AjaxResult<T>>(request);
    //         if (asyncHandle.ErrorException != null)
    //         {
    //             const string message = "Error retrieving response.  Check inner details for more info.";
    //             var twilioException = new ApplicationException(message, asyncHandle.ErrorException);
    //             throw twilioException;
    //         }
    //         if (asyncHandle.StatusCode == HttpStatusCode.OK && asyncHandle.Data != null)
    //         {
    //             return asyncHandle.Data;
    //         }
    //         else if (!string.IsNullOrWhiteSpace(asyncHandle.Content))
    //         {
    //             return JsonConvert.DeserializeObject<AjaxResult<T>>(asyncHandle.Content);
    //         }
    //         else
    //         {
    //             this.logger.LogError(eventId, asyncHandle.ErrorException, $"url:{url},input:{json},result:{asyncHandle.Content}");
    //             return default(AjaxResult<T>);
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         this.logger.LogError(eventId, ex, $"url:{url},input:{json}");
    //     }
    //     return default(AjaxResult<T>);
    // }

    /// <summary>
    /// 日志接口
    /// </summary>
    public readonly ILogger logger;
    /// <summary>
    /// 
    /// </summary>
    private readonly HttpClient client;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="base_url"></param>
    public Api(string base_url, ILogger logger)
    {
        this.logger = logger ?? NullLogger.Instance;
        this.client = new HttpClient();
        this.client.BaseAddress = new Uri(base_url);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void AddHeader(string key, string value)
    {
        this.client.DefaultRequestHeaders.Add(key, value);
    }

    /// <summary>
    /// 返回登录token
    /// </summary>
    /// <param name="api_key"></param>
    /// <param name="api_secret"></param>
    /// <returns></returns>
    public string GetLoginToken(string api_key, string api_secret)
    {
        return "";
    }

    /// <summary>
    /// HttpGet请求
    /// </summary>
    /// <param name="url">请求地址</param>
    /// <param name="input">请求参数</param>
    /// <returns></returns>
    private async Task<string?> Get(string url, Dictionary<string, string> input)
    {
        try
        {
            if (input != null && input.Count > 0)
            {
                StringBuilder sb = new StringBuilder("?");
                foreach (var item in input)
                {
                    sb.Append($"{item.Key}={item.Value}&");
                }
                url += sb.ToString().TrimEnd('&');
            }
            HttpResponseMessage response = await client.GetAsync(url);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                return null;
            }
        }
        catch (System.Exception ex)
        {
            this.logger.LogError(ex, $"url:{url},input:{JsonConvert.SerializeObject(input)}");
            return null;
        }
    }

    /// <summary>
    /// HttpPost请求
    /// </summary>
    /// <param name="url">请求地址</param>
    /// <param name="input">请求参数</param>
    /// <returns></returns>
    private async Task<string?> Post(string url, object input)
    {
        try
        {
            HttpContent content = new StringContent(JsonConvert.SerializeObject(obj));
            HttpResponseMessage response = await client.PostAsync(url, content);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                return null;
            }
        }
        catch (System.Exception ex)
        {
            this.logger.LogError(ex, $"url:{url},input:{JsonConvert.SerializeObject(input)}");
            return null;
        }
    }

}