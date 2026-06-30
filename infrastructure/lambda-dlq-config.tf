# =============================================================================
# Lambda Async Invoke Dead Letter Configuration
# Captures failed async Lambda invocations (not covered by EventBridge DLQs).
# FREE — SQS queues have 1M free requests/month.
# =============================================================================

resource "aws_sqs_queue" "lambda_async_dlq" {
  name                      = "${var.environment}-guidedmentor-lambda-async-dlq"
  message_retention_seconds = 1209600 # 14 days

  tags = {
    Environment = var.environment
    Service     = "guidedmentor"
    Purpose     = "lambda-async-failures"
  }
}
