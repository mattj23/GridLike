using System.Security.Claims;
using GridLike.Auth.Dashboard;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace GridLike.Controllers;

[ApiController]
public class AuthController : ControllerBase
{
    private readonly ISigninProvider _provider;

    public AuthController(ISigninProvider provider)
    {
        _provider = provider;
    }

    [HttpPost]
    [Route("api/auth/signin")]
    public async Task<ActionResult> SignInPost(Data value)
    {
        var identity = await _provider.Authenticate(value.User, value.Password);
        if (identity is null)
        {
            return Ok();
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity), 
            new AuthenticationProperties
            {
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10),
                IsPersistent = true
            });

        return this.Ok();
    }

    [HttpPost]
    [Route("api/auth/signout")]
    public async Task<ActionResult> SignOutPost()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }

    public class Data
    {
        public string User { get; set; }
        public string Password { get; set; }
    }
}
