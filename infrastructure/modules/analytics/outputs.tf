output "aurora_cluster_endpoint" {
  description = "Aurora PostgreSQL cluster writer endpoint"
  value       = aws_rds_cluster.analytics.endpoint
}

output "aurora_reader_endpoint" {
  description = "Aurora PostgreSQL cluster reader endpoint"
  value       = aws_rds_cluster.analytics.reader_endpoint
}

output "aurora_cluster_id" {
  description = "Aurora PostgreSQL cluster identifier"
  value       = aws_rds_cluster.analytics.cluster_identifier
}

output "aurora_cluster_arn" {
  description = "Aurora PostgreSQL cluster ARN"
  value       = aws_rds_cluster.analytics.arn
}

output "aurora_cluster_resource_id" {
  description = "Aurora PostgreSQL cluster resource ID (for IAM auth)"
  value       = aws_rds_cluster.analytics.cluster_resource_id
}

output "aurora_database_name" {
  description = "Aurora PostgreSQL database name"
  value       = local.db_name
}

output "aurora_master_secret_arn" {
  description = "ARN of the Secrets Manager secret containing Aurora master credentials"
  value       = aws_secretsmanager_secret.aurora_master.arn
}

output "rds_proxy_endpoint" {
  description = "RDS Proxy endpoint (empty if proxy disabled)"
  value       = var.enable_rds_proxy ? aws_db_proxy.analytics[0].endpoint : ""
}

output "rds_proxy_arn" {
  description = "RDS Proxy ARN (empty if proxy disabled)"
  value       = var.enable_rds_proxy ? aws_db_proxy.analytics[0].arn : ""
}

output "vpc_id" {
  description = "VPC ID for the analytics network"
  value       = aws_vpc.analytics.id
}

output "private_subnet_ids" {
  description = "Private subnet IDs for Aurora and Lambda"
  value       = aws_subnet.private[*].id
}

output "aurora_security_group_id" {
  description = "Security group ID for Aurora cluster"
  value       = aws_security_group.aurora.id
}

output "lambda_security_group_id" {
  description = "Security group ID for Lambda functions accessing Aurora"
  value       = aws_security_group.lambda.id
}

output "replication_lambda_arn" {
  description = "ARN of the DynamoDB Streams to Aurora replication Lambda"
  value       = aws_lambda_function.ddb_to_aurora_replication.arn
}

output "replication_lambda_role_arn" {
  description = "IAM role ARN for the replication Lambda"
  value       = aws_iam_role.replication_lambda.arn
}

output "replication_dlq_arn" {
  description = "ARN of the replication dead-letter queue"
  value       = aws_sqs_queue.replication_dlq.arn
}
