variable "environment" {
  description = "Deployment environment (dev, staging, prod)"
  type        = string
}

variable "aws_region" {
  description = "AWS region for resource deployment"
  type        = string
}

variable "enable_alarms" {
  description = "Enable CloudWatch alarms and alerting (disabled in dev)"
  type        = bool
  default     = false
}

variable "budget_amount" {
  description = "Monthly AWS budget limit in USD"
  type        = string
  default     = "50"
}

variable "alarm_email" {
  description = "Email address for alarm and budget notifications (empty string disables email subscription)"
  type        = string
  default     = ""
}

variable "dynamodb_table_names" {
  description = "List of DynamoDB table names to monitor for capacity alarms (Requirement 26.3)"
  type        = list(string)
  default     = []
}

variable "dlq_names" {
  description = "SQS DLQ queue names to monitor for unprocessed messages (HA/DR gap fix #2)"
  type        = list(string)
  default     = []
}
