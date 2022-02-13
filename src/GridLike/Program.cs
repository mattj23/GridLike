using GridLike.Auth;
using GridLike.Auth.Api;
using GridLike.Models;
using GridLike.Services;
using GridLike.Services.Storage;
using GridLike.Workers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Creat startup logging
using var loggerFactory = LoggerFactory.Create(b =>
{
    b.SetMinimumLevel(LogLevel.Information);
    b.AddConsole();
    b.AddEventSourceLogger();
});
var logger = loggerFactory.CreateLogger("Startup");

// SSL Offloading option
if (builder.Configuration.GetValue<bool>("SslOffload"))
{
    logger.LogInformation("SSL Offloading enabled, configuring header forwarding");
    builder.Services.Configure<ForwardedHeadersOptions>(o =>
    {
        o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        o.KnownNetworks.Clear();
        o.KnownProxies.Clear();
    });
}

// ====================================================================================================================
// Add services to the container.
// ====================================================================================================================
// Server Configuration Options
var serverConfig = builder.Configuration.GetSection("ServerOptions").GetServerConfiguration();
builder.Services.AddSingleton(serverConfig);

// Storage and database
// ====================================================================================================================
builder.Services.UseDatabase(builder.Configuration.GetSection("Database"));
builder.Services.AddStorage(builder.Configuration.GetSection("Storage"));

// Authentication
// ====================================================================================================================
builder.Services.AddHttpContextAccessor();

var authConfig = builder.Configuration.GetSection("Authentication");
builder.Services.AddGridLikeAuthentication(authConfig);

// Worker and job
// ====================================================================================================================
builder.Services.AddSingleton<JobDataStore>();
builder.Services.AddSingleton<JobDispatcher>();
builder.Services.AddSingleton<WorkerManager>();
builder.Services.AddSingleton<IHostedService, WorkerManager>(s => s.GetService<WorkerManager>()!);

// Web interface
// ====================================================================================================================
builder.Services.AddMudServices();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// ====================================================================================================================
// Build the app and middleware pipeline
// ====================================================================================================================
var app = builder.Build();

// Ensure the database is created, up to date, and consistent with the static configuration
app.ApplyMigrations();
app.ApplyBaseData();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// HTTPS Redirection
if (!builder.Configuration.GetValue<bool>("DisableHttpsRedirect"))
{
    logger.LogInformation("Enabling HTTPS redirection");
    app.UseHttpsRedirection();
}
else
{
    logger.LogInformation("Disabling HTTPS redirection");
}

app.UseStaticFiles();
app.UseRouting();
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(120),
});

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(e =>
{
    e.MapControllers();
    e.MapBlazorHub();
    e.MapFallbackToPage("/_Host");
});

app.Run();