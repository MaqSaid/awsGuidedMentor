---
name: add-background-job
description: Scaffolds a new background job with Lambda function, MediatR command/handler, and EventBridge scheduler rule
inclusion: manual
---

# Add Background Job

Creates a scheduled or event-driven background job as a Lambda function.

## Input Required
- Job name (e.g., SendWeeklyDigest)
- Schedule (rate or cron expression)
- What it does
- Which data it touches

## Steps

1. **Create MediatR Command** in `src/BackgroundJobs/Commands/`:
   ```csharp
   public sealed record {JobName}Command() : IRequest<Result>;
   ```

2. **Create Handler** in same directory:
   - Inject repositories, log start/end, handle partial failures

3. **Create Lambda Function** in `src/BackgroundJobs/Functions/`:
   - Use ServiceProviderFactory for DI
   - Mark with `[LambdaSerializer]`
   - Log AwsRequestId for tracing

4. **Add EventBridge Scheduler** in `infrastructure/modules/events/main.tf`

5. **Add SQS DLQ** (14-day retention)

6. **Verify**: `dotnet build` passes

## Conventions
- One Lambda per job
- Always include DLQ
- Use Result return type
- Log: items processed, items failed, duration
