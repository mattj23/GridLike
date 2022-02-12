using GridLike.Workers;

namespace GridLike.Services;

public class SimpleTokenAuth : IWorkerAuthenticator
{
    private readonly Config _config;
    private readonly ILogger<SimpleTokenAuth> _logger;

    public SimpleTokenAuth(Config config, ILogger<SimpleTokenAuth> logger)
    {
        _config = config;
        _logger = logger;
    }

    public Task<bool> Authenticate(RegisterMessage message)
    {
        bool result = message.Token == _config.Token;
        _logger.LogDebug("Authentication result for {0} is {1}", message.Name, result);
        return Task.FromResult(result);
    }

    public record Config
    {
        public string Token { get; init; }
    }
}