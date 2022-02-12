using GridLike.Data.Models;

namespace GridLike.Auth.Api;

public class SimpleApiChecker : IApiKeyChecker
{
    private readonly Config _config;

    public SimpleApiChecker(Config config)
    {
        _config = config;
    }

    public Task<ApiKey?> Check(string key)
    {
        if (key == _config.Key)
        {
            var result = new ApiKey
            {
                Owner = "Simple API User"
            };
            return Task.FromResult<ApiKey?>(result);
        }

        return Task.FromResult<ApiKey?>(null);
    }

    public record Config
    {
        public string Key { get; init; } = null!;
    }
}