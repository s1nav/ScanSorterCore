using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WorkerService.BFSW;

namespace WorkerService
{
    public class ScanSorter
    {
        private List<BufferingFileSystemWatcher> _watchers = new List<BufferingFileSystemWatcher>();
        private string[] _watcherFilters;
        private string _rootPath;
        private string _garbagePath;
        private readonly Settings _settings;
        private readonly ILogger _logger;
        public bool WatcherStatus => _watchers.All(w => w.EnableRaisingEvents == true);

        public ScanSorter(Settings settings, ILogger logger)
        {
            _settings = settings;
            _logger = logger;
            _watcherFilters = _settings.WatchingFilter;
            _rootPath = _settings.RootPath;
            _garbagePath = _settings.GarbagePath;
        }

        public void Start()
        {
            _watchers.Clear();
            foreach (var filter in _watcherFilters)
            {
                var watcher = new BufferingFileSystemWatcher
                {
                    Filter = filter,
                    Path = _rootPath,
                    IncludeSubdirectories = true
                };
                _watchers.Add(watcher);
                watcher.Existed += WatcherAction;
                watcher.Created += WatcherAction;
                watcher.Renamed += WatcherAction;
                watcher.Error += WatcherError;
                watcher.EnableRaisingEvents = true;
                _logger.LogInformation($"Watcher with filter {filter} started");
            }
            _logger.LogInformation("Scan Sorter started");
        }
        public void Stop()
        {
            foreach (var watcher in _watchers)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            _watchers.Clear();
            _logger.LogInformation("Scan Sorter stopped");
        }

        private bool IsFileLocked(string path)
        {
            FileStream stream = null;
            var file = new FileInfo(path);

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
            return false;
        }
        private async void MoveScanCopyFileAsync(string path)
        {
            await Task.Run(() =>
            {
                var threadName = $"{Guid.NewGuid()}";
                Thread.CurrentThread.Name = threadName;

                while (IsFileLocked(path))
                {
                    Thread.Sleep(1000);
                }

                var scanCopy = new ScanFile(path, _settings, _logger);
                scanCopy.Move();
            });
        }

        private void WatcherAction(object sender, FileSystemEventArgs e)
        {
            var fullPath = e.FullPath.ToString();
            if (!fullPath.Contains(_garbagePath))
            {
                _logger.LogInformation($"File {fullPath} created");
                try
                {
                    MoveScanCopyFileAsync(fullPath);
                }
                catch (Exception exception)
                {
                    _logger.LogError($"{exception.Message}");
                    return;
                }
            }
        }
        private void WatcherError(object sender, ErrorEventArgs e)
        {
            Stop();
            Start();
        }

    }
}
