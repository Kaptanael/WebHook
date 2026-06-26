using Microsoft.Extensions.Options;
using MVPAPI.WebHook.Application.Common.Options;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Application.Interfaces.Services;
using MVPAPI.WebHook.Infrastructure.Delivery;
using MVPAPI.WebHook.Infrastructure.Persistence;
using MVPAPI.WebHook.Infrastructure.Repositories;

namespace MVPAPI.WebHook.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var webhookDb = configuration.GetConnectionString("MVPWebhookDB")
            ?? throw new InvalidOperationException("Connection string 'MVPWebhookDB' is not configured.");
        var portalDb = configuration.GetConnectionString("PortalDB")
            ?? throw new InvalidOperationException("Connection string 'PortalDB' is not configured.");

        services.AddSingleton<IWebhookDbConnectionFactory>(new SqlConnectionFactory(webhookDb));
        services.AddSingleton<IPortalDbConnectionFactory>(new PortalDbConnectionFactory(portalDb));

        services.AddScoped<IWebHookConnectionRepository, WebHookConnectionRepository>();
        services.AddScoped<IWebhookOutboundRepository, WebhookOutboundRepository>();
        services.AddScoped<IWebhookInboundRepository, WebhookInboundRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<IClientCredentialRepository, ClientCredentialRepository>();

        services.AddHttpClient<IWebhookDeliveryClient, HttpWebhookDeliveryClient>((sp, client) =>
        {
            var dispatchOptions = sp.GetRequiredService<IOptions<WebhookDispatchOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(dispatchOptions.DeliveryTimeoutSeconds);
        });

        return services;
    }
}
