# =============================================================================
# Enhanced DynamoDB Capacity Alerts — 50% (heads-up) and 90% (urgent)
# Supplements existing 70% alarm with earlier and later thresholds. FREE.
# =============================================================================

# Read Capacity Alarm — 50% early warning
resource "aws_cloudwatch_metric_alarm" "ddb_capacity_50pct" {
  count = var.enable_alarms ? length(var.dynamodb_table_names) : 0

  alarm_name          = "${local.name_prefix}-ddb-${var.dynamodb_table_names[count.index]}-capacity-50pct"
  alarm_description   = "DynamoDB table ${var.dynamodb_table_names[count.index]} at 50% capacity — early warning"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 3
  threshold           = 50
  treat_missing_data  = "notBreaching"

  metric_query {
    id          = "utilization"
    expression  = "(consumed / provisioned) * 100"
    label       = "Capacity Utilization (%)"
    return_data = true
  }

  metric_query {
    id = "consumed"
    metric {
      metric_name = "ConsumedReadCapacityUnits"
      namespace   = "AWS/DynamoDB"
      period      = 300
      stat        = "Sum"
      dimensions  = { TableName = var.dynamodb_table_names[count.index] }
    }
  }

  metric_query {
    id = "provisioned"
    metric {
      metric_name = "ProvisionedReadCapacityUnits"
      namespace   = "AWS/DynamoDB"
      period      = 300
      stat        = "Average"
      dimensions  = { TableName = var.dynamodb_table_names[count.index] }
    }
  }

  alarm_actions = [aws_sns_topic.alarms.arn]

  tags = merge(local.common_tags, {
    Name     = "${local.name_prefix}-ddb-${var.dynamodb_table_names[count.index]}-capacity-50pct"
    Severity = "warning"
  })
}

# Read Capacity Alarm — 90% urgent
resource "aws_cloudwatch_metric_alarm" "ddb_capacity_90pct" {
  count = var.enable_alarms ? length(var.dynamodb_table_names) : 0

  alarm_name          = "${local.name_prefix}-ddb-${var.dynamodb_table_names[count.index]}-capacity-90pct"
  alarm_description   = "DynamoDB table ${var.dynamodb_table_names[count.index]} at 90% capacity — URGENT"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 2
  threshold           = 90
  treat_missing_data  = "notBreaching"

  metric_query {
    id          = "utilization"
    expression  = "(consumed / provisioned) * 100"
    label       = "Capacity Utilization (%)"
    return_data = true
  }

  metric_query {
    id = "consumed"
    metric {
      metric_name = "ConsumedReadCapacityUnits"
      namespace   = "AWS/DynamoDB"
      period      = 300
      stat        = "Sum"
      dimensions  = { TableName = var.dynamodb_table_names[count.index] }
    }
  }

  metric_query {
    id = "provisioned"
    metric {
      metric_name = "ProvisionedReadCapacityUnits"
      namespace   = "AWS/DynamoDB"
      period      = 300
      stat        = "Average"
      dimensions  = { TableName = var.dynamodb_table_names[count.index] }
    }
  }

  alarm_actions = [aws_sns_topic.alarms.arn]

  tags = merge(local.common_tags, {
    Name     = "${local.name_prefix}-ddb-${var.dynamodb_table_names[count.index]}-capacity-90pct"
    Severity = "critical"
  })
}
