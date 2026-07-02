namespace GuidedMentor.SharedInfrastructure.Data.Entities;

/// <summary>
/// Persistence model for the meetups table.
/// </summary>
public sealed class MeetupEntity
{
    public Guid Id { get; set; }
    public string Chapter { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? VenueName { get; set; }
    public string? VenueAddress { get; set; }
    public string? EventUrl { get; set; }
    public Guid CreatedBy { get; set; }
    public bool IsCancelled { get; set; }
    public Guid[] ConfirmedAttendees { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}
