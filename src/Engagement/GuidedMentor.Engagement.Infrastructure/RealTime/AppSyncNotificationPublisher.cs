using System.Net.Http.Json;
using System.Text.Json;
using GuidedMentor.Engagement.Application.Interfaces;
using GuidedMentor.Engagement.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GuidedMentor.Engagement.Infrastructure.RealTime;

/// <summary>
/// Publishes notifications to recipients via AWS AppSync GraphQL mutations,
/// which trigger WebSocket subscriptions for real-time delivery (&lt; 5 seconds).
/// </summary>
public sealed class AppSyncNotificationPublisher : IAppSyncNotificationPublisher
{
    private readonly HttpClient _httpClient;
    private readonly AppSyncOptions _options;
    private readonly ILogger<AppSyncNotificationPublisher> _logger;

    public AppSyncNotificationPublisher(
        HttpClient httpClient,
        IOptions<AppSyncOptions> options,
        ILogger<AppSyncNotificationPublisher> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        var mutation = new
        {
            query = """
                mutation PublishNotification($input: NotificationInput!) {
                    publishNotification(input: $input) {
                        notificationId
                        recipientUserId
                        type
                        message
                        actionUrl
                        isRead
                        createdAt
                    }
                }
                """,
            variables = new
            {
                input = new
                {
                    notificationId = notification.Id.Value.ToString(),
                    recipientUserId = notification.RecipientUserId.Value.ToString(),
                    type = notification.Type.ToString(),
                    message = notification.Message,
                    actionUrl = notification.ActionUrl,
                    isRead = notification.IsRead,
                    createdAt = notification.CreatedAt.ToString("o")
                }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, _options.GraphqlEndpoint)
        {
            Content = JsonContent.Create(mutation)
        };

        // AppSync IAM auth via SigV4 (handled by the configured HttpClient message handler)
        request.Headers.Add("x-api-key", _options.ApiKey);

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogDebug(
                "Published notification {NotificationId} to AppSync for user {RecipientUserId}",
                notification.Id.Value, notification.RecipientUserId.Value);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex,
                "Failed to publish notification {NotificationId} via AppSync. " +
                "Notification was persisted but real-time delivery may be delayed.",
                notification.Id.Value);

            // Don't throw — notification is already persisted, client can poll/reconnect
        }
    }
}
