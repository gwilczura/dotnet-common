using Azure.Core;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Elastic.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using System.Diagnostics;
using Wilczura.Common.Activities;
using Wilczura.Common.Consts;
using Wilczura.Common.Host.Logging;
using Wilczura.Common.Host.Models;
using Wilczura.Common.Models;
using Wilczura.Common.Security;
using Wilczura.Common.Web.Authorization;
using Wilczura.Common.Web.Client;
using Wilczura.Common.Web.Middleware;

namespace Wilczura.Common.Host.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddCustomHostServices(
        this IHostApplicationBuilder app,
        string configName,
        string mtActivitySourceName,
        AuthenticationType authenticationType,
        AssemblyPart controllersAssemblyPart)
    {
        app.AddConfigurationFromKeyVault(configName);
        //TODO: SHOW P1 - AddAllElasticApm
        //app.Services.AddAllElasticApm();

        //TODO: SHOW P1 - cross system compatibility
        // this is related with traceparent
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        if (!string.IsNullOrWhiteSpace(mtActivitySourceName))
        {
            _ = new CustomActivityListener(mtActivitySourceName);
        }
        _ = new CustomActivityListener(ObservabilityConsts.DefaultListenerName);

        var configRendomEx = app.Configuration.GetSection(RandomExceptionMiddlewareOptions.ConfigurationKey);
        app.Services.Configure<RandomExceptionMiddlewareOptions>(configRendomEx);
        var config = app.Configuration.GetSection(configName);
        app.Services.Configure<CustomOptions>(config);

        app.AddPrincipal(configName);
        app.AddEntraIdAuthentication(configName);
        app.Logging.ClearProviders();
        // TODO: SHOW P1 - Add Elasticsearch "logger"
        app.Logging.AddElasticsearch(loggerOptions =>
        {
            loggerOptions.MapCustom = CustomLogMapper.Map;
        });
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

    // TODO: SHOW P9 - Add Azure Key Vault for configuration
    public static IHostApplicationBuilder AddConfigurationFromKeyVault(
        this IHostApplicationBuilder app, 
        string sectionName)
    {
        var keyVaultName = app.Configuration[CommonConsts.KeyVaultNameKey];
        var servicePrincipalSection = app.Configuration.GetSection(sectionName).GetSection(CommonConsts.ServicePrincipalKey)!;
        var options = new ConfidentialClientApplicationOptions();
        servicePrincipalSection.Bind(options);
        TokenCredential credential =
            !string.IsNullOrWhiteSpace(options?.ClientSecret)
            ? new ClientSecretCredential(options!.TenantId, options.ClientId, options.ClientSecret)
            : new DefaultAzureCredential();
        app.Configuration.AddAzureKeyVault(
            new Uri($"https://{keyVaultName}.vault.azure.net/"),
            credential,
            new AzureKeyVaultConfigurationOptions
            {
                ReloadInterval = TimeSpan.FromMinutes(5)
            });
        return app;
    }

    // TODO: SHOW P9 - Add Principal
    public static IHostApplicationBuilder AddPrincipal(
        this IHostApplicationBuilder app, string sectionName)
    {
        var servicePrincipalSection = app.Configuration.GetSection(sectionName).GetSection(CommonConsts.ServicePrincipalKey)!;
        app.Services.Configure<ConfidentialClientApplicationOptions>(servicePrincipalSection);
        app.Services.AddSingleton<ICustomPrincipalProvider, CustomPrincipalProvider>();
        return app;
    }

    // TODO: SHOW P9 - Add Authentication
    public static IHostApplicationBuilder AddEntraIdAuthentication(
        this IHostApplicationBuilder app, string sectionName)
    {
        var servicePrincipalSection = app.Configuration.GetSection(sectionName).GetSection(CommonConsts.ServicePrincipalKey)!;
        app.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(servicePrincipalSection, subscribeToJwtBearerMiddlewareDiagnosticsEvents: false);
        app.Services.Configure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            options =>
            {
                options.Events.OnTokenValidated = async (context) =>
                {
                    await Task.CompletedTask;
                    //TODO: why is this needed?
                    // withouth this code user is not set on unauthenticated controller call
                };
                options.Events.OnAuthenticationFailed = async (context) =>
                {
                    await Task.CompletedTask;
                };

                // TODO: replace with ApplicationNameClaim
                options.TokenValidationParameters.NameClaimType = "appid";
            });
        return app;
    }
}
