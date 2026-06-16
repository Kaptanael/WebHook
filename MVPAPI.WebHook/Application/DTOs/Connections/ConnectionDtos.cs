using MVPAPI.WebHook.Domain.Enums;

namespace MVPAPI.WebHook.Application.DTOs.Connections;

public record CreateConnectionResponse(bool Success, string Message);

public record CreateConnectionRequest(
    int CompanyId,
    string ApplicationName,
    string MVPApiToken,
    string MVPApiRefreshToken,
    DateTime MVPApiExpiresIn);

public record ConnectionResponse(
    Guid Id,
    int CompanyId,
    string ApplicationName,
    string ClientToken,
    bool IsActive,
    DateTime MVPApiExpiresIn);

public record UpdateConnectionStatusRequest(ConnectionStatus Status);
