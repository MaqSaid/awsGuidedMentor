namespace GuidedMentor.LocalDev.Mocks;

/// <summary>
/// Provides mock dashboard data for local development.
/// </summary>
public static class MockDashboards
{
    public static object GetMenteeDashboard() => new
    {
        activeSessions = new[]
        {
            new { sessionId = Guid.NewGuid(), mentorName = "Sarah Chen", sessionTitle = "AWS Architecture Fundamentals", progressPercent = 65, status = "active" },
            new { sessionId = Guid.NewGuid(), mentorName = "James Wilson", sessionTitle = "Serverless Deep Dive", progressPercent = 20, status = "active" }
        },
        topMentors = new[]
        {
            new { mentorId = Guid.NewGuid(), displayName = "Dr. Emily Park", chapter = "Sydney", compatibilityScore = 92 },
            new { mentorId = Guid.NewGuid(), displayName = "Michael Torres", chapter = "Melbourne", compatibilityScore = 87 },
            new { mentorId = Guid.NewGuid(), displayName = "Aisha Patel", chapter = "Brisbane", compatibilityScore = 81 }
        },
        stats = new { completedSessions = 3, totalChecklistItems = 24, completionPercentage = 75 }
    };

    public static object GetMentorDashboard() => new
    {
        pendingRequests = new[]
        {
            new { sessionId = Guid.NewGuid(), menteeName = "Alex Kumar", goal = "Career transition to cloud", compatibilityScore = 88, requestedAt = DateTime.UtcNow.AddDays(-1) }
        },
        activeMentees = new[]
        {
            new { sessionId = Guid.NewGuid(), menteeName = "Jordan Lee", sessionTitle = "CI/CD Pipeline Design", progressPercent = 45 },
            new { sessionId = Guid.NewGuid(), menteeName = "Sam Rivera", sessionTitle = "AWS Certification Prep", progressPercent = 80 }
        },
        capacity = new { active = 2, max = 5 }
    };
}
