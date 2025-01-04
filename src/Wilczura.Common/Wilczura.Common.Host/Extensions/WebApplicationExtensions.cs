using Microsoft.AspNetCore.Builder;
using Wilczura.Common.Web.Middleware;

namespace Wilczura.Observability.Common.Host.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseObservabilityDefaults(this WebApplication app)
    {
        app.UseRequestLogging();
        app.UseHttpsRedirection();
        app.UseRandomException();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        
        return app;
    }

    public static IApplicationBuilder UseRandomException(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RandomExceptionMiddleware>();
    }

    // Use Request Logging (ASP.NET)
    public static IApplicationBuilder UseRequestLogging(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<LoggingMiddleware>();
    }
}
