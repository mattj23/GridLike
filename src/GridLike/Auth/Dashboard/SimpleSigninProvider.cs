using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace GridLike.Auth.Dashboard;

public class SimpleSigninProvider : ISigninProvider
{
    private readonly Config _config;

    public SimpleSigninProvider(Config config)
    {
        _config = config;
    }

    public record Config(string User, string Password);

    public Task<ClaimsIdentity?> Authenticate(string userName, string password)
    {
        if (userName == _config.User && password == _config.Password)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, userName)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return Task.FromResult<ClaimsIdentity?>(identity);
        }
        
        return Task.FromResult<ClaimsIdentity?>(null);
    }
}