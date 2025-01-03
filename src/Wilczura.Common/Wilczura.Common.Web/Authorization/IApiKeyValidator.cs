namespace Wilczura.Common.Web.Authorization;

public interface IApiKeyValidator
{
    bool IsValid(string apiKey);
}
