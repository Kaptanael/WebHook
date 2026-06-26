using MVPAPI.WebHook.Application.Common;

namespace MVPAPI.WebHook.Application.Interfaces.Inbound;

public enum InboundAuthOutcome
{
    NotPresented,
    Authenticated,
    Rejected
}

public sealed record InboundAuthResult(InboundAuthOutcome Outcome, int CompanyId = 0, string? apiKey = null, string? ApplicationName = null, string? Error = null)
{
    public static InboundAuthResult NotPresented() => new(InboundAuthOutcome.NotPresented);
    public static InboundAuthResult Authenticated(int companyId, string apiKey, string applicationName) => new(InboundAuthOutcome.Authenticated, companyId, apiKey, applicationName);
    public static InboundAuthResult Rejected(string error) => new(InboundAuthOutcome.Rejected, Error: error);
}

public interface IInboundAuthenticator
{
    Task<Result<InboundAuthResult>> AuthenticateAsync(InboundRequest request, CancellationToken cancellationToken = default);
}
