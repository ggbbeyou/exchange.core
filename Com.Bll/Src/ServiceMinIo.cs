using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
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
    /// 默认桶名
    /// </summary>
    public readonly string defalut_bucketName = "play559";
    /// <summary>
    /// 日志
    /// </summary>
    private readonly EventId eventId = new EventId(61, "(minio)上传文件");
    /// <summary>
    /// 目录
    /// </summary>
    /// <typeparam name="int"></typeparam>
    /// <typeparam name="string"></typeparam>
    /// <returns></returns>
    public readonly Dictionary<int, string> dir = new Dictionary<int, string>();
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
    /// <param name="logger">日志接口</param>
    /// <param name="endpoint">minio服务地址</param>
    /// <param name="access">账号</param>
    /// <param name="secret">密码</param>
    /// <param name="ssl">是否支持ssl</param>
    public ServiceMinio(ILogger? logger = null)
    {
        this.logger = logger ?? NullLogger.Instance;

        this.dir.Add(0, "backstage");
        this.dir.Add(1, "avatar");
        this.dir.Add(2, "backstage");
        this.dir.Add(3, "recharge");
        this.dir.Add(4, "withdrawal");
        this.dir.Add(5, "complaint");
        this.dir.Add(6, "game");
    }

    /// <summary>
    /// 初始化
    /// </summary>    
    /// <param name="endpoint">minio服务地址</param>
    /// <param name="accessKey">账号</param>
    /// <param name="secretKey">密码</param>
    /// <param name="ssl">是否支持ssl</param>
    public void Init(string endpoint, string accessKey, string secretKey, bool ssl)
    {
        this.minio = new MinioClient().WithEndpoint(endpoint).WithCredentials(accessKey, secretKey);
        if (ssl)
        {
            this.minio.WithSSL();
        }
        this.minio = this.minio.Build();
    }

    /// <summary>
    /// 文件上传，并返回http地址
    /// </summary>
    /// <param name="data">数据</param>
    /// <param name="bucket_name">桶名</param>
    /// <param name="object_name">对象名 : /test/test.png</param>
    /// <param name="contentType">http内容类型</param>
    /// <returns></returns>
    public async Task UploadFile(Stream data, string bucket_name, string object_name, string file_name, string contentType)
    {
        try
        {
            // bool found = await this.minio.BucketExistsAsync(bucketName);  
            bool found = await this.minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket_name));
            if (!found)
            {
                await this.minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket_name));
            }
            // var ssec = new SSEC(aesEncryption.Key);
            PutObjectArgs putObjectArgs = new PutObjectArgs().WithBucket(bucket_name).WithObject(object_name).WithContentType(contentType).WithStreamData(data).WithObjectSize(data.Length);
            //  .WithServerSideEncryption(ssec);
            await this.minio.PutObjectAsync(putObjectArgs);//, objectName, data, data.Length, contentType);
            //return await this._minio.PresignedGetObjectAsync(bucketName, objectName, 60, contentType);
        }
        catch (System.Exception ex)
        {
            this.logger.LogError(this.eventId, ex, "minio上传文件失败");
        }
    }


    /// <summary>
    /// 文件上传，并返回http地址
    /// </summary>
    /// <param name="data">数据</param>
    /// <param name="bucketName">桶名</param>
    /// <param name="objectName">对象名 : /test/test.png</param>
    /// <param name="contentType">http内容类型</param>
    /// <returns></returns>
    public async Task UploadFile(string filePath, string bucketName, string objectName, string contentType)
    {
        try
        {
            bool found = await this.minio.BucketExistsAsync(bucketName);
            if (!found)
            {
                await this.minio.MakeBucketAsync(bucketName);
            }
            await this.minio.PutObjectAsync(bucketName, objectName, filePath, contentType);
            //return await this._minio.PresignedGetObjectAsync(bucketName, objectName, 60, contentType);
        }
        catch (System.Exception ex)
        {
            this.logger.LogError(this.eventId, ex, "minio上传文件失败");
        }
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="bucketName">桶</param>
    /// <param name="objectName">文件名称：/test/test.png</param>
    /// <returns></returns>
    public async Task RemoveObjectAsync(string bucketName, string objectName)
    {
        try
        {
            await this.minio.RemoveObjectAsync(bucketName, objectName);
        }
        catch (System.Exception ex)
        {
            this.logger.LogError(this.eventId, ex, "minio删除文件失败");
        }
    }

    /// <summary>
    /// 删除桶
    /// </summary>
    /// <param name="bucketName">桶</param>
    /// <returns></returns>
    public async Task RemoveBucketAsync(string bucketName)
    {
        try
        {
            await this.minio.RemoveBucketAsync(bucketName);
        }
        catch (System.Exception ex)
        {
            this.logger.LogError(this.eventId, ex, "minio删除桶失败");
        }
    }

    /// <summary>
    /// 生成一个给HTTP GET请求用的presigned URL。浏览器/移动端的客户端可以用这个URL进行下载，即使其所在的存储桶是私有的。
    /// </summary>
    /// <param name="bucketName">桶名</param>
    /// <param name="objectName">文件名</param>
    /// <param name="second">有效时长，默认1天</param>
    /// <returns></returns>
    public async Task<string> PresignedGetObjectAsync(string bucketName, string objectName, int second = 60 * 60 * 24)
    {
        try
        {
            return await this.minio.PresignedGetObjectAsync(bucketName, objectName, 60 * 60 * 24);
        }
        catch (MinioException e)
        {
            this.logger.LogError(this.eventId, e, "minio创建下载地址失败");
            return "";
        }
    }

    /// <summary>
    /// 获取指定目录下文件
    /// </summary>
    /// <param name="bucketName">桶名</param>
    /// <param name="prefix">目录</param>
    /// <param name="recursive">是否获取子目录子文件</param>
    /// <returns></returns>
    public async Task<IObservable<Item>> ListObjectsAsync(string bucketName, string prefix, bool recursive = false)
    {
        try
        {
            bool found = await this.minio.BucketExistsAsync(bucketName);
            if (found)
            {
                IObservable<Item> observable = this.minio.ListObjectsAsync(bucketName, prefix, recursive);
                return observable;
            }
            else
            {
                this.logger.LogInformation(this.eventId, $"minio获取指定目录文件列表桶名不存在prefix：{prefix}");
                return null;
            }
        }
        catch (MinioException e)
        {
            this.logger.LogError(this.eventId, e, "minio获取指定目录文件列表异常");
            return null;
        }
    }

    /// <summary>
    /// 批量删除
    /// </summary>
    /// <param name="bucketName"></param>
    /// <param name="objectNamesList"></param>
    /// <returns></returns>
    public async Task<bool> RemoveObjectAsync(string bucketName, List<string> objectNamesList)
    {
        try
        {
            IObservable<DeleteError> observable = await minio.RemoveObjectAsync(bucketName, objectNamesList);
            IDisposable subscription = observable.Subscribe(
                deleteError => this.logger.LogInformation(this.eventId, $"minio批量删除失败objectNames:{deleteError.Key}"),
                ex => this.logger.LogError(this.eventId, ex, $"minio批量删除异常"),
                () =>
                {
                    this.logger.LogInformation(this.eventId, $"minio批量删除失败{bucketName}");
                });
            return true;
        }
        catch (MinioException e)
        {
            this.logger.LogError(this.eventId, e, $"minio批量删除异常objectNamesList:{Newtonsoft.Json.JsonConvert.SerializeObject(objectNamesList)}");
            return false;
        }
    }

    /// <summary>
    /// 获取新的对象名
    /// </summary>
    /// <param name="extension">后缀</param>
    /// <param name="dic_type">类型:1:玩家头像,2:后台,3:充值,4:取款,5:投诉,6:游戏ico,</param>
    /// <returns>key:objectName,value:url</returns>
    public KeyValuePair<string, string> GetNewObjectName(string extension, int dic_type, bool isdate)
    {
        string dictionary = this.dir[dic_type];
        dictionary = dictionary + "/";
        if (isdate)
        {
            dictionary += $"{DateTime.UtcNow.ToString("yyyyMM")}/";
        }
        dictionary = dictionary + DateTime.UtcNow.ToString("yyyyMMddHHmmsssss") + "_" + new Random().Next(0, 999999).ToString().PadLeft(6, '0') + extension;
        return new KeyValuePair<string, string>(dictionary, "/" + defalut_bucketName + "/" + dictionary);
    }

    /// <summary>
    /// 获取文件列表
    /// </summary>
    /// <param name="prefix">前缀</param>
    /// <returns></returns>
    public async Task<List<string>> GetObjectList(string prefix)
    {
        List<string> filelist = new List<string>();
        bool found = await this.minio.BucketExistsAsync(defalut_bucketName);
        bool onCompleted = false;
        if (found)
        {
            IObservable<Item> observable = this.minio.ListObjectsAsync(defalut_bucketName, prefix, true);
            IDisposable subscription = observable.Subscribe(
                    item => filelist.Add($"/{defalut_bucketName}/{item.Key}"),
                    ex => Console.WriteLine("", ex.Message),
                    () => onCompleted = true);
        }
        while (onCompleted == false)
        {
            System.Threading.Thread.Sleep(10);
        }
        return filelist;
    }

}