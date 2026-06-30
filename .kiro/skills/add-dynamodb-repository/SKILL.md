---
name: add-dynamodb-repository
description: Scaffolds a DynamoDB repository with interface in Application, implementation in Infrastructure, and Terraform table config
inclusion: manual
---

# Add DynamoDB Repository

Creates a complete data access layer for a new DynamoDB table or aggregate.

## Input Required
- Bounded context (Identity | Mentoring | Content | Engagement)
- Entity/Aggregate name (e.g., Feedback, Bookmark)
- Table name and key schema (PK, SK, GSIs)
- Key operations needed (save, get, query, delete)

## Steps

1. **Define interface** in `src/{Context}/GuidedMentor.{Context}.Application/Interfaces/`:
   ```csharp
   public interface I{Entity}Repository
   {
       Task<{Entity}?> GetByIdAsync({IdType} id, CancellationToken ct = default);
       Task SaveAsync({Entity} entity, CancellationToken ct = default);
   }
   ```

2. **Create implementation** in `src/{Context}/GuidedMentor.{Context}.Infrastructure/Repositories/`:
   - Use `IAmazonDynamoDB` directly (AOT-compatible)
   - Batch writes in chunks of 25
   - ConsistentRead for critical reads
   - Conditional writes for idempotency

3. **Add Terraform table** in `infrastructure/modules/{context}/main.tf`:
   - PAY_PER_REQUEST billing
   - PITR enabled
   - CMK encryption conditional on `var.enable_cmk`
   - DataClassification tag

4. **Register in DI** in Infrastructure DependencyInjection.cs

5. **Verify**: `dotnet build` passes

## Conventions
- Table naming: `{environment}-guidedmentor-{entity-plural}`
- Always enable point-in-time recovery
- Interface lives in Application, implementation in Infrastructure
