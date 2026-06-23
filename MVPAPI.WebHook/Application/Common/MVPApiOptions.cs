namespace MVPAPI.WebHook.Application.Common;

public class MVPApiOptions
{
    public const string SectionName = "MVPApi";

    /// <summary>Base URL of the MVP API, embedded in auto-provisioned client tokens.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    public string TokenUrl { get; set; } = string.Empty;

    public string RefreshTokenUrl { get; set; } = string.Empty;
}
