using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

namespace WorkerService
{
    public class Program
    {
        public static void Main(string[] args)
        {

            var processModule = Process.GetCurrentProcess().MainModule;
            if (processModule != null)
            {
                var pathToExe = processModule.FileName;
                var pathToContentRoot = Path.GetDirectoryName(pathToExe);
                Directory.SetCurrentDirectory(pathToContentRoot);
            }

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("appsettings.json");
                    config.AddCommandLine(args);
                })
                .ConfigureLogging(loggingBuilder =>
                {
                    var configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build();
                    var logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();
                    loggingBuilder.AddSerilog(logger, dispose: true);

                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<Settings>(hostContext.Configuration.GetSection("Settings"));
                    services.AddHostedService<Worker>();
                    services.AddTransient<Settings>(_ => _.GetRequiredService<IOptions<Settings>>().Value);
                });
    }
}
