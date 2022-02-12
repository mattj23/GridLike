using System;
using System.IO;
using System.Threading.Tasks;
using Minio;
using Minio.Exceptions;

namespace GridLike.Services.Storage
{
    public class MinioConfig
    {
        public string Endpoint {get; set; } = null!;
        public string AccessKey {get; set; } = null!;
        public string SecretKey { get; set; } = null!;
        public string Bucket { get; set; } = null!;
        public bool Ssl { get; set; }
    }
    
    public class MinioProvider : IStorageProvider
    {
        private readonly string _endpoint;
        private readonly string _accessKey;
        private readonly string _secretKey;
        private readonly string _bucket;
        private readonly bool _useSsl;

        public MinioProvider(MinioConfig config)
        {
            _endpoint = config.Endpoint;
            _accessKey = config.AccessKey;
            _secretKey = config.SecretKey;
            _useSsl = config.Ssl;
            _bucket = config.Bucket;
        }

        public async Task PutFile(string fileName, Stream data, long size)
        {
            try
            {
                var client = GetClient();
                await client.PutObjectAsync(_bucket, fileName, data, size);
            }
            catch (UnexpectedMinioException e)
            {
                throw new IOException(e.Message);
            }
        }

        public async Task DeleteFile(string fileName)
        {
            try
            {
                var client = GetClient();
                await client.RemoveObjectAsync(_bucket, fileName);
            }
            catch (UnexpectedMinioException e)
            {
                throw new IOException(e.Message);
            }
        }

        public async Task GetFile(string fileName, Action<Stream> streamCallback)
        {
            try
            {
                var client = GetClient();
                await client.GetObjectAsync(_bucket, fileName, streamCallback);
            }
            catch (UnexpectedMinioException e)
            {
                throw new IOException(e.Message);
            }
        }

        private MinioClient GetClient()
        {
            if (_useSsl)
                return new MinioClient(_endpoint, _accessKey, _secretKey).WithSSL();
            
            return new MinioClient(_endpoint, _accessKey, _secretKey);
        }
    }
}