using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Logging;

namespace Com.Matching
{
    class Program
    {
        // static async Task Main(string[] args)
        // {
        //     IHostBuilder hosts = Host.CreateDefaultBuilder();
        //     hosts.ConfigureLogging(logging =>
        //     {
        //         logging.ClearProviders();
        //         logging.AddConsole();
        //     });
        //     hosts = hosts.ConfigureHostConfiguration(config =>
        //     {
        //         config.Sources.Clear();
        //         config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        //         config.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
        //         config.AddEnvironmentVariables();
        //         config.Build();
        //     });
        //     hosts = hosts.ConfigureServices(config =>
        //     {
        //         config.AddHostedService<MainService>();
        //     });
        //     IHost host = hosts.Build();
        //     await host.RunAsync();
        // }


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
                services.AddHostedService<MainService>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
#if (DEBUG)
                logging.AddConsole();
#endif

            });


    }
}
