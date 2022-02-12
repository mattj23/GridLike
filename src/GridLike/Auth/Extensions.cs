using GridLike.Auth.Api;
using GridLike.Auth.Dashboard;
using GridLike.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace GridLike.Auth;

public static class Extensions
{
    private static void AddApiAuth(this IServiceCollection services, IConfigurationSection config)
    {
        var typeName = config.GetValue<string>("Type").ToLower();
        if (typeName == "simple")
        {
            var configObject = config.Get<SimpleApiChecker.Config>();
            services.AddSingleton(configObject);
            services.AddScoped<IApiKeyChecker, SimpleApiChecker>();
            
        }
        else
        {
            throw new NotSupportedException($"No API authorization supported for type={typeName}");
        }

    }

    private static void UseWorkerAuth(this IServiceCollection services, IConfigurationSection config)
    {
        var typeName = config.GetValue<string>("Type").ToLower();
        if (typeName == "simple")
        {
            var configObject = config.Get<SimpleTokenAuth.Config>();
            services.AddSingleton(configObject);
            services.AddSingleton<IWorkerAuthenticator, SimpleTokenAuth>();
        }
        else
        {
            throw new NotSupportedException($"No worker authentication provider found for type={typeName}");
        }
    }

    public static void AddGridLikeAuthentication(this IServiceCollection services,
        IConfigurationSection config)
    {
        services.AddAuthentication(o => { o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme; })
            .AddCookie()
            .AddScheme<AuthenticationSchemeOptions, ApiKeyHandler>(Schemes.ApiKey, _ => { });

        services.UseWorkerAuth(config.GetSection("Worker"));
        services.AddApiAuth(config.GetSection("Api"));
        
        // Dashboard Authentication Provider
        var dashConfig = config.GetSection("Dashboard");
        var typeName = dashConfig.GetValue<string>("Type").ToLower();
        if (typeName == "simple")
        {
            var configObject = dashConfig.Get<SimpleSigninProvider.Config>();
            services.AddSingleton(configObject);
            services.AddTransient<ISigninProvider, SimpleSigninProvider>();
        }
        else
        {
            throw new NotSupportedException($"No dashboard authentication provider found for type={typeName}");
        }
    }
}