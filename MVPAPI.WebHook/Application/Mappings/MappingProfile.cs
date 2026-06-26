using AutoMapper;
using MVPAPI.WebHook.Application.DTOs.Connections;
using MVPAPI.WebHook.Application.DTOs.Outbounds;
using MVPAPI.WebHook.Application.DTOs.Events;
using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<WebhookInbound, WebhookInboundResponse>()
            .ForCtorParam(nameof(WebhookInboundResponse.Id), o => o.MapFrom(s => s.Id))
            .ForCtorParam(nameof(WebhookInboundResponse.WebhookId), o => o.MapFrom(s => s.WebhookId))
            .ForCtorParam(nameof(WebhookInboundResponse.Provider), o => o.MapFrom(s => s.Provider))
            .ForCtorParam(nameof(WebhookInboundResponse.EventType), o => o.MapFrom(s => s.EventType))
            .ForCtorParam(nameof(WebhookInboundResponse.Status), o => o.MapFrom(s => s.Status))
            .ForCtorParam(nameof(WebhookInboundResponse.Attempts), o => o.MapFrom(s => s.Attempts))
            .ForCtorParam(nameof(WebhookInboundResponse.LastError), o => o.MapFrom(s => s.LastError))
            .ForCtorParam(nameof(WebhookInboundResponse.ReceivedAtUtc), o => o.MapFrom(s => s.ReceivedAtUtc))
            .ForCtorParam(nameof(WebhookInboundResponse.NextAttemptAtUtc), o => o.MapFrom(s => s.NextAttemptAtUtc))
            .ForCtorParam(nameof(WebhookInboundResponse.ProcessedAtUtc), o => o.MapFrom(s => s.ProcessedAtUtc));

        CreateMap<WebhookOutbound, WebhookOutboundResponse>()
            .ForCtorParam(nameof(WebhookOutboundResponse.Id), o => o.MapFrom(s => s.Id))
            .ForCtorParam(nameof(WebhookOutboundResponse.EndpointToken), o => o.MapFrom(s => s.EndPointToken))
            .ForCtorParam(nameof(WebhookOutboundResponse.Endpoint), o => o.MapFrom(s => s.Endpoint))
            .ForCtorParam(nameof(WebhookOutboundResponse.CompanyId), o => o.MapFrom(s => s.CompanyId))
            .ForCtorParam(nameof(WebhookOutboundResponse.TriggerJson), o => o.MapFrom(s => s.TriggerConfigJson));

        CreateMap<WebHookConnection, ConnectionResponse>()
            .ForCtorParam(nameof(ConnectionResponse.Id), o => o.MapFrom(s => s.Id))
            .ForCtorParam(nameof(ConnectionResponse.CompanyId), o => o.MapFrom(s => s.CompanyId))
            .ForCtorParam(nameof(ConnectionResponse.ApplicationName), o => o.MapFrom(s => s.ApplicationName))
            .ForCtorParam(nameof(ConnectionResponse.ClientToken), o => o.MapFrom(s => s.ClientToken))
            .ForCtorParam(nameof(ConnectionResponse.IsActive), o => o.MapFrom(s => s.IsActive))
            .ForCtorParam(nameof(ConnectionResponse.MVPApiExpiresIn), o => o.MapFrom(s => s.MVPApiExpiresIn));
    }
}
