using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Com.Model.Base;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Com.Matching
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var host = CreateHostBuilder();
            await host.RunConsoleAsync();
            return Environment.ExitCode;          
        }

        private static IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder().ConfigureServices(services =>
            {
                services.AddHostedService<Worker>();
            });
        }
    }
}
