output "bedrock_guardrail_id" {
  description = "Bedrock Guardrail ID (empty if guardrails disabled)"
  value       = var.enable_bedrock_guardrails ? aws_bedrock_guardrail.content_guardrail[0].guardrail_id : ""
}

output "bedrock_guardrail_arn" {
  description = "Bedrock Guardrail ARN (empty if guardrails disabled)"
  value       = var.enable_bedrock_guardrails ? aws_bedrock_guardrail.content_guardrail[0].guardrail_arn : ""
}

output "bedrock_guardrail_version" {
  description = "Bedrock Guardrail version number (empty if guardrails disabled)"
  value       = var.enable_bedrock_guardrails ? aws_bedrock_guardrail_version.content_guardrail_version[0].version : ""
}

output "plan_generation_dlq_arn" {
  description = "SQS DLQ ARN for failed plan generation retries"
  value       = "" # Will be populated when SQS resource is created
}
