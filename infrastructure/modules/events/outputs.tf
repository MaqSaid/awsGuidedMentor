output "event_bus_name" {
  description = "Name of the custom EventBridge event bus"
  value       = aws_cloudwatch_event_bus.main.name
}

output "event_bus_arn" {
  description = "ARN of the custom EventBridge event bus"
  value       = aws_cloudwatch_event_bus.main.arn
}

output "dlq_arns" {
  description = "ARNs of all SQS dead-letter queues for EventBridge rule targets"
  value = {
    lock_cleanup           = aws_sqs_queue.dlq_lock_cleanup.arn
    analytics_aggregation  = aws_sqs_queue.dlq_analytics_aggregation.arn
    notification_digest    = aws_sqs_queue.dlq_notification_digest.arn
    availability_reminder  = aws_sqs_queue.dlq_availability_reminder.arn
  }
}

output "scheduler_role_arn" {
  description = "ARN of the IAM role used by EventBridge Scheduler"
  value       = aws_iam_role.scheduler_execution.arn
}

output "rule_target_role_arn" {
  description = "ARN of the IAM role used by EventBridge rule targets"
  value       = aws_iam_role.eventbridge_rule_target.arn
}
