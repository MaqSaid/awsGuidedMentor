namespace GuidedMentor.Identity.Application.Interfaces;

/// <summary>
/// Provides aggregated data for the admin dashboard.
/// Implementation queries DynamoDB/Aurora for user counts, session counts, and health status.
/// </summary>
public interface IAdminDashboardDataService
{
    /// <summary>
    /// Returns the total number of registered users.
    /// </summary>
    Task<int> GetTotalUserCountAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the number of users with the specified active role.
    /// </summary>
    Task<int> GetUserCountByRoleAsync(string role, CancellationToken ct = default);

    /// <summary>
    /// Returns the number of sessions currently in an active state.
    /// </summary>
    Task<int> GetActiveSessionCountAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the overall platform health status (e.g., "Healthy", "Degraded", "Unhealthy")
    /// based on CloudWatch alarm states.
    /// </summary>
    Task<string> GetPlatformHealthStatusAsync(CancellationToken ct = default);
}
