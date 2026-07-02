namespace GuidedMentor.LocalDev.Mocks;

/// <summary>
/// Provides pre-built session plan responses for local development.
/// </summary>
public static class MockSessionPlans
{
    public static object GetMockPlan(Guid sessionId) => new
    {
        sessionId,
        sessionTitle = "AWS Solutions Architecture Fundamentals",
        agenda = new[]
        {
            new { title = "Introduction & Goal Setting", durationMinutes = 5, description = "Review mentee's current AWS knowledge and define session objectives." },
            new { title = "Architecture Patterns Discussion", durationMinutes = 10, description = "Explore common AWS architecture patterns relevant to the mentee's goals." },
            new { title = "Hands-on Design Exercise", durationMinutes = 12, description = "Work through a real-world scenario designing a scalable AWS solution." },
            new { title = "Q&A and Next Steps", durationMinutes = 8, description = "Address questions, assign follow-up tasks, and plan next session." }
        },
        preworkTasks = new[] { "Review AWS Well-Architected Framework pillars", "List 3 challenges in your current project", "Read about serverless patterns" },
        followUpTasks = new[] { "Complete the architecture diagram started in session", "Try deploying a sample Lambda function", "Document questions for next session" },
        progressPercent = 0,
        status = "active"
    };
}
