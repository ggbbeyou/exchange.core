using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Minio;
using Minio.DataModel;
using Minio.Exceptions;

namespace Com.Bll;


/// <summary>
/// minio服务
/// </summary>
public class ServiceMinio
{
    /// <summary>
    /// 日志接口
    /// </summary>
    private readonly ILogger logger;
    /// <summary>
    /// minio服务器
    /// </summary>
    public MinioClient minio;
    /// <summary>
    /// 日志
    /// </summary>
    private readonly EventId eventId = new EventId(61, "(minio)上传文件");

    // /// <summary>
    // /// http文件格式类型
    // /// </summary>
    // /// <value></value>
    // public readonly Dictionary<string, string> DictionaryContentType = new Dictionary<string, string>
    // {
    //     {"default","application/octet-stream"},
    //     {"bmp","application/x-bmp"},
    //     {"doc","application/msword"},
    //     {"docx","application/msword"},
    //     {"exe","application/x-msdownload"},
    //     {"gif","image/gif"},
    //     {"html","text/html"},
    //     {"jpg","image/jpeg"},
    //     {"mp4","video/mpeg4"},
    //     {"mpeg","video/mpg"},
    //     {"mpg","video/mpg"},
    //     {"ppt","application/x-ppt"},
    //     {"pptx","application/x-ppt"},
    //     {"png","image/png"},
    //     {"rar","application/zip"},
    //     {"txt","text/plain"},
    //     {"xls","application/x-xls"},
    //     {"xlsx","application/x-xls"},
    //     {"zip","application/zip"},
    // };

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="config">配置接口</param>
    /// <param name="logger">日志接口</param>
    public ServiceMinio(IConfiguration config, ILogger? logger = null)
    {
        this.logger = logger ?? NullLogger.Instance;
        this.minio = new MinioClient().WithEndpoint(config["minio:endpoint"]).WithCredentials(config["minio:accessKey"], config["minio:secretKey"]);
        if (bool.Parse(config["minio:ssl"]))
        {
            this.minio.WithSSL();
        }
        this.minio.Build();
    }

    /// <summary>
    /// 创建桶
    /// </summary>
    /// <param name="bucket_name">桶名</param>
    /// <returns></returns>
    public async Task MakeBucket(string bucket_name)
    {
        bool found = await this.minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket_name));
        if (!found)
        {
            await this.minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket_name));
        }
    }

    /// <summary>
    /// 文件上传，并返回http地址
    /// </summary>
    /// <param name="data">数据</param>
    /// <param name="bucket_name">桶名</param>
    /// <param name="object_name">对象名</param>
    /// <param name="contentType">内容类型</param>
    /// <returns></returns>
    public async Task UploadFile(Stream data, string bucket_name, string object_name, string contentType)
    {
        try
        {
            PutObjectArgs putObjectArgs = new PutObjectArgs().WithBucket(bucket_name).WithObject(object_name).WithContentType(contentType).WithStreamData(data).WithObjectSize(data.Length);
            await this.minio.PutObjectAsync(putObjectArgs);
        }
        catch (System.Exception ex)
        {
            this.logger.LogError(this.eventId, ex, "minio上传文件失败");
        }
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="bucket_name">桶名</param>
    /// <param name="object_name">对象名</param>
    /// <returns></returns>
    public async Task RemoveObjectAsync(string bucket_name, string object_name)
    {
        try
        {
            await this.minio.RemoveObjectAsync(new RemoveObjectArgs().WithBucket(bucket_name).WithObject(object_name));
        }
        catch (System.Exception ex)
        {
            this.logger.LogError(this.eventId, ex, "minio删除文件失败");
        }
    }

    /// <summary>
    /// 生成一个给HTTP GET请求用的presigned URL。浏览器/移动端的客户端可以用这个URL进行下载，即使其所在的存储桶是私有的。
    /// </summary>
    /// <param name="bucket_name">桶名</param>
    /// <param name="object_name">文件名</param>
    /// <param name="expiry">有效时长，5分钟</param>
    /// <returns></returns>
    public async Task<string> PresignedGetObjectAsync(string bucket_name, string object_name, int expiry = 60 * 5)
    {
        try
        {
            return await this.minio.PresignedGetObjectAsync(new PresignedGetObjectArgs().WithBucket(bucket_name).WithObject(object_name).WithExpiry(expiry));
        }
        catch (MinioException e)
        {
            this.logger.LogError(this.eventId, e, "minio创建下载地址失败");
            return "";
        }
    }

}