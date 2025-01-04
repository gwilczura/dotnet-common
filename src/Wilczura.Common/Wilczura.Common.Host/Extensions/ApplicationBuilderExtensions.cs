using Azure.Core;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Elastic.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using System.Diagnostics;
using Wilczura.Common.Activities;
using Wilczura.Common.Consts;
using Wilczura.Common.Exceptions;
using Wilczura.Common.Host.Logging;
using Wilczura.Common.Host.Models;
using Wilczura.Common.Models;
using Wilczura.Common.Web.Authorization;
using Wilczura.Common.Web.Client;
using Wilczura.Common.Web.Middleware;

namespace Wilczura.Common.Host.Extensions;

public static class ApplicationBuilderExtensions
{
    public static ILogger GetStartupLogger(this IHostApplicationBuilder app)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
            builder.AddSimpleConsole(config =>
            {
                config.IncludeScopes = false;
                config.SingleLine = true;
            });
        });

        var logger = loggerFactory.CreateLogger<StartupLog>();

        if (logger == null)
        {
            throw new CustomException(nameof(logger));
        }

        logger.LogInformation("Logger created");
        logger.LogInformation("{systemInfo}", SystemInfo.GetInfo());

        return logger;
    }

    public static IHostApplicationBuilder AddCustomHostServices(
        this IHostApplicationBuilder app,
        string configName,
        string mtActivitySourceName,
        AuthenticationType authenticationType,
        AssemblyPart controllersAssemblyPart,
        ILogger? logger = null)
    {
        app.AddConfigurationFromLocalConfig(configName);
        app.AddConfigurationFromKeyVault(configName, logger);

        LogConfigurationSources(app, logger);

        var config = app.Configuration.GetSection(configName);
        app.Services.Configure<CustomOptions>(config);
        var options = new CustomOptions();
        config.Bind(options);

        app.SetupObservability(options, mtActivitySourceName, logger);

        var configRendomEx = app.Configuration.GetSection(RandomExceptionMiddlewareOptions.ConfigurationKey);
        app.Services.Configure<RandomExceptionMiddlewareOptions>(configRendomEx);

        app.AddEntraIdPrincipalProvider(configName);
        app.AddEntraIdAuthentication(configName);
        app.Services.AddControllers(o =>
        {
            switch (authenticationType)
            {
                case AuthenticationType.ApiKey:
                    o.Filters.Add<ApiKeyAuthorizationFilter>();
                    break;
                case AuthenticationType.Default:
                    o.Filters.Add(new AuthorizeFilter());
                    break;
                default:
                    break;
            }
        })
        .ConfigureApplicationPartManager(setupAction =>
        {
            setupAction.ApplicationParts.Clear();
            setupAction.ApplicationParts.Add(controllersAssemblyPart);
        });

        app.Services.AddSingleton<ApiKeyAuthorizationFilter>();
        app.Services.AddSingleton<IApiKeyValidator, ApiKeyValidator>();

        // for intercepting http message send
        app.Services.AddTransient<CustomHttpMessageHandlerBuilder>();
        app.Services.AddTransient<HttpMessageHandlerBuilder>(services =>
        {
            return services.GetRequiredService<CustomHttpMessageHandlerBuilder>();
        });

        return app;
    }

    private static void SetupObservability(
        this IHostApplicationBuilder app,
        CustomOptions options,
        string mtActivitySourceName,
        ILogger? logger)
    {
        //TODO: SHOW P1 - AddAllElasticApm
        if (options.EnableApm)
        {
            app.Services.AddElasticApm();
        }

        //TODO: SHOW P1 - cross system compatibility
        // this is related with traceparent
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        if (!string.IsNullOrWhiteSpace(mtActivitySourceName))
        {
            _ = new CustomActivityListener(mtActivitySourceName);
        }
        _ = new CustomActivityListener(ObservabilityConsts.DefaultListenerName);

        logger?.LogInformation("Disabling default log providers. Enabling ELK.");
        app.Logging.ClearProviders();
        // TODO: SHOW P1 - Add Elasticsearch "logger"
        app.Logging.AddElasticsearch(loggerOptions =>
        {
            loggerOptions.MapCustom = CustomLogMapper.Map;
        });
    }

    private static void LogConfigurationSources(
        IHostApplicationBuilder app,
        ILogger? logger)
    {
        foreach (var source in app.Configuration.Sources)
        {
            if (source is JsonConfigurationSource jsonSource)
            {
                logger?.LogInformation("Source: {name}, {path}", nameof(JsonConfigurationSource), jsonSource.Path);
            }
            else
            {
                logger?.LogInformation("Source: {name}", source.GetType().Name);
            }
        }
    }

    // TODO: SHOW P9 - Add Azure Key Vault for configuration
    public static IHostApplicationBuilder AddConfigurationFromKeyVault(
        this IHostApplicationBuilder app,
        string sectionName,
        ILogger? logger = null)
    {
        var keyVaultName = app.Configuration[CommonConsts.KeyVaultNameKey];
        var servicePrincipalSection = app.Configuration.GetSection(sectionName).GetSection(CommonConsts.ServicePrincipalKey)!;
        var options = new ConfidentialClientApplicationOptions();
        servicePrincipalSection.Bind(options);

        TokenCredential credential =
            !string.IsNullOrWhiteSpace(options?.ClientSecret)
            ? new ClientSecretCredential(options!.TenantId, options.ClientId, options.ClientSecret)
            : new DefaultAzureCredential();

        logger?.LogInformation("KeyVault: {vaultName}, {name}, {clientId}", keyVaultName, credential.GetType().Name, options?.ClientId);

        try
        {
            app.Configuration.AddAzureKeyVault(
                new Uri($"https://{keyVaultName}.vault.azure.net/"),
                credential,
                new AzureKeyVaultConfigurationOptions
                {
                    ReloadInterval = TimeSpan.FromMinutes(5)
                });
        }
        catch (Exception ex)
        {
            logger?.LogError("KeyVault Failure: {message}", ex.Message);
        }

        return app;
    }

    private static IHostApplicationBuilder AddConfigurationFromLocalConfig(
        this IHostApplicationBuilder app,
        string sectionName)
    {
        app.Configuration.AddJsonFile("appsettings.local.json", optional: true);
        return app;
    }
}
