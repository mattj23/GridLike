using GridLike.Workers;

namespace GridLike.Services;

public interface IWorkerAuthenticator
{
    Task<bool> Authenticate(RegisterMessage message);

}