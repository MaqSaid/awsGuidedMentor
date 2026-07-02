---
name: add-signalr-notification
description: Wires a new real-time notification type through SignalR hub to the React frontend
inclusion: manual
---

# Add SignalR Notification

Creates a real-time notification that pushes from backend to connected frontend clients via SignalR.

## Input Required
- Notification type name (e.g., SessionAccepted, NewOpportunity, MagicLinkSent)
- Payload shape (what data the frontend needs)
- Trigger point (which command/handler sends it)
- Target audience (specific user, role, or broadcast)

## Steps

### 1. Define notification payload
`src/Shared/GuidedMentor.SharedInfrastructure/Hubs/Notifications/{NotificationType}Notification.cs`

```csharp
namespace GuidedMentor.SharedInfrastructure.Hubs.Notifications;

public sealed record {NotificationType}Notification(
    string Message,
    string? ActionUrl = null
    // Add domain-specific fields
);
```

### 2. Send from handler via IHubContext
In the relevant command handler:
```csharp
private readonly IHubContext<NotificationHub> _hub;

// After business logic succeeds:
await _hub.Clients.User(recipientUserId.ToString())
    .SendAsync("Receive{NotificationType}", new {NotificationType}Notification(
        Message: "...",
        ActionUrl: "/relevant/path"
    ), ct);
```

### 3. Also persist to notifications table
```csharp
await _notificationRepository.SaveAsync(new Notification(
    recipientUserId, "{NotificationType}", message, actionUrl));
```

### 4. Frontend: subscribe in React
In the relevant page or layout component:
```typescript
import { useSignalR } from '@/hooks/useSignalR';

const { connection } = useSignalR();

useEffect(() => {
  connection?.on('Receive{NotificationType}', (notification) => {
    // Show toast, update query cache, etc.
    queryClient.invalidateQueries({ queryKey: ['notifications'] });
    toast.info(notification.message);
  });
  return () => connection?.off('Receive{NotificationType}');
}, [connection]);
```

### 5. Write tests
- Unit test: handler sends to hub (mock IHubContext)
- Integration test: connect SignalR client, trigger event, verify receipt

### 6. Verify
`dotnet build -c Release`

## Conventions
- Hub URL: `/hubs/notifications`
- Method naming: `Receive{NotificationType}` (PascalCase)
- Always persist notification to DB AND push via SignalR
- Frontend uses `useSignalR` hook (shared in `hooks/`)
- Fallback: TanStack Query polling every 30s if WebSocket fails
- Target specific users with `.Clients.User(userId)` — never broadcast sensitive data

## SignalR Hub Setup
The hub is registered in `Program.cs`:
```csharp
app.MapHub<NotificationHub>("/hubs/notifications");
```

Authentication: Hub requires JWT bearer token (same as API endpoints).
The frontend connects with:
```typescript
const connection = new HubConnectionBuilder()
  .withUrl('/hubs/notifications', { accessTokenFactory: () => token })
  .withAutomaticReconnect()
  .build();
```
