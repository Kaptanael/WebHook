using MVPAPI.WebHook.Application.Interfaces.Services;
using MVPAPI.WebHook.Application.Mappings;
using MVPAPI.WebHook.Application.Services;

namespace MVPAPI.WebHook.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
        services.AddScoped<IWebHookConnectionManager, WebHookConnectionManager>();
        services.AddScoped<IWebHookConnectionService, WebHookConnectionService>();
        services.AddHttpClient<IAccountApiClient, AccountApiClient>();
        services.AddScoped<IWebhookEndpointService, WebhookEndpointService>();
        services.AddScoped<IWebhookEventService, WebhookEventService>();
        services.AddScoped<IWebhookDispatchService, WebhookDispatchService>();        
        services.AddScoped<ITokenValidator, TokenValidator>();
        return services;
    }
}
