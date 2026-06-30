# Observability Module
# Manages: CloudWatch dashboards, alarms, SNS notifications, AWS Budgets
# Requirements: 22.4, 22.5, 22.7, 22.8

locals {
  service_name = "Observability"
  name_prefix  = "${var.environment}-guidedmentor"

  common_tags = {
    Environment    = var.environment
    Service        = local.service_name
    BoundedContext = "Platform"
  }
}

# =============================================================================
# SNS Topic for Alarm Notifications (Requirement 22.5)
# =============================================================================

resource "aws_sns_topic" "alarms" {
  name = "${local.name_prefix}-alarms"

  tags = merge(local.common_tags, {
    Name = "${local.name_prefix}-alarms"
  })
}

resource "aws_sns_topic_subscription" "alarm_email" {
  count = var.alarm_email != "" ? 1 : 0

  topic_arn = aws_sns_topic.alarms.arn
  protocol  = "email"
  endpoint  = var.alarm_email
}

# =============================================================================
# CloudWatch Alarms (Requirement 22.4) — Conditional on var.enable_alarms
# =============================================================================

# High Error Rate: API Gateway 5xx errors exceed 1% over 5 minutes
resource "aws_cloudwatch_metric_alarm" "high_error_rate" {
  count = var.enable_alarms ? 1 : 0

  alarm_name          = "${local.name_prefix}-high-error-rate"
  alarm_description   = "API Gateway 5xx error rate exceeds 1% over 5 minutes"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 2
  threshold           = 1
  treat_missing_data  = "notBreaching"

  metric_query {
    id          = "error_rate"
    expression  = "(errors / requests) * 100"
    label       = "Error Rate (%)"
    return_data = true
  }

  metric_query {
    id = "errors"

    metric {
      metric_name = "5XXError"
      namespace   = "AWS/ApiGateway"
      period      = 300
      stat        = "Sum"

      dimensions = {
        ApiName = "${local.name_prefix}-api"
      }
    }
  }

  metric_query {
    id = "requests"

    metric {
      metric_name = "Count"
      namespace   = "AWS/ApiGateway"
      period      = 300
      stat        = "Sum"

      dimensions = {
        ApiName = "${local.name_prefix}-api"
      }
    }
  }

  alarm_actions = [aws_sns_topic.alarms.arn]
  ok_actions    = [aws_sns_topic.alarms.arn]

  tags = merge(local.common_tags, {
    Name = "${local.name_prefix}-high-error-rate"
  })
}

# High Latency: API Gateway p99 latency exceeds 5000ms
resource "aws_cloudwatch_metric_alarm" "high_latency" {
  count = var.enable_alarms ? 1 : 0

  alarm_name          = "${local.name_prefix}-high-latency-p99"
  alarm_description   = "API Gateway p99 latency exceeds 5 seconds"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 1
  metric_name         = "Latency"
  namespace           = "AWS/ApiGateway"
  period              = 300
  extended_statistic  = "p99"
  threshold           = 5000
  treat_missing_data  = "notBreaching"

  dimensions = {
    ApiName = "${local.name_prefix}-api"
  }

  alarm_actions = [aws_sns_topic.alarms.arn]
  ok_actions    = [aws_sns_topic.alarms.arn]

  tags = merge(local.common_tags, {
    Name = "${local.name_prefix}-high-latency-p99"
  })
}

# Bedrock Failures: Custom metric exceeds 3 failures in 10 minutes
resource "aws_cloudwatch_metric_alarm" "bedrock_failures" {
  count = var.enable_alarms ? 1 : 0

  alarm_name          = "${local.name_prefix}-bedrock-failures"
  alarm_description   = "Bedrock API failures exceed 3 in 10 minutes"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 1
  metric_name         = "BedrockFailures"
  namespace           = "GuidedMentor/Content"
  period              = 600
  statistic           = "Sum"
  threshold           = 3
  treat_missing_data  = "notBreaching"

  alarm_actions = [aws_sns_topic.alarms.arn]
  ok_actions    = [aws_sns_topic.alarms.arn]

  tags = merge(local.common_tags, {
    Name = "${local.name_prefix}-bedrock-failures"
  })
}

# DynamoDB Throttled Requests: Any throttling detected
resource "aws_cloudwatch_metric_alarm" "ddb_throttled" {
  count = var.enable_alarms ? 1 : 0

  alarm_name          = "${local.name_prefix}-ddb-throttled"
  alarm_description   = "DynamoDB throttled requests detected"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 1
  metric_name         = "ThrottledRequests"
  namespace           = "AWS/DynamoDB"
  period              = 60
  statistic           = "Sum"
  threshold           = 0
  treat_missing_data  = "notBreaching"

  alarm_actions = [aws_sns_topic.alarms.arn]
  ok_actions    = [aws_sns_topic.alarms.arn]

  tags = merge(local.common_tags, {
    Name = "${local.name_prefix}-ddb-throttled"
  })
}

