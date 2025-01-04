using Elastic.Apm.Api;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wilczura.Common.Logging;
using Wilczura.Common.Models;

namespace Wilczura.Common.ServiceBus;

public class PublishFilter<T> : ObservingFilter<PublishContext<T>> where T : class
{
    public PublishFilter(
        ILogger<PublishFilter<T>> logger,
        IOptionsSnapshot<CustomOptions> customOptions) : base(logger, customOptions)
    {
    }

    protected override string Message => "Message publish";

    protected override string ActivityName => "message-publish";

    protected override EventId Event => LogEvents.Custom;

    protected override DistributedTracingData? GetDistributedTracingData(PublishContext<T> context)
    {
        return null;
    }

    protected override string GetEndpoint(PublishContext<T> context)
    {
        return $"{context.DestinationAddress?.LocalPath}";
    }
}
