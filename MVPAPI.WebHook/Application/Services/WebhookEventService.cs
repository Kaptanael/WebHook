using MVPAPI.WebHook.Application.Common.Exceptions;
using MVPAPI.WebHook.Application.DTOs.Events;
using MVPAPI.WebHook.Application.Interfaces;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Application.Interfaces.Services;
using MVPAPI.WebHook.Domain.Entities;
using MVPAPI.WebHook.Domain.Enums;

namespace MVPAPI.WebHook.Application.Services;

public class WebhookEventService(
    IWebHookConnectionRepository connectionRepository,
    IWebhookEndpointRepository endpointRepository,
    IWebhookEventRepository eventRepository) : IWebhookEventService
{
    private const int MaxAttempts = 5;

    public async Task<IReadOnlyList<EventResponse>> PublishAsync(PublishEventRequest request, CancellationToken cancellationToken = default)
    {
        var connection = await connectionRepository.GetByClientTokenAsync(request.ClientToken, cancellationToken)
            ?? throw new NotFoundException("No connection found for the supplied client token.");

        var endpoints = await endpointRepository.GetByCompanyIdAsync(connection.CompanyId, cancellationToken);
        if (endpoints.Count == 0)
        {
            return [];
        }

        var events = new List<WebhookEvent>(endpoints.Count);
        foreach (var endpoint in endpoints)
        {
            var webhookEvent = new WebhookEvent
            {
                WebhookId = endpoint.Id,
                Provider = request.Provider,
                EventType = request.EventType,
                Payload = request.Payload,
                Status = EventStatus.Pending,
                ReceivedAtUtc = DateTime.UtcNow,
                NextAttemptAtUtc = DateTime.UtcNow
            };
            events.Add(webhookEvent);
            await eventRepository.AddAsync(webhookEvent, cancellationToken);
        }

        return events.Select(ToResponse).ToList();
    }

    public async Task<EventResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var webhookEvent = await eventRepository.GetByIdAsync(id, cancellationToken);
        return webhookEvent is null ? null : ToResponse(webhookEvent);
    }

    public async Task<IReadOnlyList<EventResponse>> GetDueForProcessingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var events = await eventRepository.GetDueForProcessingAsync(batchSize, DateTime.UtcNow, cancellationToken);
        return events.Select(ToResponse).ToList();
    }

    public async Task<bool> MarkProcessingAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var webhookEvent = await eventRepository.GetByIdAsync(id, cancellationToken);
        if (webhookEvent is null)
        {
            return false;
        }

        webhookEvent.Status = EventStatus.Processing;
        webhookEvent.ProcessingStartedAtUtc = DateTime.UtcNow;
        await eventRepository.UpdateAsync(webhookEvent, cancellationToken);
        return true;
    }

    public async Task<bool> MarkCompletedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var webhookEvent = await eventRepository.GetByIdAsync(id, cancellationToken);
        if (webhookEvent is null)
        {
            return false;
        }

        webhookEvent.Status = EventStatus.Completed;
        webhookEvent.ProcessedAtUtc = DateTime.UtcNow;
        webhookEvent.NextAttemptAtUtc = null;
        await eventRepository.UpdateAsync(webhookEvent, cancellationToken);
        return true;
    }

    public async Task<bool> MarkFailedAsync(Guid id, string error, CancellationToken cancellationToken = default)
    {
        var webhookEvent = await eventRepository.GetByIdAsync(id, cancellationToken);
        if (webhookEvent is null)
        {
            return false;
        }

        webhookEvent.Attempts++;
        webhookEvent.LastError = error;

        if (webhookEvent.Attempts >= MaxAttempts)
        {
            webhookEvent.Status = EventStatus.Failed;
            webhookEvent.NextAttemptAtUtc = null;
        }
        else
        {
            // Exponential backoff: 1, 2, 4, 8... minutes
            webhookEvent.Status = EventStatus.Retrying;
            webhookEvent.NextAttemptAtUtc = DateTime.UtcNow.AddMinutes(Math.Pow(2, webhookEvent.Attempts - 1));
        }

        await eventRepository.UpdateAsync(webhookEvent, cancellationToken);
        return true;
    }

    private static EventResponse ToResponse(WebhookEvent webhookEvent) =>
        new(webhookEvent.Id,
            webhookEvent.WebhookId,
            webhookEvent.Provider,
            webhookEvent.EventType,
            webhookEvent.Status,
            webhookEvent.Attempts,
            webhookEvent.LastError,
            webhookEvent.ReceivedAtUtc,
            webhookEvent.NextAttemptAtUtc,
            webhookEvent.ProcessedAtUtc);
}
