---
name: add-api-endpoint
description: Scaffolds a new API endpoint following Clean Architecture with command, handler, validator, tests, and endpoint mapping
inclusion: manual
---

# Add API Endpoint

Scaffolds a complete endpoint following the project's Clean Architecture pattern.

## Input Required
- Bounded context (Identity | Mentoring | Content | Engagement)
- HTTP method and path (e.g., `POST /v1/sessions/{id}/feedback`)
- Brief description of what it does

## Steps

### 0. Define the API contract first

Before writing any implementation code, define the endpoint in the OpenAPI spec:

1. Add the operation to the appropriate Api project's endpoint group with full metadata:
   ```csharp
   group.MapPost("/path", handler)
       .WithName("OperationName")
       .WithTags("FeatureGroup")
       .WithDescription("What this endpoint does.")
       .Produces<ResponseDto>(StatusCodes.Status200OK)
       .Produces(StatusCodes.Status400BadRequest)
       .ProducesValidationProblem()
       .RequireAuthorization();
   ```
2. This ensures the contract is visible in `/openapi/v1.json` before implementation begins.
3. The handler can initially return a stub (`Results.Ok()` or `Results.StatusCode(501)`) — the contract shape matters first.
4. Run `dotnet build` to verify the spec generates cleanly.
5. Frontend can generate a typed client from this spec immediately (see "Frontend API Client Generation" below).

### 1. Create the Command/Query record

In `src/{Context}/GuidedMentor.{Context}.Application/Commands/` or `Queries/`:
```csharp
public sealed record {Verb}{Noun}Command(...params...) : IRequest<Result<T>>, IAuditableCommand;
```
- Use `IRequest<Result>` for commands, `IRequest<T>` for queries
- Implement `IAuditableCommand` for state-changing operations

### 2. Create the Handler

In the same directory:
```csharp
public sealed class {Verb}{Noun}Handler : IRequestHandler<{Command}, Result<T>>
```
- Inject repository interfaces (defined in Application, implemented in Infrastructure)
- Return `Result.Success()` or `Result.Failure("message")`

### 3. Create the FluentValidation Validator

```csharp
public sealed class {Command}Validator : AbstractValidator<{Command}>
```
- Validate all input constraints from the requirements

### 4. Create Unit Tests

In `tests/GuidedMentor.{Context}.Tests/`:
- Test file: `{Handler}Tests.cs`
- Cover: happy path, validation failures, edge cases
- Use NSubstitute for mocks, FluentAssertions for assertions

### 5. Wire the endpoint implementation

In `src/{Context}/GuidedMentor.{Context}.Api/`:
- Replace the stub handler from Step 0 with the real MediatR dispatch
- Map HTTP method to MediatR Send
- Extract userId from JWT claims
- Return appropriate status codes (200, 201, 400, 401, 403, 404)

### 6. Add JSON serialization (for Native AOT)

- Add request/response DTOs to the `[JsonSerializable]` context in Program.cs

### 7. Verify

Run `dotnet build` then `dotnet test`

## Frontend API Client Generation

After the endpoint contract is defined in Step 0:

1. The OpenAPI spec at `/openapi/v1.json` is immediately updated with the new operation.
2. Run the OpenAPI client generator to update `api-client.ts`:
   ```bash
   npx openapi-typescript http://localhost:{port}/openapi/v1.json -o frontend/host-shell/src/types/api-client.ts
   ```
3. Frontend can develop against the contract using MSW mocks while backend implements the handler:
   - Add a mock handler in `frontend/host-shell/src/mocks/handlers/` matching the new endpoint
   - The typed client gives the frontend team compile-time safety against the contract
   - Backend and frontend can work in parallel — the spec is the single source of truth

## Checklist
- [ ] Endpoint contract defined with full OpenAPI metadata (Step 0)
- [ ] Command/Query record with correct return type
- [ ] Handler with sealed class, proper DI
- [ ] FluentValidation validator
- [ ] Unit tests (3+ test cases)
- [ ] Endpoint mapping with auth (stub replaced with real handler)
- [ ] JSON serialization context updated
- [ ] Frontend API client regenerated
- [ ] Build passes, tests pass
