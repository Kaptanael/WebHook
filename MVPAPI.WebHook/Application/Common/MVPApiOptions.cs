namespace MVPAPI.WebHook.Application.Common;

public class MVPApiOptions
{
    public const string SectionName = "MVPApi";

    public string TokenUrl { get; set; } = string.Empty;

    public string RefreshTokenUrl { get; set; } = string.Empty;
}
