using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.Engagement.Domain.Repositories;
using GuidedMentor.SharedKernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GuidedMentor.Engagement.Infrastructure.Persistence;

/// <summary>
/// DynamoDB implementation of the notification repository.
/// Table: Notifications_Table (PK=notificationId).
/// GSI-Recipient: PK=recipientUserId, SK=createdAt (for unread counts and full scans).
/// GSI-RecipientMonth: PK=recipientMonthKey (recipientUserId#YYYY-MM), SK=createdAt
///   — distributes writes across monthly partitions to prevent hot keys (Requirement 26.2).
/// </summary>
public sealed class DynamoDbNotificationRepository : INotificationRepository
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _tableName;
    private readonly ILogger<DynamoDbNotificationRepository> _logger;

    private const string GsiRecipientName = "GSI-Recipient";
    private const string GsiRecipientMonthName = "GSI-RecipientMonth";

    public DynamoDbNotificationRepository(
        IAmazonDynamoDB dynamoDb,
        IOptions<NotificationTableOptions> options,
        ILogger<DynamoDbNotificationRepository> logger)
    {
        _dynamoDb = dynamoDb;
        _tableName = options.Value.TableName;
        _logger = logger;
    }

    public async Task SaveAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        var recipientMonthKey = BuildRecipientMonthKey(
            notification.RecipientUserId.Value.ToString(),
            notification.CreatedAt);

        var item = new Dictionary<string, AttributeValue>
        {
            ["notificationId"] = new AttributeValue { S = notification.Id.Value.ToString() },
            ["recipientUserId"] = new AttributeValue { S = notification.RecipientUserId.Value.ToString() },
            ["recipientMonthKey"] = new AttributeValue { S = recipientMonthKey },
            ["type"] = new AttributeValue { S = notification.Type.ToString() },
            ["message"] = new AttributeValue { S = notification.Message },
            ["actionUrl"] = new AttributeValue { S = notification.ActionUrl },
            ["isRead"] = new AttributeValue { BOOL = notification.IsRead },
            ["createdAt"] = new AttributeValue { S = notification.CreatedAt.ToString("o") }
        };

        var request = new PutItemRequest
        {
            TableName = _tableName,
            Item = item
        };

        await _dynamoDb.PutItemAsync(request, cancellationToken);
        _logger.LogDebug("Saved notification {NotificationId} for user {RecipientUserId} with partition key {RecipientMonthKey}",
            notification.Id.Value, notification.RecipientUserId.Value, recipientMonthKey);
    }

    public async Task<Notification?> GetByIdAsync(NotificationId id, CancellationToken cancellationToken = default)
    {
        var request = new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["notificationId"] = new AttributeValue { S = id.Value.ToString() }
            }
        };

        var response = await _dynamoDb.GetItemAsync(request, cancellationToken);

        if (response.Item is null || response.Item.Count == 0)
            return null;

        return MapFromDynamoDb(response.Item);
    }

    public async Task<IReadOnlyList<Notification>> GetByRecipientAsync(
        UserId recipientUserId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        // Use the composite GSI-RecipientMonth to query recent months.
        // Query current month first, then previous months until we reach the limit.
        var notifications = new List<Notification>();
        var currentMonth = DateTime.UtcNow;
        var maxMonthsBack = 6; // Look back up to 6 months

        for (var i = 0; i < maxMonthsBack && notifications.Count < limit; i++)
        {
            var targetMonth = currentMonth.AddMonths(-i);
            var recipientMonthKey = BuildRecipientMonthKey(
                recipientUserId.Value.ToString(), targetMonth);

            var remaining = limit - notifications.Count;

            var request = new QueryRequest
            {
                TableName = _tableName,
                IndexName = GsiRecipientMonthName,
                KeyConditionExpression = "recipientMonthKey = :monthKey",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":monthKey"] = new AttributeValue { S = recipientMonthKey }
                },
                ScanIndexForward = false, // Reverse chronological order
                Limit = remaining
            };

            var response = await _dynamoDb.QueryAsync(request, cancellationToken);

            notifications.AddRange(response.Items.Select(MapFromDynamoDb));
        }

        return notifications
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToList()
            .AsReadOnly();
    }

    public async Task<int> GetUnreadCountAsync(UserId recipientUserId, CancellationToken cancellationToken = default)
    {
        // Use the original GSI-Recipient for unread count (scans all months for a user).
        // This is acceptable since it's a count query with a filter, not a hot write path.
        var request = new QueryRequest
        {
            TableName = _tableName,
            IndexName = GsiRecipientName,
            KeyConditionExpression = "recipientUserId = :userId",
            FilterExpression = "isRead = :isRead",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":userId"] = new AttributeValue { S = recipientUserId.Value.ToString() },
                [":isRead"] = new AttributeValue { BOOL = false }
            },
            Select = "COUNT"
        };

        var response = await _dynamoDb.QueryAsync(request, cancellationToken);
        return response.Count;
    }

    public async Task MarkAsReadAsync(NotificationId id, CancellationToken cancellationToken = default)
    {
        var request = new UpdateItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["notificationId"] = new AttributeValue { S = id.Value.ToString() }
            },
            UpdateExpression = "SET isRead = :isRead",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":isRead"] = new AttributeValue { BOOL = true }
            }
        };

        await _dynamoDb.UpdateItemAsync(request, cancellationToken);
    }

    public async Task BatchMarkAsReadAsync(UserId recipientUserId, CancellationToken cancellationToken = default)
    {
        // Use the original GSI-Recipient for batch read operations.
        var unreadNotifications = new List<string>();
        string? lastEvaluatedKey = null;

        do
        {
            var queryRequest = new QueryRequest
            {
                TableName = _tableName,
                IndexName = GsiRecipientName,
                KeyConditionExpression = "recipientUserId = :userId",
                FilterExpression = "isRead = :isRead",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":userId"] = new AttributeValue { S = recipientUserId.Value.ToString() },
                    [":isRead"] = new AttributeValue { BOOL = false }
                },
                ProjectionExpression = "notificationId"
            };

            if (lastEvaluatedKey is not null)
            {
                queryRequest.ExclusiveStartKey = new Dictionary<string, AttributeValue>
                {
                    ["notificationId"] = new AttributeValue { S = lastEvaluatedKey }
                };
            }

            var response = await _dynamoDb.QueryAsync(queryRequest, cancellationToken);

            foreach (var item in response.Items)
            {
                unreadNotifications.Add(item["notificationId"].S);
            }

            lastEvaluatedKey = response.LastEvaluatedKey?.GetValueOrDefault("notificationId")?.S;
        } while (lastEvaluatedKey is not null);

        // Batch update each notification (DynamoDB doesn't support batch updates natively,
        // so we use individual UpdateItem calls — acceptable for <= 50 notifications)
        var updateTasks = unreadNotifications.Select(id =>
            MarkAsReadAsync(new NotificationId(Guid.Parse(id)), cancellationToken));

        await Task.WhenAll(updateTasks);

        _logger.LogDebug("Batch marked {Count} notifications as read for user {UserId}",
            unreadNotifications.Count, recipientUserId.Value);
    }

    /// <summary>
    /// Builds the composite partition key: recipientUserId#YYYY-MM.
    /// This distributes writes across monthly partitions to prevent hot keys
    /// for high-volume notification recipients (Requirement 26.2).
    /// </summary>
    internal static string BuildRecipientMonthKey(string recipientUserId, DateTime dateTime)
    {
        return $"{recipientUserId}#{dateTime:yyyy-MM}";
    }

    private static Notification MapFromDynamoDb(Dictionary<string, AttributeValue> item)
    {
        return Notification.Reconstitute(
            id: new NotificationId(Guid.Parse(item["notificationId"].S)),
            recipientUserId: new UserId(Guid.Parse(item["recipientUserId"].S)),
            type: Enum.Parse<NotificationType>(item["type"].S),
            message: item["message"].S,
            actionUrl: item["actionUrl"].S,
            isRead: item["isRead"].BOOL,
            createdAt: DateTime.Parse(item["createdAt"].S).ToUniversalTime());
    }
}
