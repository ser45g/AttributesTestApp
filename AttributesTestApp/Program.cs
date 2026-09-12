using AttributesTestApp;
using AttributesTestApp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var app = Host.CreateDefaultBuilder(args).ConfigureServices(services =>
{
    services.AddScoped<IDateTimeProvider, DateTimeProvider>();
}).ConfigureHostConfiguration(config =>
{
    config.AddJsonFile("appsettings.json");
}).Build();

var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();

var engine = CommandEngine.Create(scopeFactory);

await app.StartAsync();

await engine.Run(args);