using Elastic.Apm;
using Microsoft.Extensions.Logging;
using Wilczura.Common.Consts;
using Wilczura.Common.Logging;
using Wilczura.Common.Models;

namespace Wilczura.Common.Web.Client;

public class CustomLoggingHttpMessageHandler : HttpClientHandler
{
    private readonly ILogger<CustomLoggingHttpMessageHandler> _logger;
    private readonly CustomOptions _options;

    public CustomLoggingHttpMessageHandler(
        ILogger<CustomLoggingHttpMessageHandler> logger,
        CustomOptions options)
    {
        _logger = logger;
        _options = options;
    }

    //TODO: SHOW P3 - HttpMessageHandler Logging (HttpClient)
    protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Exception? exception = null;
        HttpResponseMessage? responseMessage = null;
        var message = "Http out";
        var activityName = "http-out";
        var logInfo = new LogInfo(message, ObservabilityConsts.EventCategoryWeb);
        var eventId = LogEvents.WebRequest;
        logInfo.HttpMethod = request.Method.Method;
        logInfo.Endpoint = request.RequestUri?.LocalPath;
        logInfo.EventAction = activityName;

        async Task<HttpResponseMessage> action()
        {
            var logScope = new LogScope(_logger, logInfo, LogLevel.Information, eventId, activityName: activityName);
            try
            {
                responseMessage = await base.SendAsync(request, cancellationToken);
                return responseMessage;
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                if (exception != null)
                {
                    logInfo.ApplyException(exception);
                }
                else if (responseMessage?.IsSuccessStatusCode == false)
                {
                    logInfo.EventReason = responseMessage.ReasonPhrase;
                    logInfo.EventOutcome = ObservabilityConsts.EventOutcomeFailure;
                }
                else
                {
                    logInfo.EventOutcome = ObservabilityConsts.EventOutcomeSuccess;
                }

                logScope.Dispose();
            }
        }

        if (_options.EnableApm)
        {
            // TODO: distributed traces not working
            return await Agent.Tracer.CaptureTransaction(activityName, eventId.Name, action, null);
        }
        else
        {
            return await action();
        }
    }
}
