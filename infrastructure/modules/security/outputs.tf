output "kms_key_arn" {
  description = "ARN of the Customer Managed KMS key for customer-sensitive data encryption"
  value       = var.enable_cmk ? aws_kms_key.customer_data[0].arn : ""
}

output "kms_key_id" {
  description = "ID of the Customer Managed KMS key"
  value       = var.enable_cmk ? aws_kms_key.customer_data[0].key_id : ""
}

output "waf_web_acl_arn" {
  description = "ARN of the WAF Web ACL for API Gateway and CloudFront association"
  value       = var.enable_waf ? aws_wafv2_web_acl.main[0].arn : ""
}

output "waf_web_acl_id" {
  description = "ID of the WAF Web ACL"
  value       = var.enable_waf ? aws_wafv2_web_acl.main[0].id : ""
}

output "permission_boundary_arn" {
  description = "ARN of the Lambda permission boundary IAM policy"
  value       = aws_iam_policy.lambda_permission_boundary.arn
}

output "code_signing_config_arn" {
  description = "ARN of the Lambda code signing configuration (empty if disabled)"
  value       = var.enable_cmk ? aws_lambda_code_signing_config.main[0].arn : ""
}

output "signing_profile_version_arn" {
  description = "ARN of the Signer signing profile version (empty if disabled)"
  value       = var.enable_cmk ? aws_signer_signing_profile.lambda[0].version_arn : ""
}
