using GridLike.Data.Models;

namespace GridLike.Auth.Api;

public interface IApiKeyChecker
{
    Task<ApiKey?> Check(string key);
}