# Events Module
# Manages: EventBridge event bus, event rules, scheduler rules, SQS dead-letter queues,
#           and IAM roles for cross-context async operations

locals {
  service_name = "Events"
  name_prefix  = "${var.environment}-guidedmentor"

  common_tags = {
    Environment    = var.environment
    Service        = local.service_name
    BoundedContext = "CrossCutting"
  }
}

# =============================================================================
# Data Sources
# =============================================================================

data "aws_caller_identity" "current" {}

# =============================================================================
# Custom EventBridge Event Bus
# =============================================================================

resource "aws_cloudwatch_event_bus" "main" {
  name = "${local.name_prefix}"

  tags = merge(local.common_tags, {
    Name        = "${local.name_prefix}"
    Description = "GuidedMentor platform events for cross-context communication"
  })
}

# =============================================================================
# EventBridge Rules (event-driven routing on custom bus)
# =============================================================================

# Rule: SessionAccepted → route to Content context (plan generation Lambda)
resource "aws_cloudwatch_event_rule" "session_accepted" {
  name           = "${local.name_prefix}-session-accepted"
  description    = "Routes SessionAccepted events to Content context for plan generation"
  event_bus_name = aws_cloudwatch_event_bus.main.name

  event_pattern = jsonencode({
    source      = ["guidedmentor.mentoring"]
    detail-type = ["SessionAccepted"]
  })

  tags = local.common_tags
}

# Rule: CompletionMarked → route to Engagement context (notification Lambda)
resource "aws_cloudwatch_event_rule" "completion_marked" {
  name           = "${local.name_prefix}-completion-marked"
  description    = "Routes CompletionMarked events to Engagement context for notifications"
  event_bus_name = aws_cloudwatch_event_bus.main.name

  event_pattern = jsonencode({
    source      = ["guidedmentor.mentoring"]
    detail-type = ["CompletionMarked"]
  })

  tags = local.common_tags
}

# Rule: PlanGenerationFailed → route to Content context (retry Lambda)
resource "aws_cloudwatch_event_rule" "plan_generation_failed" {
  name           = "${local.name_prefix}-plan-generation-failed"
  description    = "Routes PlanGenerationFailed events to Content context for async retry"
  event_bus_name = aws_cloudwatch_event_bus.main.name

  event_pattern = jsonencode({
    source      = ["guidedmentor.content"]
    detail-type = ["PlanGenerationFailed"]
  })

  tags = local.common_tags
}

# =============================================================================
# SQS Dead-Letter Queues (one per EventBridge rule target — 14-day retention)
# =============================================================================

resource "aws_sqs_queue" "dlq_lock_cleanup" {
  name                      = "${local.name_prefix}-dlq-lock-cleanup"
  message_retention_seconds = 1209600 # 14 days

  tags = merge(local.common_tags, {
    Name    = "${local.name_prefix}-dlq-lock-cleanup"
    Purpose = "DLQ for lock expiration cleanup scheduler"
  })
}

resource "aws_sqs_queue" "dlq_analytics_aggregation" {
  name                      = "${local.name_prefix}-dlq-analytics-aggregation"
  message_retention_seconds = 1209600 # 14 days

  tags = merge(local.common_tags, {
    Name    = "${local.name_prefix}-dlq-analytics-aggregation"
    Purpose = "DLQ for analytics aggregation scheduler"
  })
}

resource "aws_sqs_queue" "dlq_notification_digest" {
  name                      = "${local.name_prefix}-dlq-notification-digest"
  message_retention_seconds = 1209600 # 14 days

  tags = merge(local.common_tags, {
    Name    = "${local.name_prefix}-dlq-notification-digest"
    Purpose = "DLQ for notification digest scheduler"
  })
}

resource "aws_sqs_queue" "dlq_availability_reminder" {
  name                      = "${local.name_prefix}-dlq-availability-reminder"
  message_retention_seconds = 1209600 # 14 days

  tags = merge(local.common_tags, {
    Name    = "${local.name_prefix}-dlq-availability-reminder"
    Purpose = "DLQ for availability reminder scheduler"
  })
}

# =============================================================================
# IAM Role for EventBridge Scheduler (invoke Lambda + send to DLQ)
# =============================================================================

resource "aws_iam_role" "scheduler_execution" {
  name = "${local.name_prefix}-scheduler-execution"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          Service = "scheduler.amazonaws.com"
        }
        Action = "sts:AssumeRole"
        Condition = {
          StringEquals = {
            "aws:SourceAccount" = data.aws_caller_identity.current.account_id
          }
        }
      }
    ]
  })

  tags = merge(local.common_tags, {
    Name = "${local.name_prefix}-scheduler-execution"
  })
}

