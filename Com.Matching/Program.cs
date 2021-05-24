using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Com.Model.Base;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Com.Matching
{
    class Program
    {
        static async Task Main(string[] args)
        {
            IHostBuilder host = Host.CreateDefaultBuilder();
            host = host.ConfigureHostConfiguration(config =>
            {
                config.Sources.Clear();
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();
                config.Build();
            });
            host = host.ConfigureServices(build =>
            {
                build.AddHostedService<Worker>();
            });
            IHost ihost = host.Build();
            await ihost.RunAsync();
        }

    }
}
