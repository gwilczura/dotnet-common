using Elastic.Apm.Api;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wilczura.Common.Logging;
using Wilczura.Common.Models;

namespace Wilczura.Common.ServiceBus;

public class SendFilter<T> : ObservingFilter<SendContext<T>> where T : class
{
    public SendFilter(
        ILogger<SendFilter<T>> logger,
        IOptionsSnapshot<CustomOptions> customOptions) : base(logger, customOptions)
    {
    }

    protected override string Message => "Message send";

    protected override string ActivityName => "message-send";

    protected override EventId Event => LogEvents.Custom;

    protected override DistributedTracingData? GetDistributedTracingData(SendContext<T> context)
    {
        return null;
    }

    protected override string GetEndpoint(SendContext<T> context)
    {
        return $"{context.DestinationAddress?.LocalPath}";
    }
}
