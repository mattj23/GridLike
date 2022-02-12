using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;

namespace GridLike.Services.Storage
{
    public class FilesystemConfig
    {
        public string Path { get; set; }
    }
    
    public class FilesystemProvider : IStorageProvider
    {
        private readonly string _path;
        private readonly ILogger<FilesystemProvider> _logger;

        public FilesystemProvider(FilesystemConfig config, ILogger<FilesystemProvider> logger)
        {
            _path = config.Path;
            _logger = logger;
        }

        public async Task PutFile(string fileName, Stream data, long size)
        {
            var target = Path.Combine(_path, fileName);
            var directory = Path.GetDirectoryName(target);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            await using var stream = File.Open(target, FileMode.Create);
            data.Seek(0, SeekOrigin.Begin);
            await data.CopyToAsync(stream);
        }

        public Task DeleteFile(string fileName)
        {
            File.Delete(fileName);
            return Task.CompletedTask;
        }

        public async Task GetFile(string fileName, Action<Stream> streamCallback)
        {
            var target = Path.Combine(_path, fileName);
            await using var stream = File.Open(target, FileMode.Open);
            streamCallback(stream);
        }
    }
}