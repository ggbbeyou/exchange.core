using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Com.Matching
{
    class Program
    {
        static async Task Main(string[] args)
        {
            IHostBuilder hosts = Host.CreateDefaultBuilder();
            hosts = hosts.ConfigureHostConfiguration(config =>
            {
                config.Sources.Clear();
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();
                config.Build();
            });
            hosts = hosts.ConfigureServices(config =>
            {
                config.AddHostedService<MainService>();
            });
            IHost host = hosts.Build();
            await host.RunAsync();
        }

    }
}
