# =============================================================================
# Custom CloudWatch Log Metric Filters
# Turns log patterns into countable metrics for dashboards and alarms. FREE.
# =============================================================================

# Metric: Count of prompt injection attempts detected
resource "aws_cloudwatch_log_metric_filter" "prompt_injection_detected" {
  count          = var.enable_alarms ? 1 : 0
  name           = "${local.name_prefix}-prompt-injection"
  pattern        = "\"Prompt injection pattern detected\""
  log_group_name = "/aws/lambda/${local.name_prefix}-content"

  metric_transformation {
    name      = "PromptInjectionAttempts"
    namespace = "GuidedMentor/${var.environment}"
    value     = "1"
  }
}

# Metric: Count of rate limit hits
resource "aws_cloudwatch_log_metric_filter" "rate_limit_exceeded" {
  count          = var.enable_alarms ? 1 : 0
  name           = "${local.name_prefix}-rate-limit"
  pattern        = "\"Rate limit exceeded\""
  log_group_name = "/aws/lambda/${local.name_prefix}-engagement"

  metric_transformation {
    name      = "RateLimitExceeded"
    namespace = "GuidedMentor/${var.environment}"
    value     = "1"
  }
}

# Metric: Count of session plan generation failures
resource "aws_cloudwatch_log_metric_filter" "plan_generation_failed" {
  count          = var.enable_alarms ? 1 : 0
  name           = "${local.name_prefix}-plan-failed"
  pattern        = "\"Session plan generation failed\""
  log_group_name = "/aws/lambda/${local.name_prefix}-content"

  metric_transformation {
    name      = "PlanGenerationFailures"
    namespace = "GuidedMentor/${var.environment}"
    value     = "1"
  }
}

# Metric: Count of account lockouts
resource "aws_cloudwatch_log_metric_filter" "account_lockout" {
  count          = var.enable_alarms ? 1 : 0
  name           = "${local.name_prefix}-account-lockout"
  pattern        = "\"Account locked\""
  log_group_name = "/aws/lambda/${local.name_prefix}-identity"

  metric_transformation {
    name      = "AccountLockouts"
    namespace = "GuidedMentor/${var.environment}"
    value     = "1"
  }
}
