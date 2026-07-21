# Add Local Dev Mock Service

Scaffolds a no-op mock implementation of an external service interface for local development. Used when the real implementation requires cloud resources (AWS, external APIs) not available locally.

## When to use
- Adding a new external dependency (EventBridge, SES, AppSync, Bedrock, etc.)
- The interface is in the Application layer but the real implementation needs cloud credentials
- You want the backend to start and run locally without the external dependency

## Steps

1. **Identify the interface** in the Application layer (e.g., `IEventBridgePublisher`, `INotificationPublisher`)
2. **Create the mock** in `src/Shared/GuidedMentor.LocalDev/Mocks/Mock{ServiceName}.cs`:
   - Class must be `internal sealed`
   - Implement all interface methods as no-ops returning `Task.CompletedTask`
   - Log each call to `Console.WriteLine($"[DEV] {ServiceName}: {Action} — {details}")` or `ILogger`
   - Use file-scoped namespace: `namespace GuidedMentor.LocalDev.Mocks;`
3. **Register in Program.cs** (composition root):
   - Use `builder.Services.AddSingleton<IInterface, MockImplementation>();` for stateless mocks
   - Use `builder.Services.AddScoped<IInterface, MockImplementation>();` if it depends on DbContext
   - Place after repository registrations, before SignalR/Hangfire section
4. **Verify** the backend starts without DI validation errors

## Checklist
- [ ] Interface identified in Application layer
- [ ] Mock class created in `LocalDev/Mocks/` with `internal sealed` modifier
- [ ] All interface methods implemented (no-op + console log)
- [ ] File-scoped namespace used
- [ ] Registered in `Program.cs`
- [ ] Backend starts cleanly (`dotnet run --project src/Shared/GuidedMentor.LocalDev`)

## Example

```csharp
using GuidedMentor.Mentoring.Application.Interfaces;

namespace GuidedMentor.LocalDev.Mocks;

internal sealed class MockEventBridgePublisher : IEventBridgePublisher
{
    public Task PublishSessionAcceptedAsync(Guid sessionId, Guid menteeId, Guid mentorId, CancellationToken ct = default)
    {
        Console.WriteLine($"[DEV] EventBridge: SessionAccepted — session={sessionId}");
        return Task.CompletedTask;
    }
}
```
