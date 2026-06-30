# Chaos Testing Guide

## Overview

GuidedMentor uses AWS Fault Injection Simulator (FIS) to validate resilience patterns. Chaos experiments are defined as Terraform resources and deployed only to staging (`enable_chaos_testing = true`).

**Cost:** FIS experiment templates are free. You only pay for affected AWS resources during the experiment (Lambda invocations, etc.), which are already part of normal staging costs.

## Prerequisites

- Chaos testing is **staging only** — never run against production
- Ensure the team is aware before starting experiments
- Run during business hours so team members can observe and respond
- Verify monitoring dashboards and alarms are active before testing

## Available Experiments

### 1. Lambda Throttle (Content Context)

**Purpose:** Validates that the circuit breaker trips and the platform degrades gracefully when Lambda invocations are delayed.

**What it does:**
- Adds 5-second delay to 50% of Content Lambda invocations
- Duration: 5 minutes

**Expected behavior:**
- Circuit breaker opens after threshold failures
- API returns cached/degraded response instead of timeout
- High latency alarm fires
- Circuit breaker recovers after experiment ends

### 2. DynamoDB API Error Injection

**Purpose:** Validates retry logic and graceful degradation when DynamoDB operations fail.

**What it does:**
- Injects API unavailable errors for 30% of GetItem/PutItem/Query operations
- Duration: 3 minutes

**Expected behavior:**
- Polly retry policies handle transient failures
- Some requests succeed on retry
- DLQ receives events that exhaust retries
- DynamoDB throttled alarm fires
- Full recovery after experiment ends

## Running an Experiment

### Start

```bash
# List available experiment templates
aws fis list-experiment-templates \
  --query 'experimentTemplates[].{id:id,description:description}' \
  --output table

# Start an experiment
aws fis start-experiment \
  --experiment-template-id {template-id} \
  --tags "RunBy=$(whoami),Date=$(date +%Y-%m-%d)"
```

### Monitor During Experiment

1. **CloudWatch Dashboard:** Watch `{env}-guidedmentor-ops` dashboard for metric changes
2. **Alarms:** Expect relevant alarms to fire (this validates alerting works)
3. **Logs:** Monitor Lambda log groups for error patterns
4. **DLQ:** Check if failed events are correctly routed to dead letter queues

```bash
# Watch experiment status
aws fis get-experiment --id {experiment-id} \
  --query 'experiment.{state:state.status,reason:state.reason}'

# Check alarm states
aws cloudwatch describe-alarms \
  --alarm-name-prefix "staging-guidedmentor" \
  --state-value ALARM \
  --query 'MetricAlarms[].AlarmName'
```

### Abort (Emergency Stop)

```bash
aws fis stop-experiment --id {experiment-id}
```

The experiment stops immediately and all injected faults are removed. Recovery is automatic.

## Post-Experiment Validation

After the experiment completes, verify:

1. **Recovery:** All alarms return to OK within 5 minutes of experiment end
2. **No data loss:** Check DLQ for accumulated messages and verify they can be replayed
3. **Circuit breaker reset:** Confirm circuit breaker transitions from open → half-open → closed
4. **Metrics normalization:** Latency and error rate return to baseline

```bash
# Verify all alarms are OK
aws cloudwatch describe-alarms \
  --alarm-name-prefix "staging-guidedmentor" \
  --state-value ALARM

# Check DLQ depths
for DLQ in lock-cleanup notification-digest availability-reminder analytics-aggregation; do
  aws sqs get-queue-attributes \
    --queue-url https://sqs.{region}.amazonaws.com/{account}/staging-guidedmentor-dlq-${DLQ} \
    --attribute-names ApproximateNumberOfMessages
done
```

## Scheduling Chaos Tests

Recommended cadence:
- **Weekly:** Run one experiment during staging regression testing
- **Pre-release:** Run all experiments before promoting to production
- **After architecture changes:** Re-validate resilience patterns

## Troubleshooting

| Issue | Resolution |
|-------|-----------|
| Experiment fails to start | Check FIS IAM role has required permissions |
| Alarms don't fire | Verify `enable_alarms = true` in staging |
| No recovery after stop | Check Lambda function configuration wasn't permanently changed |
| DLQ accumulates post-experiment | Run the DLQ replay procedure (see `dlq-messages.md`) |
