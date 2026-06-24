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
        services.AddScoped<IWebHookConnectionManager, WebHookConnectionManager>();
        services.AddScoped<IWebHookConnectionService, WebHookConnectionService>();
        services.AddHttpClient<IAccountApiClient, AccountApiClient>();
        services.AddScoped<IWebhookEndpointService, WebhookEndpointService>();
        services.AddScoped<IWebhookEventService, WebhookEventService>();
        services.AddScoped<IWebhookEventLifecycleService, WebhookEventLifecycleService>();
        services.AddScoped<IWebhookDispatchService, WebhookDispatchService>();
        services.AddScoped<ITokenDecoder, TokenDecoder>();
        services.AddSingleton<IWebhookSignatureVerifier, WebhookSignatureVerifier>();
        services.AddSingleton<IWebhookSigner, StandardWebhookSigner>();

        // Inbound webhook pipeline.
        services.AddScoped<IInboundWebhookPipeline, InboundWebhookPipeline>();
        services.AddScoped<IApiKeyInboundResolver, ApiKeyStandardWebhookResolver>();
        services.AddScoped<IInboundProvisioningService, InboundProvisioningService>();
        services.AddScoped<IPayloadAdapter, DefaultPayloadAdapter>();
        // Fallback per-endpoint credential matching (used only when no X-Api-Key is presented).
        services.AddScoped<IInboundAuthenticator, StandardWebhooksInboundAuthenticator>();
        services.AddScoped<IInboundAuthenticator, TokenInboundAuthenticator>();
        return services;
    }
}
