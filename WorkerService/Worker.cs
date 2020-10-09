using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly Settings _settings;
        private readonly ILogger _logger;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private ScanSorter _scanSorter;

        public Worker(IHostApplicationLifetime hostApplicationLifetime, IOptions<Settings> settings, ILogger<Worker> logger)
        {
            _settings = settings.Value;
            _logger = logger;
            _hostApplicationLifetime = hostApplicationLifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.CompletedTask;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service starting...");
            if (!_settings.Valid())
            {
                _logger.LogError("Please check configuration file and restart the service");
                _hostApplicationLifetime.StopApplication();
            }

            _scanSorter = new ScanSorter(_settings, _logger);
            _scanSorter.Start();
            _logger.LogInformation("Service started");
            return base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service stopping");
            _scanSorter?.Stop();
            await base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
