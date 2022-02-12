using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace GridLike.Auth.Api;

public class ApiKeyHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IApiKeyChecker _checker;
    
    public ApiKeyHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, 
        ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, IApiKeyChecker checker) : base(options, logger, encoder, clock)
    {
        _checker = checker;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!this.Request.Headers.TryGetValue("X-API-KEY", out var values)) return AuthenticateResult.NoResult();

        var key = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(key)) return AuthenticateResult.NoResult();

        var result = await _checker.Check(key);
        if (result is null) return AuthenticateResult.NoResult();

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, result.Owner),
            new(ClaimTypes.Role, Policies.ApiUser)
        };

        var identities = new List<ClaimsIdentity> { new(claims, Schemes.ApiKey) };
        var principal = new ClaimsPrincipal(identities);
        var ticket = new AuthenticationTicket(principal, Schemes.ApiKey);
        return AuthenticateResult.Success(ticket);
    }
}