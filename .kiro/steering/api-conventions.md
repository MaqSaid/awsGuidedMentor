---
inclusion: fileMatch
fileMatchPattern: "**/Endpoints/*.cs"
---

# API Endpoint Conventions

## URL Pattern
- Always versioned: `/v1/{resource}`
- Plural nouns: `/v1/mentors`, `/v1/sessions`
- Actions as sub-paths: `/v1/sessions/{id}/complete`

## Handler Pattern
```csharp
group.MapPost("/{id:guid}/action", async (Guid id, HttpContext httpContext, IMediator mediator, CancellationToken ct) =>
{
    var userId = GetUserId(httpContext);
    if (userId is null) return Results.Unauthorized();
    var command = new ActionCommand(userId.Value, id);
    var result = await mediator.Send(command, ct);
    return result.IsSuccess ? Results.Ok(...) : Results.BadRequest(new { Error = result.Error });
});
```

## Error Response
Always: `{ statusCode, error, message, correlationId }`

## Auth
- Extract userId: `httpContext.User.FindFirst("sub")?.Value`
- Admin check: verify `cognito:groups` contains "Super_Admin"
- Anonymous paths: `/v1/health`, `/v1/auth/*`