# =============================================================================
# DynamoDB Auto-Scaling Capacity Alarms (Requirement 26.3)
# Triggers at 70% consumed capacity to proactively alert operators
# before throttling occurs. Monitors both RCU and WCU for each table.
# =============================================================================

# Read Capacity Alarm — triggers at 70% consumed read capacity
resource "aws_cloudwatch_metric_alarm" "ddb_read_capacity_high" {
  count = var.enable_alarms ? length(var.dynamodb_table_names) : 0

  alarm_name          = "${local.name_prefix}-ddb-${var.dynamodb_table_names[count.index]}-read-capacity-70pct"
  alarm_description   = "DynamoDB table ${var.dynamodb_table_names[count.index]} consumed read capacity exceeds 70% of provisioned capacity"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 3
  threshold           = 70
  treat_missing_data  = "notBreaching"

  metric_query {
    id          = "utilization"
    expression  = "(consumed / provisioned) * 100"
    label       = "Read Capacity Utilization (%)"
    return_data = true
  }

  metric_query {
    id = "consumed"

    metric {
      metric_name = "ConsumedReadCapacityUnits"
      namespace   = "AWS/DynamoDB"
      period      = 300
      stat        = "Sum"

      dimensions = {
        TableName = var.dynamodb_table_names[count.index]
      }
    }
  }

  metric_query {
    id = "provisioned"

    metric {
      metric_name = "ProvisionedReadCapacityUnits"
      namespace   = "AWS/DynamoDB"
      period      = 300
      stat        = "Average"

      dimensions = {
        TableName = var.dynamodb_table_names[count.index]
      }
    }
  }

  alarm_actions = [aws_sns_topic.alarms.arn]
  ok_actions    = [aws_sns_topic.alarms.arn]

  tags = merge(local.common_tags, {
    Name = "${local.name_prefix}-ddb-${var.dynamodb_table_names[count.index]}-read-capacity-70pct"
  })
}

# Write Capacity Alarm — triggers at 70% consumed write capacity
resource "aws_cloudwatch_metric_alarm" "ddb_write_capacity_high" {
  count = var.enable_alarms ? length(var.dynamodb_table_names) : 0

  alarm_name          = "${local.name_prefix}-ddb-${var.dynamodb_table_names[count.index]}-write-capacity-70pct"
  alarm_description   = "DynamoDB table ${var.dynamodb_table_names[count.index]} consumed write capacity exceeds 70% of provisioned capacity"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 3
  threshold           = 70
  treat_missing_data  = "notBreaching"

  metric_query {
    id          = "utilization"
    expression  = "(consumed / provisioned) * 100"
    label       = "Write Capacity Utilization (%)"
    return_data = true
  }

  metric_query {
    id = "consumed"

    metric {
      metric_name = "ConsumedWriteCapacityUnits"
      namespace   = "AWS/DynamoDB"
      period      = 300
      stat        = "Sum"

      dimensions = {
        TableName = var.dynamodb_table_names[count.index]
      }
    }
  }

  metric_query {
    id = "provisioned"

    metric {
      metric_name = "ProvisionedWriteCapacityUnits"
      namespace   = "AWS/DynamoDB"
      period      = 300
      stat        = "Average"

      dimensions = {
        TableName = var.dynamodb_table_names[count.index]
      }
    }
  }

  alarm_actions = [aws_sns_topic.alarms.arn]
  ok_actions    = [aws_sns_topic.alarms.arn]

  tags = merge(local.common_tags, {
    Name = "${local.name_prefix}-ddb-${var.dynamodb_table_names[count.index]}-write-capacity-70pct"
  })
}

# =============================================================================
# AWS Budget Alerts (Requirement 22.8) — Always enabled
# =============================================================================

resource "aws_budgets_budget" "monthly" {
  name         = "${local.name_prefix}-monthly-budget"
  budget_type  = "COST"
  limit_amount = var.budget_amount
  limit_unit   = "USD"
  time_unit    = "MONTHLY"

  cost_filter {
    name   = "TagKeyValue"
    values = ["user:Environment$${var.environment}"]
  }

  # Alert at 50% threshold
  notification {
    comparison_operator        = "GREATER_THAN"
    threshold                  = 50
    threshold_type             = "PERCENTAGE"
    notification_type          = "ACTUAL"
    subscriber_sns_topic_arns  = [aws_sns_topic.alarms.arn]
  }

  # Alert at 80% threshold
  notification {
    comparison_operator        = "GREATER_THAN"
    threshold                  = 80
    threshold_type             = "PERCENTAGE"
    notification_type          = "ACTUAL"
    subscriber_sns_topic_arns  = [aws_sns_topic.alarms.arn]
  }

  # Alert at 100% threshold
  notification {
    comparison_operator        = "GREATER_THAN"
    threshold                  = 100
    threshold_type             = "PERCENTAGE"
    notification_type          = "ACTUAL"
    subscriber_sns_topic_arns  = [aws_sns_topic.alarms.arn]
  }

  tags = merge(local.common_tags, {
    Name = "${local.name_prefix}-monthly-budget"
  })
}


