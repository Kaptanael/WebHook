using Microsoft.Extensions.Options;
using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Interfaces;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Infrastructure.Delivery;
using MVPAPI.WebHook.Infrastructure.Persistence;
using MVPAPI.WebHook.Infrastructure.Repositories;

namespace MVPAPI.WebHook.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MVPWebhookDB")
            ?? throw new InvalidOperationException("Connection string 'MVPWebhookDB' is not configured.");

        services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));

        services.AddScoped<IWebHookConnectionRepository, WebHookConnectionRepository>();
        services.AddScoped<IWebhookEndpointRepository, WebhookEndpointRepository>();
        services.AddScoped<IWebhookEventRepository, WebhookEventRepository>();

        services.AddHttpClient<IWebhookDeliveryClient, HttpWebhookDeliveryClient>((sp, client) =>
        {
            var dispatchOptions = sp.GetRequiredService<IOptions<WebhookDispatchOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(dispatchOptions.DeliveryTimeoutSeconds);
        });

        return services;
    }
}
