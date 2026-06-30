# =============================================================================
# AWS Cost Anomaly Detection (FREE)
# Alerts when spending patterns deviate from historical norms.
# Catches: unexpected Lambda invocations, DynamoDB spikes, Bedrock token overuse.
# =============================================================================

resource "aws_ce_anomaly_monitor" "guidedmentor" {
  name              = "${var.environment}-guidedmentor-cost-monitor"
  monitor_type      = "DIMENSIONAL"
  monitor_dimension = "SERVICE"

  tags = {
    Environment = var.environment
    Service     = "guidedmentor"
    Purpose     = "cost-governance"
  }
}

resource "aws_ce_anomaly_subscription" "guidedmentor_alerts" {
  name      = "${var.environment}-guidedmentor-cost-alerts"
  frequency = "DAILY"

  monitor_arn_list = [aws_ce_anomaly_monitor.guidedmentor.arn]

  subscriber {
    type    = "SNS"
    address = aws_sns_topic.alarms.arn
  }

  # Alert threshold — notify when anomaly impact exceeds $10
  # Low threshold for dev/demo; increase for production
  threshold_expression {
    dimension {
      key           = "ANOMALY_TOTAL_IMPACT_ABSOLUTE"
      values        = ["10"]
      match_options = ["GREATER_THAN_OR_EQUAL"]
    }
  }

  tags = {
    Environment = var.environment
    Service     = "guidedmentor"
    Purpose     = "cost-governance"
  }
}
