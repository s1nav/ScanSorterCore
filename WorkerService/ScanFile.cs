using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace WorkerService
{
    public class ScanFile
    {
        private string _generatedFileName;
        private FileInfo _sourceFile;
        private ScanUser _owner;
        private Guid _guid = Guid.NewGuid();
        private readonly Settings _settings;
        private readonly ILogger _logger;

        public ScanFile(string path, Settings settings, ILogger logger)
        {
            _settings = settings;
            _sourceFile = new FileInfo(path);
            _generatedFileName = $"{_guid}{_sourceFile.Extension}";
            _owner = new ScanUser(_sourceFile.Directory.Name, _settings);
            _logger = logger;
        }

        private void MoveToGarbage()
        {
            var destinationFile = $"{_settings.GarbagePath}\\{_owner.UserName}-{_generatedFileName}";

            if (!Directory.Exists(_settings.GarbagePath))
                Directory.CreateDirectory(_settings.GarbagePath);
                
            _sourceFile.MoveTo(destinationFile);
            _logger.LogInformation($"{Thread.CurrentThread.Name} \t scan copy moved to {destinationFile}");
        }
        private void MoveToHomeDirectory()
        {
            var scanDirectory = $"{_owner.HomeDirectory}\\{_settings.UsersSubDir}";
            var destinationFile = $"{scanDirectory}\\{_generatedFileName}";

            if (!Directory.Exists(scanDirectory))
                Directory.CreateDirectory(scanDirectory);
            
            _sourceFile.MoveTo(destinationFile);
            _logger.LogInformation($"{Thread.CurrentThread.Name} \t scan copy moved to {destinationFile}");
        }
        private bool IsHomeDirectoryValid()
        {
            return !string.IsNullOrEmpty(_owner.HomeDirectory) && Directory.Exists(_owner.HomeDirectory);
        }
        public void Move()
        {
            if(IsHomeDirectoryValid())
            {
                try
                {
                    MoveToHomeDirectory();
                }
                catch (Exception exception)
                {
                    _logger.LogError($"{Thread.CurrentThread.Name} \t {exception.Message}");
                    MoveToGarbage();
                }
            }
            else
            {
                _logger.LogInformation($"{Thread.CurrentThread.Name} \t home directory is invalid");
                MoveToGarbage();
            }
        }

    }
}
