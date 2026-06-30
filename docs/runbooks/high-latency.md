# Runbook: High Latency (P99)

## Alarm

**Name:** `{env}-guidedmentor-high-latency-p99`  
**Trigger:** API Gateway P99 latency exceeds 5 seconds  
**Severity:** P3 (escalate to P2 if sustained >10 minutes)

## Response Steps

### 1. Check DynamoDB Consumed Capacity

```bash
aws cloudwatch get-metric-statistics \
  --namespace AWS/DynamoDB \
  --metric-name ConsumedReadCapacityUnits \
  --dimensions Name=TableName,Value={env}-guidedmentor-sessions \
  --start-time $(date -u -d '30 minutes ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 300 \
  --statistics Sum
```

Check for sudden spikes that may indicate hot partitions or unexpected load.

### 2. Check Lambda Cold Starts

Query CloudWatch Logs Insights:

```
filter @type = "REPORT"
| stats count() as invocations,
        count(@initDuration) as coldStarts,
        avg(@initDuration) as avgColdStart,
        max(@duration) as maxDuration
  by bin(5m)
| sort bin desc
```

If cold starts are elevated, consider provisioned concurrency (step 5).

### 3. Check Bedrock API Latency

For Content bounded context:

```
fields @timestamp, @duration, @message
| filter @message like /Bedrock/
| stats avg(@duration) as avgLatency, max(@duration) as maxLatency by bin(5m)
| sort bin desc
```

Bedrock model invocations can take 2-10s — this may be normal for content generation endpoints.

### 4. Scale if Capacity Issue

DynamoDB tables use on-demand billing (no scaling needed). If throttling occurs, see `ddb-throttled.md`.

For Lambda concurrency limits:
```bash
aws lambda get-function-concurrency \
  --function-name {env}-guidedmentor-{context}
```

### 5. Enable Provisioned Concurrency if Cold-Start Related

If cold starts are the dominant latency contributor:

```bash
aws lambda put-provisioned-concurrency-config \
  --function-name {env}-guidedmentor-{context} \
  --qualifier live \
  --provisioned-concurrent-executions 5
```

> **Note:** Provisioned concurrency incurs cost. Use only as a temporary measure during incidents, then evaluate if permanent configuration is needed.

## Resolution

- Confirm P99 latency returns below 5s
- Alarm transitions to OK state
- If Bedrock-related: latency may be inherent to model inference — consider adjusting threshold for content endpoints
