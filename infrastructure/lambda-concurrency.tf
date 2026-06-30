# =============================================================================
# Lambda Concurrency Configuration (HA/DR gap fix #3)
# Prevents any single function from consuming all account-level concurrency
# (default 1000). This stops cascading failures from burst traffic.
#
# API functions get 100 (handles ~100 concurrent requests per context)
# Background jobs get 5-20 (they're scheduled, not burst)
# =============================================================================

variable "lambda_concurrency_config" {
  description = "Reserved concurrency per Lambda function (HA/DR gap fix #3)"
  type        = map(number)
  default = {
    "identity"               = 100
    "mentoring"              = 100
    "content"                = 50
    "engagement"             = 100
    "lock-cleanup"           = 5
    "notification-digest"    = 5
    "availability-reminder"  = 5
    "analytics-aggregation"  = 10
    "ddb-stream-replication" = 20
  }
}

# API functions — disable Lambda-level retry (API Gateway handles retries at client)
resource "aws_lambda_function_event_invoke_config" "api_functions" {
  for_each = toset(["identity", "mentoring", "content", "engagement"])

  function_name = "${var.environment}-guidedmentor-${each.key}"

  maximum_retry_attempts       = 0   # API calls should not auto-retry at Lambda level
  maximum_event_age_in_seconds = 60  # Discard events older than 60s — stale requests are useless
}
