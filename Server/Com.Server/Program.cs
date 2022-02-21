using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Com.Server;
class Program
{
    static async Task Main(string[] args)
    {
        IHostBuilder hosts = Host.CreateDefaultBuilder();
        // hosts.SetBasePath(Environment.CurrentDirectory)
        hosts = hosts.ConfigureHostConfiguration(config =>
        {
            config.Sources.Clear();
            config.AddEnvironmentVariables();
            config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            config.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
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