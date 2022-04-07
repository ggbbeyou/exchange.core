using Com.Db;
using GrpcExchange;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Grpc.Net.Client;
using Com.Service;
using NLog.Extensions.Logging;
using Exceptionless;

IHostBuilder builder = Host.CreateDefaultBuilder(args);
builder.ConfigureServices((hostContext, services) =>
        {
            services.AddDbContextPool<DbContextEF>(options =>
            {
                // options.UseLoggerFactory(LoggerFactory.Create(builder => { builder.AddConsole(); }));
                // options.EnableSensitiveDataLogging();
                DbContextOptions options1 = options.UseSqlServer(hostContext.Configuration.GetConnectionString("Mssql")).Options;
            });
            services.AddHostedService<MainService>();
            services.BuildServiceProvider();
        });
builder.ConfigureLogging((hostContext, logging) =>
        {
            logging.ClearProviders();
#if (DEBUG)
            logging.AddConsole();
#endif
            logging.AddNLog();
        });
// ExceptionlessClient.Default.Startup("kaOhMYizKiSSQaFtlOiWEpbb49GrBTi7rhGHuPXd");
var app = builder.Build();
app.Run();




/*
namespace Com.Service;

/// <summary>
/// 
/// </summary>
class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    static async Task Main(string[] args)
    {
        using IHost host = CreateHostBuilder(args).Build();
        await host.RunAsync();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
        .ConfigureServices((hostContext, services) =>
        {
            services.AddDbContextPool<DbContextEF>(options =>
            {
                // options.UseLoggerFactory(LoggerFactory.Create(builder => { builder.AddConsole(); }));
                // options.EnableSensitiveDataLogging();
                DbContextOptions options1 = options.UseSqlServer(hostContext.Configuration.GetConnectionString("Mssql")).Options;
            });
            services.AddHostedService<MainService>();
            services.BuildServiceProvider();
        })
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
#if (DEBUG)
            logging.AddConsole();
#endif
        });

}


*/


