using Microsoft.Extensions.Options;
using Wilczura.Common.Exceptions;
using Wilczura.Common.Models;

namespace Wilczura.Common.Web.Authorization;

public class ApiKeyValidator : IApiKeyValidator
{
    string ApiKey;
    public ApiKeyValidator(IOptions<CustomOptions> options)
    {
        ApiKey = options.Value.ApiKey ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new CustomException("ApiKey can not be empty");
        }
    }

    public bool IsValid(string apiKey)
    {
        return apiKey?.ToLower() == ApiKey.ToLower();
    }
}
