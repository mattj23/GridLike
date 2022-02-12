using System;
using System.IO;
using System.Threading.Tasks;

namespace GridLike.Services.Storage
{
    public interface IStorageProvider
    {
        Task PutFile(string fileName, Stream data, long size);
        Task DeleteFile(string fileName);
        Task GetFile(string fileName, Action<Stream> streamCallback);
    }
}