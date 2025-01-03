using MassTransit;
using Microsoft.Extensions.Logging;
using Wilczura.Common.Consts;
using Wilczura.Common.Logging;

namespace Wilczura.Common.ServiceBus;

public abstract class ObservingFilter<TContext> : IFilter<TContext>
        where TContext : class, PipeContext
{
    protected abstract string Message { get; }
    protected abstract string ActivityName { get; }
    protected abstract EventId Event { get; }

    private readonly ILogger<ObservingFilter<TContext>> _logger;

    public ObservingFilter(ILogger<ObservingFilter<TContext>> logger)
    {
        _logger = logger;
    }

    protected abstract string GetEndpoint(TContext context);

    public void Probe(ProbeContext context)
    {
    }

    public async Task Send(TContext context, IPipe<TContext> next)
    {
        Exception? exception = null;
        var message = Message;
        var activityName = ActivityName;
        var logInfo = new LogInfo(message, ObservabilityConsts.EventCategoryProcess);
        logInfo.Endpoint = GetEndpoint(context);
        logInfo.EventAction = activityName;
        var logScope = new LogScope(_logger, logInfo, LogLevel.Information, Event, activityName: activityName);
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
