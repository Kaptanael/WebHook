using MVPAPI.WebHook.Application.Interfaces.Inbound;
using MVPAPI.WebHook.Application.Interfaces.Services;
using MVPAPI.WebHook.Application.Mappings;
using MVPAPI.WebHook.Application.Services;
using MVPAPI.WebHook.Application.Services.Inbound;

namespace MVPAPI.WebHook.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
        services.AddHttpClient<IAccountApiClient, AccountApiClient>();
        services.AddScoped<IWebHookConnectionService, WebHookConnectionService>();
        services.AddScoped<IWebhookEndpointService, WebhookOutboundService>();
        services.AddScoped<IWebhookInboundService, WebhookInboundService>();
        services.AddScoped<IWebhookEventLifecycleService, WebhookEventLifecycleService>();
        services.AddScoped<IWebhookDispatchService, WebhookDispatchService>();
        services.AddScoped<IWebhookManager, WebhookManager>();        
        services.AddSingleton<Common.WebhookSigner>();

        // Inbound webhook pipeline.
        services.AddScoped<IInboundWebhookHandler, InboundWebhookHandler>();
        services.AddKeyedScoped<IInboundAuthenticator, StandardInboundAuthenticator>("standard");
        services.AddKeyedScoped<IInboundAuthenticator, TokenInboundAuthenticator>("token");
        services.AddScoped<IInboundProvisioningService, InboundProvisioningService>();

        // Common helpers shared by the receivers.
        services.AddMemoryCache();
        services.AddSingleton<IInboundReplayGuard, InboundReplayGuard>();
        services.AddScoped<PayloadNormalizer>();

        // One receiver per credential scheme (each handles auth + normalize + queue); the handler routes
        // by header type.
        services.AddScoped<StandardWebhookReceiver>();   // x-api-key + signed triplet
        services.AddScoped<ApiKeyReceiver>();            // x-api-key only (no signature)
        services.AddScoped<TokenWebhookReceiver>();             // x-token
        services.AddScoped<TokenInboundAuthenticator>();
        return services;
    }
}
