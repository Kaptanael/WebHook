using AutoMapper;
using MVPAPI.WebHook.Application.DTOs.Connections;
using MVPAPI.WebHook.Application.DTOs.Endpoints;
using MVPAPI.WebHook.Application.DTOs.Events;
using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<WebhookEvent, EventResponse>()
            .ForCtorParam(nameof(EventResponse.Id), o => o.MapFrom(s => s.Id))
            .ForCtorParam(nameof(EventResponse.WebhookId), o => o.MapFrom(s => s.WebhookId))
            .ForCtorParam(nameof(EventResponse.Provider), o => o.MapFrom(s => s.Provider))
            .ForCtorParam(nameof(EventResponse.EventType), o => o.MapFrom(s => s.EventType))
            .ForCtorParam(nameof(EventResponse.Status), o => o.MapFrom(s => s.Status))
            .ForCtorParam(nameof(EventResponse.Attempts), o => o.MapFrom(s => s.Attempts))
            .ForCtorParam(nameof(EventResponse.LastError), o => o.MapFrom(s => s.LastError))
            .ForCtorParam(nameof(EventResponse.ReceivedAtUtc), o => o.MapFrom(s => s.ReceivedAtUtc))
            .ForCtorParam(nameof(EventResponse.NextAttemptAtUtc), o => o.MapFrom(s => s.NextAttemptAtUtc))
            .ForCtorParam(nameof(EventResponse.ProcessedAtUtc), o => o.MapFrom(s => s.ProcessedAtUtc));

        CreateMap<WebhookEndpoint, EndpointResponse>()
            .ForCtorParam(nameof(EndpointResponse.Id), o => o.MapFrom(s => s.Id))
            .ForCtorParam(nameof(EndpointResponse.EndpointToken), o => o.MapFrom(s => s.EndPointToken))
            .ForCtorParam(nameof(EndpointResponse.Endpoint), o => o.MapFrom(s => s.Endpoint))
            .ForCtorParam(nameof(EndpointResponse.CompanyId), o => o.MapFrom(s => s.CompanyId))
            .ForCtorParam(nameof(EndpointResponse.TriggerJson), o => o.MapFrom(s => s.TriggerConfigJson));

        CreateMap<WebHookConnection, ConnectionResponse>()
            .ForCtorParam(nameof(ConnectionResponse.Id), o => o.MapFrom(s => s.Id))
            .ForCtorParam(nameof(ConnectionResponse.CompanyId), o => o.MapFrom(s => s.CompanyId))
            .ForCtorParam(nameof(ConnectionResponse.ApplicationName), o => o.MapFrom(s => s.ApplicationName))
            .ForCtorParam(nameof(ConnectionResponse.ClientToken), o => o.MapFrom(s => s.ClientToken))
            .ForCtorParam(nameof(ConnectionResponse.IsActive), o => o.MapFrom(s => s.IsActive))
            .ForCtorParam(nameof(ConnectionResponse.MVPApiExpiresIn), o => o.MapFrom(s => s.MVPApiExpiresIn));
    }
}
