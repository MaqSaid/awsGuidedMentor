output "api_gateway_id" {
  description = "API Gateway REST API ID"
  value       = aws_api_gateway_rest_api.main.id
}

output "api_gateway_url" {
  description = "API Gateway invoke URL (stage URL)"
  value       = aws_api_gateway_stage.main.invoke_url
}

output "api_gateway_execution_arn" {
  description = "API Gateway execution ARN (for Lambda permissions)"
  value       = aws_api_gateway_rest_api.main.execution_arn
}

output "api_gateway_root_resource_id" {
  description = "API Gateway root resource ID"
  value       = aws_api_gateway_rest_api.main.root_resource_id
}

output "api_gateway_v1_resource_id" {
  description = "API Gateway /v1 resource ID (base path for all endpoints)"
  value       = aws_api_gateway_resource.v1.id
}

output "api_gateway_authorizer_id" {
  description = "API Gateway Cognito authorizer ID"
  value       = aws_api_gateway_authorizer.cognito.id
}

output "cloudfront_distribution_id" {
  description = "CloudFront distribution ID for SPA hosting"
  value       = aws_cloudfront_distribution.spa.id
}

output "cloudfront_domain" {
  description = "CloudFront distribution domain name"
  value       = aws_cloudfront_distribution.spa.domain_name
}

output "spa_bucket_name" {
  description = "S3 bucket name for SPA assets"
  value       = aws_s3_bucket.spa.id
}

output "spa_bucket_arn" {
  description = "S3 bucket ARN for SPA assets"
  value       = aws_s3_bucket.spa.arn
}

output "resume_bucket_name" {
  description = "S3 bucket name for resume storage"
  value       = aws_s3_bucket.resumes.id
}

output "resume_bucket_arn" {
  description = "S3 bucket ARN for resume storage"
  value       = aws_s3_bucket.resumes.arn
}

output "cloudfront_oai_iam_arn" {
  description = "CloudFront OAI IAM ARN (for S3 bucket policies)"
  value       = aws_cloudfront_origin_access_identity.spa.iam_arn
}
