using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Com.Bll.Util;

/// <summary>
/// MongoDb帮助类
/// </summary>
public class MongoHelper
{
    private readonly IMongoDatabase _db = null!;
    private readonly ILogger logger;

    /// <summary>
    /// 数据库对象
    /// </summary>
    private IMongoDatabase _dataBase;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="conStrMdb">连接字符串</param>
    public MongoHelper(string conStrMdb, ILogger? logger)
    {
        this.logger = logger ?? NullLogger.Instance;
        this._dataBase = GetDb(conStrMdb);
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="_dataBase">接口</param>
    public MongoHelper(IMongoDatabase _dataBase, ILogger? logger)
    {
        this.logger = logger ?? NullLogger.Instance;
        this._dataBase = _dataBase;
    }

    /// <summary>
    /// 若没有，根据传入的数据库名字来生成对应的数据库名，否则，返回db
    /// </summary>
    /// <param name="conStrMdb">数据库连接</param>
    /// <returns></returns>
    public IMongoDatabase GetDb(string conStrMdb)
    {
        var db = new MongoClient(conStrMdb).GetDatabase(new MongoUrlBuilder(conStrMdb).DatabaseName);
        return db;
    }

    /// <summary>
    /// 创建集合对象
    /// </summary>
    /// <param name="collName">集合名称</param>
    ///<returns>集合对象</returns>
    private IMongoCollection<T> GetColletion<T>(string collName)
    {
        return _dataBase.GetCollection<T>(collName);
    }

    /// <summary>
    /// 获取指定数据库集合中的所有的文档
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="tableName">表名</param>
    /// <returns></returns>
    public List<T> FindAll<T>(string tableName)
    {
        List<T> list = new List<T>();
        try
        {
            var collection = GetColletion<T>(collName: tableName);
            FilterDefinition<T> filter = Builders<T>.Filter.Empty;
            list = collection.Find<T>(filter).ToList<T>();
        }
        catch (Exception ex)
        {

            this.logger.LogError(ex.Message, "MongoDbHelper.FindAll");
        }
        return list;
    }

    /// <summary>
    /// 插入对象
    /// </summary>
    /// <param name="collName">集合名称</param>
    /// <param name="document">插入的对象</param>
    /// <returns>异常返回-101</returns>
    public void Insert<T>(string collName, T document)
    {
        try
        {
            var coll = GetColletion<T>(collName);
            coll.InsertOne(document);

        }
        catch (Exception ex)
        {
            this.logger.LogError(ex.Message, "MongoDbHelper.Insert");
        }
    }

    /// <summary>
    /// 批量插入
    /// </summary>
    /// <param name="collName">集合名称</param>
    /// <param name="documents">要插入的对象集合</param>
    /// <returns>异常返回-101</returns>
    public void InsertMany<T>(string collName, List<T> documents)
    {
        try
        {
            var coll = GetColletion<T>(collName);
            coll.InsertMany(documents);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex.Message, "MongoDbHelper.InsertMany");
        }
    }

    /// <summary>
    /// 修改文档
    /// </summary>
    /// <param name="collName">集合名称</param>
    /// <param name="filter">修改条件</param>
    /// <param name="update">修改结果</param>
    /// <param name="upsert">是否插入新文档（filter条件满足就更新，否则插入新文档）</param>
    /// <returns>修改影响文档数,异常返回-101</returns>
    public void Update<T>(string collName, Expression<Func<T, Boolean>> filter, UpdateDefinition<T> update, Boolean upsert = false)
    {
        try
        {
            var coll = GetColletion<T>(collName);
            var result = coll.UpdateMany(filter, update, new UpdateOptions { IsUpsert = upsert });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex.Message, "MongoDbHelper.Update");
        }
    }

    /// <summary>
    /// 按BsonDocument条件删除
    /// </summary>
    /// <param name="collName">集合名称</param>
    /// <param name="document">文档</param>
    /// <returns>异常返回-101</returns>
    public void Delete<T>(string collName, BsonDocument document)
    {
        try
        {
            var coll = GetColletion<T>(collName);
            var result = coll.DeleteOne(document);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex.Message, "MongoDbHelper.Delete");
        }
    }

    /// <summary>
    /// 按条件表达式删除
    /// </summary>
    /// <param name="collName">集合名称</param>
    /// <param name="predicate">条件表达式</param>
    /// <returns>异常返回-101</returns>
    public void Delete<T>(string collName, Expression<Func<T, Boolean>> predicate)
    {
        try
        {
            var coll = GetColletion<T>(collName);
            var result = coll.DeleteOne(predicate);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex.Message, "MongoDbHelper.Delete");
        }
    }

    /// <summary>
    /// 按检索条件删除（建议用Builders-T构建复杂的查询条件）
    /// </summary>
    /// <param name="collName">集合名称</param>
    /// <param name="filter">条件</param>
    /// <returns></returns>
    public void Delete<T>(string collName, FilterDefinition<T> filter)
    {
        try
        {
            var coll = GetColletion<T>(collName);
            var result = coll.DeleteOne(filter);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex.Message, "MongoDbHelper.Delete");
        }
    }

    /// <summary>
    /// 查询，复杂查询直接用Linq处理
    /// </summary>
    /// <param name="collName">集合名称</param>
    /// <returns>要查询的对象</returns>
    public IQueryable<T>? GetQueryable<T>(string collName)
    {
        try
        {
            var coll = GetColletion<T>(collName);
            return coll.AsQueryable<T>();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex.Message, "MongoDbHelper.GetQueryable");
            return null;
        }
    }
}