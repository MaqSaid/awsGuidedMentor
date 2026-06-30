using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.Engagement.Domain.Repositories;

namespace GuidedMentor.Engagement.Infrastructure.Repositories;

/// <summary>
/// DynamoDB implementation of IConsentRepository.
/// Persists user consent preferences.
///
/// Requirements: 30.7, 30.8
/// </summary>
public sealed class DynamoDbConsentRepository : IConsentRepository
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private const string TableName = "UserConsent_Table";

    public DynamoDbConsentRepository(IAmazonDynamoDB dynamoDb)
    {
        _dynamoDb = dynamoDb ?? throw new ArgumentNullException(nameof(dynamoDb));
    }

    public async Task<ConsentPreference?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var response = await _dynamoDb.GetItemAsync(new GetItemRequest
        {
            TableName = TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["userId"] = new AttributeValue { S = userId.ToString() },
            },
        }, ct);

        if (!response.IsItemSet)
            return null;

        var item = response.Item;
        var preference = ConsentPreference.Create(
            userId,
            item.TryGetValue("status", out var statusAttr) ? statusAttr.S : "pending");

        return preference;
    }

    public async Task UpsertAsync(ConsentPreference consent, CancellationToken ct = default)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["userId"] = new AttributeValue { S = consent.UserId.ToString() },
            ["consentId"] = new AttributeValue { S = consent.Id.ToString() },
            ["status"] = new AttributeValue { S = consent.Status },
            ["updatedAt"] = new AttributeValue { S = consent.UpdatedAt.ToString("O") },
        };

        await _dynamoDb.PutItemAsync(new PutItemRequest
        {
            TableName = TableName,
            Item = item,
        }, ct);
    }
}
