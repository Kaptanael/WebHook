namespace MVPAPI.WebHook.Application.Common.Options;

public class MVPApiOptions
{
    public const string SectionName = "MVPApi";

    // Base URL of the MVP API.
    public string BaseUrl { get; set; } = string.Empty;

    // URL to obtain an access token from the MVP API.
    public string TokenUrl { get; set; } = string.Empty;

    // URL to refresh an access token from the MVP API.
    public string RefreshTokenUrl { get; set; } = string.Empty;
}
