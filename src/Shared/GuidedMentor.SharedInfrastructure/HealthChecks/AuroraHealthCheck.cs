using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace GuidedMentor.SharedInfrastructure.HealthChecks;

/// <summary>
/// Custom health check that verifies Aurora PostgreSQL connectivity
/// by executing a simple SELECT 1 query via NpgsqlDataSource.
/// </summary>
public sealed class AuroraHealthCheck : IHealthCheck
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<AuroraHealthCheck> _logger;

    public AuroraHealthCheck(
        NpgsqlDataSource dataSource,
        ILogger<AuroraHealthCheck> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 5;

            var result = await command.ExecuteScalarAsync(cancellationToken);

            return result is 1
                ? HealthCheckResult.Healthy("Aurora PostgreSQL is reachable.")
                : HealthCheckResult.Degraded("Aurora PostgreSQL returned unexpected result.");
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Aurora PostgreSQL health check failed");
            return HealthCheckResult.Unhealthy($"Aurora PostgreSQL connectivity failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Aurora health check failed with unexpected error");
            return HealthCheckResult.Unhealthy($"Aurora PostgreSQL unavailable: {ex.Message}");
        }
    }
}
