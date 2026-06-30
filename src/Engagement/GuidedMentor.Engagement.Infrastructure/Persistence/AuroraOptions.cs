namespace GuidedMentor.Engagement.Infrastructure.Persistence;

/// <summary>
/// Configuration options for Aurora PostgreSQL analytics database connection.
/// Connected via RDS Proxy for Lambda connection pooling.
/// </summary>
public sealed class AuroraOptions
{
    public const string SectionName = "Aurora";

    /// <summary>
    /// Connection string for the Aurora PostgreSQL analytics database.
    /// In production, points to the RDS Proxy endpoint.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
}
