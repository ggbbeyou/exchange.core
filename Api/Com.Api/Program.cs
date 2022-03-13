using NLog.Web;

namespace Com.Api;

/// <summary>
/// 
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        var logger = NLog.Web.NLogBuilder.ConfigureNLog("NLog.config").GetCurrentClassLogger();
        try
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler((sender, args) =>
            {
                Exception e = (Exception)args.ExceptionObject;
                logger.Fatal(e, "由于异常而停止程序");
            });
            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception ex)
        {
            logger.Fatal(ex, "由于异常而停止程序");
        }
        finally
        {
            NLog.LogManager.Shutdown();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseStartup<Startup>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
#if (DEBUG)
        logging.AddConsole();
#endif
    })
    .UseNLog();
}

