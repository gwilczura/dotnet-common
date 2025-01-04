using Elastic.Apm;
using Elastic.Apm.Api;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wilczura.Common.Consts;
using Wilczura.Common.Logging;
using Wilczura.Common.Models;

namespace Wilczura.Common.ServiceBus;

public abstract class ObservingFilter<TContext> : IFilter<TContext>
        where TContext : class, PipeContext
{
    protected abstract string Message { get; }
    protected abstract string ActivityName { get; }
    protected abstract EventId Event { get; }

    private readonly ILogger<ObservingFilter<TContext>> _logger;
    private readonly CustomOptions _customOptions;

    public ObservingFilter(
        ILogger<ObservingFilter<TContext>> logger,
        IOptionsSnapshot<CustomOptions> customOptions)
    {
        _logger = logger;
        _customOptions = customOptions.Value;
    }

    protected abstract string GetEndpoint(TContext context);

    protected abstract DistributedTracingData? GetDistributedTracingData(TContext context);

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

        async Task action()
        {
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

        if (_customOptions.EnableApm)
        {
            // TODO: distributed traces not working
            var currentTraceData = GetDistributedTracingData(context);

            await Agent.Tracer.CaptureTransaction(activityName, Event.Name, action, currentTraceData);
        }
        else
        {
            await action();
        }
    }
}