resource "aws_iam_role_policy" "scheduler_invoke_lambda" {
  name = "InvokeLambdaAndSendDLQ"
  role = aws_iam_role.scheduler_execution.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "AllowLambdaInvoke"
        Effect = "Allow"
        Action = [
          "lambda:InvokeFunction"
        ]
        Resource = "arn:aws:lambda:${var.aws_region}:${data.aws_caller_identity.current.account_id}:function:${local.name_prefix}-*"
      },
      {
        Sid    = "AllowSQSSendDLQ"
        Effect = "Allow"
        Action = [
          "sqs:SendMessage"
        ]
        Resource = [
          aws_sqs_queue.dlq_lock_cleanup.arn,
          aws_sqs_queue.dlq_analytics_aggregation.arn,
          aws_sqs_queue.dlq_notification_digest.arn,
          aws_sqs_queue.dlq_availability_reminder.arn
        ]
      }
    ]
  })
}

# =============================================================================
# IAM Role for EventBridge Rule Targets (invoke Lambda)
# =============================================================================

resource "aws_iam_role" "eventbridge_rule_target" {
  name = "${local.name_prefix}-eventbridge-rule-target"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          Service = "events.amazonaws.com"
        }
        Action = "sts:AssumeRole"
        Condition = {
          StringEquals = {
            "aws:SourceAccount" = data.aws_caller_identity.current.account_id
          }
        }
      }
    ]
  })

  tags = merge(local.common_tags, {
    Name = "${local.name_prefix}-eventbridge-rule-target"
  })
}

resource "aws_iam_role_policy" "eventbridge_invoke_lambda" {
  name = "InvokeLambda"
  role = aws_iam_role.eventbridge_rule_target.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "AllowLambdaInvoke"
        Effect = "Allow"
        Action = [
          "lambda:InvokeFunction"
        ]
        Resource = "arn:aws:lambda:${var.aws_region}:${data.aws_caller_identity.current.account_id}:function:${local.name_prefix}-*"
      }
    ]
  })
}

# =============================================================================
# EventBridge Scheduler Rules (cron-based)
# =============================================================================

# Scheduler: Lock expiration cleanup — every 5 minutes
resource "aws_scheduler_schedule" "lock_cleanup" {
  name       = "${local.name_prefix}-lock-cleanup"
  group_name = "default"

  flexible_time_window {
    mode = "OFF"
  }

  schedule_expression = "rate(5 minutes)"

  target {
    arn      = "arn:aws:lambda:${var.aws_region}:${data.aws_caller_identity.current.account_id}:function:${local.name_prefix}-lock-cleanup"
    role_arn = aws_iam_role.scheduler_execution.arn

    dead_letter_config {
      arn = aws_sqs_queue.dlq_lock_cleanup.arn
    }

    retry_policy {
      maximum_event_age_in_seconds = 300
      maximum_retry_attempts       = 2
    }
  }
}

# Scheduler: Analytics aggregation — every hour (only if Aurora enabled)
resource "aws_scheduler_schedule" "analytics_aggregation" {
  count      = var.enable_aurora ? 1 : 0
  name       = "${local.name_prefix}-analytics-aggregation"
  group_name = "default"

  flexible_time_window {
    mode = "OFF"
  }

  schedule_expression = "rate(1 hour)"

  target {
    arn      = "arn:aws:lambda:${var.aws_region}:${data.aws_caller_identity.current.account_id}:function:${local.name_prefix}-analytics-aggregation"
    role_arn = aws_iam_role.scheduler_execution.arn

    dead_letter_config {
      arn = aws_sqs_queue.dlq_analytics_aggregation.arn
    }

    retry_policy {
      maximum_event_age_in_seconds = 3600
      maximum_retry_attempts       = 2
    }
  }
}

# Scheduler: Notification digest — daily at 9 AM AEST (23:00 UTC previous day)
resource "aws_scheduler_schedule" "notification_digest" {
  name       = "${local.name_prefix}-notification-digest"
  group_name = "default"

  flexible_time_window {
    mode = "OFF"
  }

  schedule_expression          = "cron(0 23 * * ? *)"
  schedule_expression_timezone = "UTC"

  target {
    arn      = "arn:aws:lambda:${var.aws_region}:${data.aws_caller_identity.current.account_id}:function:${local.name_prefix}-notification-digest"
    role_arn = aws_iam_role.scheduler_execution.arn

    dead_letter_config {
      arn = aws_sqs_queue.dlq_notification_digest.arn
    }

    retry_policy {
      maximum_event_age_in_seconds = 86400
      maximum_retry_attempts       = 2
    }
  }
}

# Scheduler: Availability reminder — daily at midnight UTC
resource "aws_scheduler_schedule" "availability_reminder" {
  name       = "${local.name_prefix}-availability-reminder"
  group_name = "default"

  flexible_time_window {
    mode = "OFF"
  }

  schedule_expression          = "cron(0 0 * * ? *)"
  schedule_expression_timezone = "UTC"

  target {
    arn      = "arn:aws:lambda:${var.aws_region}:${data.aws_caller_identity.current.account_id}:function:${local.name_prefix}-availability-reminder"
    role_arn = aws_iam_role.scheduler_execution.arn

    dead_letter_config {
      arn = aws_sqs_queue.dlq_availability_reminder.arn
    }

    retry_policy {
      maximum_event_age_in_seconds = 86400
      maximum_retry_attempts       = 2
    }
  }
}
