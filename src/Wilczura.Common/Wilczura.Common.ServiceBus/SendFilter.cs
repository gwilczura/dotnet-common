using MassTransit;
using Microsoft.Extensions.Logging;
using Wilczura.Common.Logging;

namespace Wilczura.Common.ServiceBus;

public class SendFilter<T> : ObservingFilter<SendContext<T>> where T : class
{
    public SendFilter(ILogger<SendFilter<T>> logger) : base(logger)
    {
    }

    protected override string Message => "Message send";

    protected override string ActivityName => "message-send";

    protected override EventId Event => LogEvents.Custom;

    protected override string GetEndpoint(SendContext<T> context)
    {
        return $"{context.DestinationAddress?.LocalPath}";
    }
}
