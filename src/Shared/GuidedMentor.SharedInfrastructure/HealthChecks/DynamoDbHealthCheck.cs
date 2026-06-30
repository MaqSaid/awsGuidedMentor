using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.SharedInfrastructure.HealthChecks;

/// <summary>
/// Custom health check that verifies DynamoDB connectivity by performing a DescribeTable call.
/// Returns Healthy if the table is ACTIVE, Degraded if UPDATING, Unhealthy otherwise.
/// </summary>
public sealed class DynamoDbHealthCheck : IHealthCheck
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _tableName;
    private readonly ILogger<DynamoDbHealthCheck> _logger;

    public DynamoDbHealthCheck(
        IAmazonDynamoDB dynamoDb,
        string tableName,
        ILogger<DynamoDbHealthCheck> logger)
    {
        _dynamoDb = dynamoDb;
        _tableName = tableName;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _dynamoDb.DescribeTableAsync(
                new DescribeTableRequest { TableName = _tableName },
                cancellationToken);

            var status = response.Table.TableStatus;

            return status.Value switch
            {
                "ACTIVE" => HealthCheckResult.Healthy($"DynamoDB table '{_tableName}' is active."),
                "UPDATING" => HealthCheckResult.Degraded($"DynamoDB table '{_tableName}' is updating."),
                _ => HealthCheckResult.Unhealthy($"DynamoDB table '{_tableName}' status: {status.Value}")
            };
        }
        catch (ResourceNotFoundException)
        {
            _logger.LogError("DynamoDB table '{TableName}' not found", _tableName);
            return HealthCheckResult.Unhealthy($"DynamoDB table '{_tableName}' does not exist.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DynamoDB health check failed for table '{TableName}'", _tableName);
            return HealthCheckResult.Unhealthy($"DynamoDB connectivity failed: {ex.Message}");
        }
    }
}
