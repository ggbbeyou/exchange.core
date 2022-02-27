using Com.Common;

namespace Com.Bll;

/*


*/


/// <summary>
/// 交易记录
/// </summary>
public class DealService
{
    /// <summary>
    /// 单例类的实例
    /// </summary>
    /// <returns></returns>
    public static readonly DealService instance = new DealService();
    /// <summary>
    /// 常用接口
    /// </summary>
    private FactoryConstant constant = null!;
    /// <summary>
    /// 系统初始化时间  初始化  注:2017-1-1 此时是一年第一天，一年第一月，一年第一个星期日(星期日是一个星期开始的第一天)
    /// </summary>
    public DateTimeOffset system_init;
    /// <summary>
    /// k线DB类
    /// </summary>
    public KilneHelper kilneHelper = null!;
    /// <summary>
    /// 交易记录Db类
    /// </summary>
    public DealHelper dealHelper = null!;

    private DealService()
    {

    }


    /// <summary>
    ///
    /// </summary>
    /// <param name="configuration">配置接口</param>
    /// <param name="environment">环境接口</param>
    /// <param name="logger">日志接口</param>
    public void Init(FactoryConstant constant, DateTimeOffset system_init)
    {
        this.system_init = system_init;
        this.constant = constant;
        this.dealHelper = new DealHelper(constant);
        this.kilneHelper = new KilneHelper(constant, system_init);
    }






}