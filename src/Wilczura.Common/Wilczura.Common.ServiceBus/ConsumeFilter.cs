using MassTransit;
using Microsoft.Extensions.Logging;
using Wilczura.Common.Logging;

namespace Wilczura.Common.ServiceBus;

public class ConsumeFilter<T> : ObservingFilter<ConsumeContext<T>> where T : class
{
    public ConsumeFilter(ILogger<ConsumeFilter<T>> logger) : base(logger)
    {
    }

    protected override string Message => "Message consume";

    protected override string ActivityName => "message-consume";

    protected override EventId Event => LogEvents.ConsumeMessage;

    protected override string GetEndpoint(ConsumeContext<T> context)
    {
        return $"{context.DestinationAddress?.LocalPath}";
    }
}