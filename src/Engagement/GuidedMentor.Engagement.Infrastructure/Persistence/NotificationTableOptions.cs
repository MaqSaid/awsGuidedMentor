namespace GuidedMentor.Engagement.Infrastructure.Persistence;

/// <summary>
/// Configuration options for the DynamoDB Notifications_Table.
/// </summary>
public sealed class NotificationTableOptions
{
    public const string SectionName = "Notifications";

    public string TableName { get; set; } = "Notifications_Table";
}
