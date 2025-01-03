using Elastic.Apm;
using Elastic.Apm.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Wilczura.Common.Consts;
using Wilczura.Common.Logging;
using Wilczura.Common.Models;
using Wilczura.Common.Web.Extensions;

namespace Wilczura.Common.Web.Middleware;

public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingMiddleware> _logger;
    private readonly CustomOptions _customOptions;

    public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger, CustomOptions customOptions)
    {
        _next = next;
        _logger = logger;
        _customOptions = customOptions;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        Exception? exception = null;
        var message = "Http in";
        var activityName = "http-in";
        var logInfo = new LogInfo(message, ObservabilityConsts.EventCategoryWeb);

        Func<Task> action = async () =>
        {
            logInfo.EventAction = activityName;
            var logScope = new LogScope(_logger, logInfo, LogLevel.Information, LogEvents.WebRequest, activityName: activityName);
            logInfo.HttpMethod = context.Request.Method;
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                logInfo.EventReason = $"HTTP Code {context.Response.StatusCode}";

                if (exception != null)
                {
                    logInfo.ApplyException(exception);
                }
                else if (context.Response.StatusCode >= 400)
                {
                    logInfo.EventOutcome = ObservabilityConsts.EventOutcomeFailure;
                }
                else if (context.Response.StatusCode >= 300)
                {
                    logInfo.EventOutcome = ObservabilityConsts.EventOutcomeUnknown;
                }
                else
                {
                    logInfo.EventOutcome = ObservabilityConsts.EventOutcomeSuccess;
                }

                logInfo.ApplyPrincipal(context.GetPrincipal());

                logScope.Dispose();
            }
        };

        if (_customOptions.EnableApm)
        {
            var containsTraceParentHeader =
                context.Request.Headers.TryGetValue(HeaderNames.TraceParent, out var traceParentHeader);
            DistributedTracingData? currentTraceData = null;

            if (containsTraceParentHeader)
            {
                currentTraceData = DistributedTracingData.TryDeserializeFromString(traceParentHeader);
            }

            await Agent.Tracer.CaptureTransaction(activityName, nameof(LogEvents.WebRequest), action, currentTraceData);
        }
        else
        {
            await action();
        }
    }
}