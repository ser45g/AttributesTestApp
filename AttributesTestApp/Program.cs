using AttributesTestApp;
using AttributesTestApp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings() 
{ 
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Services.AddScoped<IDateTimeProvider, DateTimeProvider>();

var app = builder.Build();

var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();

var engine = CommandEngine.Create(scopeFactory);

await app.StartAsync();


await engine.Run(args);