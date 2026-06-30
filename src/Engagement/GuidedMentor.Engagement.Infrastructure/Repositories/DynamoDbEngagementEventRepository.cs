using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.Engagement.Domain.Repositories;

namespace GuidedMentor.Engagement.Infrastructure.Repositories;

/// <summary>
/// DynamoDB implementation of IEngagementEventRepository.
/// Batch writes engagement events to the EngagementEvents_Table.
///
/// Requirements: 30.2
/// </summary>
public sealed class DynamoDbEngagementEventRepository : IEngagementEventRepository
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private const string TableName = "EngagementEvents_Table";
    private const int MaxBatchSize = 25; // DynamoDB BatchWriteItem limit

    public DynamoDbEngagementEventRepository(IAmazonDynamoDB dynamoDb)
    {
        _dynamoDb = dynamoDb ?? throw new ArgumentNullException(nameof(dynamoDb));
    }

    public async Task BatchPutAsync(IReadOnlyList<EngagementEvent> events, CancellationToken ct = default)
    {
        if (events.Count == 0) return;

        // Split into chunks of 25 (DynamoDB limit per batch)
        var chunks = events
            .Select((e, i) => new { Event = e, Index = i })
            .GroupBy(x => x.Index / MaxBatchSize)
            .Select(g => g.Select(x => x.Event).ToList());

        foreach (var chunk in chunks)
        {
            var writeRequests = chunk.Select(e => new WriteRequest
            {
                PutRequest = new PutRequest
                {
                    Item = ToAttributeMap(e),
                },
            }).ToList();

            var request = new BatchWriteItemRequest
            {
                RequestItems = new Dictionary<string, List<WriteRequest>>
                {
                    [TableName] = writeRequests,
                },
            };

            var response = await _dynamoDb.BatchWriteItemAsync(request, ct);

            // Handle unprocessed items with simple retry
            if (response.UnprocessedItems.Count > 0)
            {
                await Task.Delay(100, ct);
                await _dynamoDb.BatchWriteItemAsync(
                    new BatchWriteItemRequest { RequestItems = response.UnprocessedItems }, ct);
            }
        }
    }

    private static Dictionary<string, AttributeValue> ToAttributeMap(EngagementEvent e)
    {
        var map = new Dictionary<string, AttributeValue>
        {
            ["eventId"] = new AttributeValue { S = e.Id.ToString() },
            ["userIdHash"] = new AttributeValue { S = e.UserIdHash },
            ["eventType"] = new AttributeValue { S = e.EventType },
            ["timestamp"] = new AttributeValue { N = e.Timestamp.ToString() },
            ["sessionId"] = new AttributeValue { S = e.SessionId },
            ["pageContext"] = new AttributeValue { S = e.PageContext },
            ["activeRole"] = new AttributeValue { S = e.ActiveRole },
            ["ttl"] = new AttributeValue { N = e.Ttl.ToString() },
        };

        if (e.EventData is { Count: > 0 })
        {
            map["eventData"] = new AttributeValue
            {
                S = System.Text.Json.JsonSerializer.Serialize(e.EventData),
            };
        }

        return map;
    }
}
