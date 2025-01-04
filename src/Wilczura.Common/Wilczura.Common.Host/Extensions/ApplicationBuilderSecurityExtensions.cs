using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Wilczura.Common.Consts;
using Wilczura.Common.Host.Security;
using Wilczura.Common.Security;

namespace Wilczura.Common.Host.Extensions;

public static class ApplicationBuilderSecurityExtensions
{
    public static IHostApplicationBuilder AddEntraIdPrincipalProvider(
        this IHostApplicationBuilder app,
        string sectionName)
    {
        var servicePrincipalSection = app.Configuration.GetSection(sectionName).GetSection(CommonConsts.ServicePrincipalKey)!;
        app.Services.Configure<ConfidentialClientApplicationOptions>(servicePrincipalSection);
        app.Services.AddSingleton<ICustomPrincipalProvider, CustomPrincipalProvider>();
        return app;
    }

    public static IHostApplicationBuilder AddEntraIdAuthentication(
        this IHostApplicationBuilder app,
        string sectionName,
        ILogger? logger)
    {
        var servicePrincipalSection = app.Configuration.GetSection(sectionName).GetSection(CommonConsts.ServicePrincipalKey)!;
        if (servicePrincipalSection?.Value == null)
        {
            logger?.LogInformation("Entra Auth - disabled");
        }
        else
        {
            logger?.LogInformation("Entra Auth - enabled");
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
                    // TODO: ObservabilityConsts.ApplicationNameClaim
                    options.TokenValidationParameters.NameClaimType = "appid";
                });
        }

        return app;
    }
}
