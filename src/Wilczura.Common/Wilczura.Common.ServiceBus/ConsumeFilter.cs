﻿using MassTransit;
using Microsoft.Extensions.Logging;
using Wilczura.Common.Consts;
using Wilczura.Common.Logging;

namespace Wilczura.Common.ServiceBus;

public class ConsumeFilter<T> : IFilter<ConsumeContext<T>> where T : class
{
    private readonly ILogger<ConsumeFilter<T>> _logger;

    public ConsumeFilter(ILogger<ConsumeFilter<T>> logger)
    {
        _logger = logger;
    }

    public void Probe(ProbeContext context)
    {
    }

    // TODO: SHOW P3 - MassTransit filter based logging (Send)
    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        Exception? exception = null;
        var message = "Message consume";
        var activityName = "message-consume";
        var logInfo = new LogInfo(message, ObservabilityConsts.EventCategoryProcess);
        logInfo.Endpoint = $"{context.DestinationAddress?.LocalPath}";
        logInfo.EventAction = activityName;
        var logScope = new LogScope(_logger, logInfo, LogLevel.Information, LogEvents.WebRequest, activityName: activityName);
        try
        {
            await next.Send(context);
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
            else
            {
                logInfo.EventOutcome = ObservabilityConsts.EventOutcomeSuccess;
            }

            logScope.Dispose();
        }
    }
}
