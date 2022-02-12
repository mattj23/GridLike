using System.Security.Claims;

namespace GridLike.Auth.Dashboard;

public interface ISigninProvider
{
    Task<ClaimsIdentity?> Authenticate(string userName, string password);
}