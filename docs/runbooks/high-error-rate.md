# Runbook: High Error Rate

## Alarm

**Name:** `{env}-guidedmentor-high-error-rate`  
**Trigger:** API Gateway 5xx error rate exceeds 1% over 5 minutes  
**Severity:** P2 (escalate to P1 if sustained >15 minutes)

## Response Steps

### 1. Check CloudWatch Logs Insights for Error Patterns

```
fields @timestamp, @message, @logStream
| filter @message like /(?i)error|exception|fail/
| sort @timestamp desc
| limit 50
```

Run against all Lambda log groups:
- `/aws/lambda/{env}-guidedmentor-identity`
- `/aws/lambda/{env}-guidedmentor-mentoring`
- `/aws/lambda/{env}-guidedmentor-content`
- `/aws/lambda/{env}-guidedmentor-engagement`

### 2. Identify Affected Lambda Function

Check per-function error metrics in CloudWatch:
- Namespace: `AWS/Lambda`
- Metric: `Errors`
- Group by: `FunctionName`

Narrow down which bounded context is failing.

### 3. Check Recent Deployments

```bash
aws lambda list-versions-by-function \
  --function-name {env}-guidedmentor-{context} \
  --query 'Versions[-3:].[Version,Description,LastModified]' \
  --output table
```

Compare alarm trigger time with deployment timestamps.

### 4. Rollback if Deploy-Related

If errors correlate with a recent deployment:

```bash
# Get previous version number
PREV_VERSION=$(aws lambda get-alias \
  --function-name {env}-guidedmentor-{context} \
  --name live \
  --query 'FunctionVersion' \
  --output text)

# Rollback to previous version
aws lambda update-alias \
  --function-name {env}-guidedmentor-{context} \
  --name live \
  --function-version {previous-version}
```

### 5. Escalate if Infrastructure Issue

If errors are not deploy-related:
- Check AWS Health Dashboard for service issues
- Check DynamoDB throttling (see `ddb-throttled.md`)
- Check Bedrock availability (see `bedrock-failures.md`)
- Escalate to on-call engineer with findings

## Resolution

- Confirm error rate returns below 1%
- Alarm transitions to OK state
- Document root cause in incident log
