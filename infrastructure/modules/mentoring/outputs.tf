output "mentors_table_name" {
  description = "DynamoDB Mentors table name"
  value       = aws_dynamodb_table.mentors.name
}

output "mentors_table_arn" {
  description = "DynamoDB Mentors table ARN"
  value       = aws_dynamodb_table.mentors.arn
}

output "mentees_table_name" {
  description = "DynamoDB Mentees table name"
  value       = aws_dynamodb_table.mentees.name
}

output "mentees_table_arn" {
  description = "DynamoDB Mentees table ARN"
  value       = aws_dynamodb_table.mentees.arn
}

output "sessions_table_name" {
  description = "DynamoDB Sessions table name"
  value       = aws_dynamodb_table.sessions.name
}

output "sessions_table_arn" {
  description = "DynamoDB Sessions table ARN"
  value       = aws_dynamodb_table.sessions.arn
}

output "opportunities_table_name" {
  description = "DynamoDB Opportunities table name"
  value       = aws_dynamodb_table.opportunities.name
}

output "opportunities_table_arn" {
  description = "DynamoDB Opportunities table ARN"
  value       = aws_dynamodb_table.opportunities.arn
}

output "kms_key_arn" {
  description = "KMS key ARN for Mentoring context encryption (null if CMK disabled)"
  value       = var.enable_cmk ? aws_kms_key.mentoring[0].arn : null
}
