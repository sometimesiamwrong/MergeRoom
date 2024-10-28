using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Threading.Tasks;

namespace IntegrationTests
{
    public class ProgramTest
    {
        public static IHost HostApp { get; private set; }

        public static void SetupHost(string[] args)
        {
            HostApp = CreateHostBuilder(args).Build();

            Factory.ServiceProvider = HostApp.Services;
        }

        public static void StartHost()
        {
            HostApp.RunAsync();
        }

        public static Task StopHost()
        {
            return HostApp.StopAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(cfg =>
                {
                    cfg.AddJsonFile("appsettings.json", false, true);
                    cfg.AddEnvironmentVariables();
                })
                .UseSerilog((hostingContext, loggerConfiguration) =>
                {
                    loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseKestrel(options =>
                        {
                            options.Limits.MaxRequestBodySize = 1073741824L; //1GiB
                        })
                        .UseStartup<StartupTest>();
                });
        }
    }
}
