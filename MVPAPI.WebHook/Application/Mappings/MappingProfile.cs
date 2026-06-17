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
        CreateMap<MvpEvent, MvpEventResponse>()
            .ForCtorParam(nameof(MvpEventResponse.Id), o => o.MapFrom(s => s.Seqno))
            .ForCtorParam(nameof(MvpEventResponse.Category), o => o.MapFrom(s => s.Cat))
            .ForCtorParam(nameof(MvpEventResponse.PanelNo), o => o.MapFrom(s => s.PnlNo))
            .ForCtorParam(nameof(MvpEventResponse.EventDate), o => o.MapFrom(s => s.EDate))
            .ForCtorParam(nameof(MvpEventResponse.FacilityNo), o => o.MapFrom(s => s.Facno))
            .ForCtorParam(nameof(MvpEventResponse.Archive), o => o.MapFrom(s => s.Arch))
            .ForCtorParam(nameof(MvpEventResponse.AcknowledgeOperator), o => o.MapFrom(s => s.AckOpr))
            .ForCtorParam(nameof(MvpEventResponse.AcknowledgeTimestamp), o => o.MapFrom(s => s.AckTStamp))
            .ForCtorParam(nameof(MvpEventResponse.ResponseRequired), o => o.MapFrom(s => s.RespReq))
            .ForCtorParam(nameof(MvpEventResponse.CaObjectId), o => o.MapFrom(s => s.CaObjectID))
            .ForCtorParam(nameof(MvpEventResponse.UtcOffset), o => o.MapFrom(s => s.UTCOffset));

        CreateMap<CreateMVPEventPayload, MvpEvent>()
            .ForMember(dest => dest.Seqno,         opt => opt.Ignore())
            .ForMember(dest => dest.CompanyId,     opt => opt.Ignore())
            .ForMember(dest => dest.Cat,           opt => opt.MapFrom(src => src.Category))
            .ForMember(dest => dest.PnlNo,         opt => opt.MapFrom(src => src.PanelId))
            .ForMember(dest => dest.EDate,         opt => opt.MapFrom(src => src.EventDate))
            .ForMember(dest => dest.DeviceNo,      opt => opt.MapFrom(src => src.DeviceId))
            .ForMember(dest => dest.Facno,         opt => opt.MapFrom(src => src.FacilityNo))
            .ForMember(dest => dest.Arch,          opt => opt.MapFrom(src => src.Archive))
            .ForMember(dest => dest.AckOpr,        opt => opt.MapFrom(src => src.AcknowledgeOperator))
            .ForMember(dest => dest.AckTStamp,     opt => opt.MapFrom(src => src.AcknowledgeTimeStamp))
            .ForMember(dest => dest.RespReq,       opt => opt.MapFrom(src => src.ResponseRequired))
            .ForMember(dest => dest.SeqNoFromLock, opt => opt.MapFrom(src => src.SequenceNoFromLock))
            .ForMember(dest => dest.CaObjectID,    opt => opt.Ignore())
            .ForMember(dest => dest.UTCOffset,     opt => opt.Ignore());

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
