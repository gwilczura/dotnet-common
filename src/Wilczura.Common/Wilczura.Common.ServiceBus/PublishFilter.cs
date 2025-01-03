using MassTransit;
using Microsoft.Extensions.Logging;
using Wilczura.Common.Logging;

namespace Wilczura.Common.ServiceBus;

public class PublishFilter<T> : ObservingFilter<PublishContext<T>> where T : class
{
    public PublishFilter(ILogger<PublishFilter<T>> logger) : base(logger)
    {
    }

    protected override string Message => "Message publish";

    protected override string ActivityName => "message-publish";

    protected override EventId Event => LogEvents.Custom;

    protected override string GetEndpoint(PublishContext<T> context)
    {
        return $"{context.DestinationAddress?.LocalPath}";
    }
}