# =============================================================================
# CloudWatch Dashboard — Ops (Requirement 22.4)
# Widgets: API Gateway latency (p50, p95, p99), error count by endpoint,
#          Lambda invocations, DynamoDB consumed capacity
# =============================================================================

resource "aws_cloudwatch_dashboard" "ops" {
  dashboard_name = "${local.name_prefix}-ops"

  dashboard_body = jsonencode({
    widgets = [
      {
        type   = "metric"
        x      = 0
        y      = 0
        width  = 12
        height = 6
        properties = {
          title  = "API Gateway Latency (p50, p95, p99)"
          region = var.aws_region
          metrics = [
            ["AWS/ApiGateway", "Latency", "ApiName", "${local.name_prefix}-api", { stat = "p50", label = "p50" }],
            ["AWS/ApiGateway", "Latency", "ApiName", "${local.name_prefix}-api", { stat = "p95", label = "p95" }],
            ["AWS/ApiGateway", "Latency", "ApiName", "${local.name_prefix}-api", { stat = "p99", label = "p99" }]
          ]
          period = 300
          view   = "timeSeries"
          yAxis = {
            left = {
              label     = "Milliseconds"
              showUnits = false
            }
          }
        }
      },
      {
        type   = "metric"
        x      = 12
        y      = 0
        width  = 12
        height = 6
        properties = {
          title  = "API Gateway Errors (4xx, 5xx)"
          region = var.aws_region
          metrics = [
            ["AWS/ApiGateway", "4XXError", "ApiName", "${local.name_prefix}-api", { stat = "Sum", label = "4xx Errors" }],
            ["AWS/ApiGateway", "5XXError", "ApiName", "${local.name_prefix}-api", { stat = "Sum", label = "5xx Errors" }]
          ]
          period = 300
          view   = "timeSeries"
          yAxis = {
            left = {
              label     = "Count"
              showUnits = false
            }
          }
        }
      },
      {
        type   = "metric"
        x      = 0
        y      = 6
        width  = 12
        height = 6
        properties = {
          title  = "Lambda Invocations by Context"
          region = var.aws_region
          metrics = [
            ["AWS/Lambda", "Invocations", "FunctionName", "${local.name_prefix}-identity", { stat = "Sum", label = "Identity" }],
            ["AWS/Lambda", "Invocations", "FunctionName", "${local.name_prefix}-mentoring", { stat = "Sum", label = "Mentoring" }],
            ["AWS/Lambda", "Invocations", "FunctionName", "${local.name_prefix}-content", { stat = "Sum", label = "Content" }],
            ["AWS/Lambda", "Invocations", "FunctionName", "${local.name_prefix}-engagement", { stat = "Sum", label = "Engagement" }]
          ]
          period = 300
          view   = "timeSeries"
          yAxis = {
            left = {
              label     = "Count"
              showUnits = false
            }
          }
        }
      },
      {
        type   = "metric"
        x      = 12
        y      = 6
        width  = 12
        height = 6
        properties = {
          title  = "DynamoDB Consumed Capacity"
          region = var.aws_region
          metrics = [
            ["AWS/DynamoDB", "ConsumedReadCapacityUnits", "TableName", "${local.name_prefix}-users", { stat = "Sum", label = "Users RCU" }],
            ["AWS/DynamoDB", "ConsumedWriteCapacityUnits", "TableName", "${local.name_prefix}-users", { stat = "Sum", label = "Users WCU" }],
            ["AWS/DynamoDB", "ConsumedReadCapacityUnits", "TableName", "${local.name_prefix}-sessions", { stat = "Sum", label = "Sessions RCU" }],
            ["AWS/DynamoDB", "ConsumedWriteCapacityUnits", "TableName", "${local.name_prefix}-sessions", { stat = "Sum", label = "Sessions WCU" }]
          ]
          period = 300
          view   = "timeSeries"
          yAxis = {
            left = {
              label     = "Capacity Units"
              showUnits = false
            }
          }
        }
      }
    ]
  })
}

# =============================================================================
# DLQ Monitoring Alarms (HA/DR gap fix #2)
# Triggers when any DLQ receives messages — indicates event processing failure
# =============================================================================

resource "aws_cloudwatch_metric_alarm" "dlq_messages" {
  count = var.enable_alarms ? length(var.dlq_names) : 0

  alarm_name          = "${local.name_prefix}-dlq-${var.dlq_names[count.index]}-has-messages"
  alarm_description   = "DLQ ${var.dlq_names[count.index]} has unprocessed messages — indicates event processing failure"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 1
  metric_name         = "ApproximateNumberOfMessagesVisible"
  namespace           = "AWS/SQS"
  period              = 300
  statistic           = "Sum"
  threshold           = 0
  treat_missing_data  = "notBreaching"

  dimensions = {
    QueueName = var.dlq_names[count.index]
  }

  alarm_actions = [aws_sns_topic.alarms.arn]
  ok_actions    = [aws_sns_topic.alarms.arn]

  tags = merge(local.common_tags, {
    Name = "${local.name_prefix}-dlq-${var.dlq_names[count.index]}-has-messages"
  })
}
