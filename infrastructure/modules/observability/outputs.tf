output "alarm_sns_topic_arn" {
  description = "SNS topic ARN for CloudWatch alarm notifications"
  value       = aws_sns_topic.alarms.arn
}

output "alarm_sns_topic_name" {
  description = "SNS topic name for CloudWatch alarm notifications"
  value       = aws_sns_topic.alarms.name
}

output "dashboard_name" {
  description = "CloudWatch Ops dashboard name"
  value       = aws_cloudwatch_dashboard.ops.dashboard_name
}

output "dashboard_arn" {
  description = "CloudWatch Ops dashboard ARN"
  value       = aws_cloudwatch_dashboard.ops.dashboard_arn
}

output "budget_name" {
  description = "AWS Budget name for monthly cost tracking"
  value       = aws_budgets_budget.monthly.name
}

output "high_error_rate_alarm_arn" {
  description = "ARN of the high error rate CloudWatch alarm (empty if alarms disabled)"
  value       = var.enable_alarms ? aws_cloudwatch_metric_alarm.high_error_rate[0].arn : ""
}

output "high_latency_alarm_arn" {
  description = "ARN of the high latency CloudWatch alarm (empty if alarms disabled)"
  value       = var.enable_alarms ? aws_cloudwatch_metric_alarm.high_latency[0].arn : ""
}

output "bedrock_failures_alarm_arn" {
  description = "ARN of the Bedrock failures CloudWatch alarm (empty if alarms disabled)"
  value       = var.enable_alarms ? aws_cloudwatch_metric_alarm.bedrock_failures[0].arn : ""
}

output "ddb_throttled_alarm_arn" {
  description = "ARN of the DynamoDB throttled requests CloudWatch alarm (empty if alarms disabled)"
  value       = var.enable_alarms ? aws_cloudwatch_metric_alarm.ddb_throttled[0].arn : ""
}

output "ddb_read_capacity_alarm_arns" {
  description = "ARNs of DynamoDB read capacity 70% CloudWatch alarms (Requirement 26.3)"
  value       = var.enable_alarms ? aws_cloudwatch_metric_alarm.ddb_read_capacity_high[*].arn : []
}

output "ddb_write_capacity_alarm_arns" {
  description = "ARNs of DynamoDB write capacity 70% CloudWatch alarms (Requirement 26.3)"
  value       = var.enable_alarms ? aws_cloudwatch_metric_alarm.ddb_write_capacity_high[*].arn : []
}
