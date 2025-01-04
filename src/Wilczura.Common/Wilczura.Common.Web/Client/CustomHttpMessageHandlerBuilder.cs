using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wilczura.Common.Models;

namespace Wilczura.Common.Web.Client;

public class CustomHttpMessageHandlerBuilder : HttpMessageHandlerBuilder
{
    private readonly ILogger<CustomLoggingHttpMessageHandler> _logger;
    private readonly CustomOptions _options;

    public override string? Name { get; set; }
    public override HttpMessageHandler PrimaryHandler { get; set; }
    public override IList<DelegatingHandler> AdditionalHandlers => new List<DelegatingHandler>();

    public CustomHttpMessageHandlerBuilder(
        ILogger<CustomLoggingHttpMessageHandler> logger,
        IOptionsSnapshot<CustomOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        PrimaryHandler = new CustomLoggingHttpMessageHandler(logger, _options);
    }

    public override HttpMessageHandler Build()
    {
        return new CustomLoggingHttpMessageHandler(_logger, _options);
    }
}
