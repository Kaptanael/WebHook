using MVPAPI.WebHook.Application.Interfaces.Services;
using MVPAPI.WebHook.Application.Services;

namespace MVPAPI.WebHook.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IWebHookConnectionService, WebHookConnectionService>();
        services.AddHttpClient<IAccountApiClient, AccountApiClient>();
        services.AddScoped<IWebhookEndpointService, WebhookEndpointService>();
        services.AddScoped<IWebhookEventService, WebhookEventService>();
        services.AddScoped<IWebhookDispatchService, WebhookDispatchService>();
        services.AddScoped<ITokenValidator, TokenValidator>();
        return services;
    }
}
