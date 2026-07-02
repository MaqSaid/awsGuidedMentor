---
name: add-background-job
description: Scaffolds a new background job using Hangfire with recurring schedule or fire-and-forget pattern
inclusion: manual
---

# Add Background Job (Hangfire)

Creates a scheduled or event-driven background job using Hangfire (replaces EventBridge Lambda).

## Input Required
- Job name (e.g., CleanupExpiredTokens, SendCompletionReminder)
- Schedule (cron expression or one-time delay)
- What it does
- Which data it touches

## Steps

### 1. Create job class
`src/Shared/GuidedMentor.SharedInfrastructure/Jobs/{JobName}Job.cs`

```csharp
namespace GuidedMentor.SharedInfrastructure.Jobs;

public sealed class {JobName}Job
{
    private readonly GuidedMentorDbContext _db;
    private readonly ILogger<{JobName}Job> _logger;

    public {JobName}Job(GuidedMentorDbContext db, ILogger<{JobName}Job> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("{JobName} started");
        // Job logic here
        _logger.LogInformation("{JobName} completed");
    }
}
```

### 2. Register the recurring job
In `Program.cs` or a startup extension:
```csharp
RecurringJob.AddOrUpdate<{JobName}Job>(
    "{job-name}",
    job => job.ExecuteAsync(CancellationToken.None),
    "*/5 * * * *"); // every 5 minutes
```

### 3. Write unit test
Test the job logic in isolation (mock DbContext or use in-memory provider).

### 4. Verify
`dotnet build -c Release`

## Conventions
- One class per job
- Always log start/end
- Handle partial failures gracefully
- Use `ILogger` for observability
- Cron expressions: use https://crontab.guru for reference

## Common schedules
- Token cleanup: `*/5 * * * *` (every 5 min)
- Completion reminders: `0 9 * * *` (daily 9 AM)
- Analytics aggregation: `0 * * * *` (hourly)
- Opportunity expiry: `0 0 * * *` (daily midnight)
