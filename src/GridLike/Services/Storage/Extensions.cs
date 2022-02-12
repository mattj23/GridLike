using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GridLike.Services.Storage
{
    public static class Extensions
    {
        public static void AddStorage(this IServiceCollection services, IConfigurationSection config)
        {
            var providerType = config.GetValue<string>("Type").ToLower();

            switch (providerType)
            {
                case "s3":
                {
                    var configObject = config.Get<MinioConfig>();
                    services.AddSingleton(configObject);
                    services.AddTransient<IStorageProvider, MinioProvider>();
                    break;
                }
                case "filesystem":
                {
                    var configObject = config.Get<FilesystemConfig>();
                    services.AddSingleton(configObject);
                    services.AddTransient<IStorageProvider, FilesystemProvider>();
                    break;
                }
                default:
                    throw new NotSupportedException($"No storage provider backend for type={providerType}");
            }
        }
        
    }
}