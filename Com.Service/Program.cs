using Com.Db;
using GrpcExchange;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Grpc.Net.Client;


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
                options.UseLoggerFactory(LoggerFactory.Create(builder => { builder.AddConsole(); }));
                options.EnableSensitiveDataLogging();
                DbContextOptions options1 = options.UseSqlServer(hostContext.Configuration.GetConnectionString("Mssql")).Options;
            });
            services.AddGrpcClient<GreeterImpl>(options =>
            {
                options.Address = new Uri(hostContext.Configuration.GetValue<string>("manage_url"));
            });
            // services.AddHostedService<MainService>();
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