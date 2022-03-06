using Com.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace Com.Server;

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


    /*
    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
        .ConfigureServices((hostContext, services) =>
        {
            services.AddDbContext<DbContextEF>(DIExtension.UseDefaultSharding<DbContextEF>);
            services.AddShardingConfigure<DbContextEF>()
                    .AddEntityConfig(op =>
                    {
                        op.CreateShardingTableOnStart = true;
                        op.EnsureCreatedWithOutShardingTable = true;
                        op.UseShardingQuery((conn, builder) =>
                        {
                            builder.UseSqlServer(conn);
                        });
                        op.UseShardingTransaction((conn, builder) =>
                        {
                            builder.UseSqlServer(conn);
                        });
                        op.AddShardingTableRoute<RouteDeal>();
                        op.AddShardingTableRoute<RouteOrder>();
                        op.AddShardingTableRoute<RouteKline>();
                    }).AddConfig(op =>
                    {
                        op.ConfigId = "c1";
                        op.AddDefaultDataSource(Guid.NewGuid().ToString("n"), hostContext.Configuration.GetConnectionString("Mssql"));
                        op.ReplaceTableEnsureManager(P => new SqlServerTableEnsureManager<DbContextEF>());
                    }).EnsureConfig();
            var buildServiceProvider = services.BuildServiceProvider();
            //启动必备
            buildServiceProvider.GetRequiredService<IShardingBootstrapper>().Start();
            services.AddHostedService<MainService>();
        })
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
#if (DEBUG)
            logging.AddConsole();
#endif
        });

        */

}