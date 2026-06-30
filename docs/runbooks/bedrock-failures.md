# Runbook: Bedrock API Failures

## Alarm

**Name:** `{env}-guidedmentor-bedrock-failures`  
**Trigger:** More than 3 Bedrock API failures within 10 minutes  
**Severity:** P3 (escalate to P2 if session plan generation is fully blocked)

## Response Steps

### 1. Check AWS Health Dashboard for Bedrock Service Issues

- Navigate to [AWS Health Dashboard](https://health.aws.amazon.com/health/status)
- Filter by region: `ap-southeast-2`
- Check for active events affecting Amazon Bedrock

Also check:
```bash
aws health describe-events \
  --filter "services=BEDROCK,eventStatusCodes=open,upcoming" \
  --region us-east-1
```

### 2. Verify Guardrails Configuration

If guardrails are enabled, check if they are blocking legitimate requests:

```bash
aws bedrock get-guardrail \
  --guardrail-identifier {guardrail-id} \
  --guardrail-version DRAFT
```

Check CloudWatch Logs for guardrail intervention patterns:
```
fields @timestamp, @message
| filter @message like /GuardrailIntervention|GUARDRAIL/
| sort @timestamp desc
| limit 20
```

### 3. Check Circuit Breaker State

The Content bounded context uses Polly circuit breaker for Bedrock calls. Check application logs:

```
fields @timestamp, @message
| filter @message like /CircuitBreaker|circuit.*open|circuit.*half/
| sort @timestamp desc
| limit 10
```

If circuit is open, it will automatically transition to half-open after the configured break duration (30s default).

### 4. Monitor EventBridge DLQ for Session Plans

Session plan generation uses async EventBridge invocations with automatic retry:
- Events retry 2x with exponential backoff
- Failed events route to the DLQ

Check DLQ for failed session plan events:
```bash
aws sqs get-queue-attributes \
  --queue-url https://sqs.{region}.amazonaws.com/{account}/{env}-guidedmentor-dlq-content \
  --attribute-names ApproximateNumberOfMessages
```

### 5. If Regional Issue — Wait for AWS Resolution

If AWS Health Dashboard confirms a regional Bedrock issue:
- The platform degrades gracefully (session plans queue for later processing)
- No manual intervention needed — EventBridge retries will process once service recovers
- Communicate to users that AI-generated content may be temporarily delayed
- Monitor DLQ depth — messages will be processed when Bedrock recovers

## Resolution

- Confirm Bedrock failures return to 0
- Alarm transitions to OK state
- If DLQ accumulated messages, verify they are being reprocessed
- No data loss expected — events are durable in EventBridge/SQS
