using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Wilczura.Common.Host.Extensions;
using Wilczura.Common.Host.Models;
using Wilczura.Demo.Adapters.Controllers;
using Wilczura.Observability.Common.Host.Extensions;

var builder = WebApplication.CreateBuilder(args);
var configName = "Demo";
// Add services to the container.
var logger = builder.GetStartupLogger();
builder.AddCustomHostServices(
    configName,
    mtActivitySourceName: string.Empty,
    AuthenticationType.ApiKey,
    new AssemblyPart(typeof(HealthController).Assembly),
    logger);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseObservabilityDefaults();

app.Run();

// needed for integration tests
public partial class Program { }