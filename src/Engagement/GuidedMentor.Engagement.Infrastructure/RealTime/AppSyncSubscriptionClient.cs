using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.SharedKernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GuidedMentor.Engagement.Infrastructure.RealTime;

/// <summary>
/// AppSync WebSocket subscription client with exponential backoff reconnection.
/// Listens for real-time notifications via the AppSync real-time endpoint.
/// Used by backend services that need to consume notifications (e.g., integration tests, workers).
/// Frontend clients use the AppSync JS SDK directly for WebSocket subscriptions.
/// </summary>
public sealed class AppSyncSubscriptionClient : IAsyncDisposable
{
    private readonly AppSyncOptions _options;
    private readonly ILogger<AppSyncSubscriptionClient> _logger;
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _receiveLoop;

    private int _reconnectAttempt;
    private const int MaxReconnectAttempt = 10;
    private const int BaseDelayMs = 1000; // 1 second
    private const int MaxDelayMs = 60_000; // 60 seconds cap

    /// <summary>
    /// Fired when a notification is received via the subscription.
    /// </summary>
    public event Func<Notification, Task>? OnNotificationReceived;

    /// <summary>
    /// Indicates whether the WebSocket connection is currently active.
    /// </summary>
    public bool IsConnected => _webSocket?.State == WebSocketState.Open;

    public AppSyncSubscriptionClient(
        IOptions<AppSyncOptions> options,
        ILogger<AppSyncSubscriptionClient> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Connects to the AppSync real-time endpoint and subscribes to notifications for the given user.
    /// Implements exponential backoff reconnection on disconnect.
    /// </summary>
    public async Task ConnectAsync(string userId, string authToken, CancellationToken cancellationToken = default)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await ConnectInternalAsync(userId, authToken, _cancellationTokenSource.Token);
    }

    private async Task ConnectInternalAsync(string userId, string authToken, CancellationToken cancellationToken)
    {
        _webSocket?.Dispose();
        _webSocket = new ClientWebSocket();

        try
        {
            var uri = new Uri(_options.RealtimeEndpoint);
            _webSocket.Options.SetRequestHeader("Authorization", authToken);

            await _webSocket.ConnectAsync(uri, cancellationToken);
            _reconnectAttempt = 0;

            _logger.LogInformation("Connected to AppSync real-time endpoint for user {UserId}", userId);

            // Send subscription registration
            var subscriptionPayload = JsonSerializer.Serialize(new
            {
                type = "start",
                id = Guid.NewGuid().ToString(),
                payload = new
                {
                    data = JsonSerializer.Serialize(new
                    {
                        query = $"subscription OnNotification {{ onNotification(recipientUserId: \"{userId}\") {{ notificationId recipientUserId type message actionUrl isRead createdAt }} }}"
                    }),
                    extensions = new
                    {
                        authorization = new
                        {
                            Authorization = authToken,
                            host = new Uri(_options.GraphqlEndpoint).Host
                        }
                    }
                }
            });

            var buffer = Encoding.UTF8.GetBytes(subscriptionPayload);
            await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken);

            // Start receive loop
            _receiveLoop = ReceiveLoopAsync(userId, authToken, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to connect to AppSync. Will attempt reconnection.");
            await ReconnectWithBackoffAsync(userId, authToken, cancellationToken);
        }
    }

    private async Task ReceiveLoopAsync(string userId, string authToken, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];

        try
        {
            while (_webSocket?.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await _webSocket.ReceiveAsync(buffer, cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogWarning("AppSync WebSocket closed. Initiating reconnection.");
                    await ReconnectWithBackoffAsync(userId, authToken, cancellationToken);
                    return;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await HandleMessageAsync(message);
                }
            }
        }
        catch (WebSocketException ex)
        {
            _logger.LogWarning(ex, "WebSocket disconnected. Initiating reconnection with exponential backoff.");
            await ReconnectWithBackoffAsync(userId, authToken, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("WebSocket receive loop cancelled.");
        }
    }

    /// <summary>
    /// Implements exponential backoff reconnection strategy.
    /// Delay = min(BaseDelay * 2^attempt + jitter, MaxDelay).
    /// </summary>
    private async Task ReconnectWithBackoffAsync(string userId, string authToken, CancellationToken cancellationToken)
    {
        while (_reconnectAttempt < MaxReconnectAttempt && !cancellationToken.IsCancellationRequested)
        {
            _reconnectAttempt++;

            var delay = CalculateBackoffDelay(_reconnectAttempt);
            _logger.LogInformation(
                "Reconnection attempt {Attempt}/{Max} in {DelayMs}ms",
                _reconnectAttempt, MaxReconnectAttempt, delay);

            try
            {
                await Task.Delay(delay, cancellationToken);
                await ConnectInternalAsync(userId, authToken, cancellationToken);
                return; // Successfully reconnected
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Reconnection attempt {Attempt} failed.", _reconnectAttempt);
            }
        }

        _logger.LogError(
            "Failed to reconnect after {MaxAttempts} attempts. Subscription for user {UserId} is disconnected.",
            MaxReconnectAttempt, userId);
    }

    /// <summary>
    /// Calculates exponential backoff delay with jitter.
    /// Formula: min(baseDelay * 2^attempt + random(0..baseDelay), maxDelay)
    /// </summary>
    internal static int CalculateBackoffDelay(int attempt)
    {
        var exponentialDelay = BaseDelayMs * (1 << Math.Min(attempt, 20)); // Prevent overflow
        var jitter = Random.Shared.Next(0, BaseDelayMs);
        return Math.Min(exponentialDelay + jitter, MaxDelayMs);
    }

    private async Task HandleMessageAsync(string message)
    {
        try
        {
            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeElement))
                return;

            var messageType = typeElement.GetString();

            if (messageType == "data" && root.TryGetProperty("payload", out var payload))
            {
                if (payload.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("onNotification", out var notifData))
                {
                    var notification = Notification.Reconstitute(
                        id: new NotificationId(Guid.Parse(notifData.GetProperty("notificationId").GetString()!)),
                        recipientUserId: new UserId(Guid.Parse(notifData.GetProperty("recipientUserId").GetString()!)),
                        type: Enum.Parse<NotificationType>(notifData.GetProperty("type").GetString()!),
                        message: notifData.GetProperty("message").GetString()!,
                        actionUrl: notifData.GetProperty("actionUrl").GetString()!,
                        isRead: notifData.GetProperty("isRead").GetBoolean(),
                        createdAt: DateTime.Parse(notifData.GetProperty("createdAt").GetString()!).ToUniversalTime());

                    if (OnNotificationReceived is not null)
                    {
                        await OnNotificationReceived.Invoke(notification);
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse AppSync message.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cancellationTokenSource?.Cancel();

        if (_receiveLoop is not null)
        {
            try
            {
                await _receiveLoop;
            }
            catch (OperationCanceledException) { }
        }

        if (_webSocket?.State == WebSocketState.Open)
        {
            await _webSocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Client disconnecting",
                CancellationToken.None);
        }

        _webSocket?.Dispose();
        _cancellationTokenSource?.Dispose();
    }
}
