using Amazon.Lambda.Core;
using GuidedMentor.Engagement.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.BackgroundJobs.Functions;

/// <summary>
/// Lambda function triggered by DynamoDB Streams.
/// Routes stream records to the existing DynamoDbStreamReplicationHandler
/// for analytics data synchronisation from DynamoDB to Aurora PostgreSQL.
///
/// Requirements: 20.4
/// </summary>
public sealed class DynamoDbStreamReplicationFunction
{
    private readonly DynamoDbStreamReplicationHandler _replicationHandler;
    private readonly ILogger<DynamoDbStreamReplicationFunction> _logger;

    public DynamoDbStreamReplicationFunction()
    {
        var services = ServiceProviderFactory.Create();
        _replicationHandler = services.GetRequiredService<DynamoDbStreamReplicationHandler>();
        _logger = services.GetRequiredService<ILogger<DynamoDbStreamReplicationFunction>>();
    }

    /// <summary>
    /// Entry point invoked by DynamoDB Streams via EventBridge pipe.
    /// Delegates to the existing DynamoDbStreamReplicationHandler for Aurora replication.
    /// </summary>
    [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
    public async Task HandleAsync(DynamoDbStreamEvent streamEvent, ILambdaContext context)
    {
        var recordCount = streamEvent.Records?.Count ?? 0;

        _logger.LogInformation(
            "DynamoDB Stream replication triggered with {RecordCount} records. RequestId: {RequestId}",
            recordCount,
            context.AwsRequestId);

        try
        {
            await _replicationHandler.HandleAsync(streamEvent);

            _logger.LogInformation(
                "DynamoDB Stream replication completed. {RecordCount} records processed.",
                recordCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "DynamoDB Stream replication failed for batch of {RecordCount} records.",
                recordCount);
            throw;
        }
    }
}
