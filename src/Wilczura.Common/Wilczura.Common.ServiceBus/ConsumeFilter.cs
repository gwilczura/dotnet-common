using Elastic.Apm.Api;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wilczura.Common.Logging;
using Wilczura.Common.Models;
using Wilczura.Common.ServiceBus.Consts;

namespace Wilczura.Common.ServiceBus;

public class ConsumeFilter<T> : ObservingFilter<ConsumeContext<T>> where T : class
{
    public ConsumeFilter(
        ILogger<ConsumeFilter<T>> logger,
        IOptionsSnapshot<CustomOptions> customOptions) : base(logger, customOptions)
    {
    }

    protected override string Message => "Message consume";

    protected override string ActivityName => "message-consume";

    protected override EventId Event => LogEvents.ConsumeMessage;

    protected override DistributedTracingData? GetDistributedTracingData(ConsumeContext<T> context)
    {
        var traceParentHeader = context.GetHeader(HeaderNames.Traceparent);
        DistributedTracingData? currentTraceData = null;

        if (!string.IsNullOrWhiteSpace(traceParentHeader))
        {
            currentTraceData = DistributedTracingData.TryDeserializeFromString(traceParentHeader);
        }

        return currentTraceData;
    }

    protected override string GetEndpoint(ConsumeContext<T> context)
    {
        return $"{context.DestinationAddress?.LocalPath}";
    }
}