# =============================================================================
# Lambda Function URLs for Direct Health Check Access. FREE.
# Bypasses API Gateway for faster health probes (direct HTTPS endpoint).
# Useful for external monitoring services (UptimeRobot, Pingdom).
#
# NOTE: Function URLs require the Lambda functions to exist first.
# These resources should be applied after initial Lambda deployment.
# Uncomment after first `terraform apply` + Lambda deploy.
# =============================================================================

# resource "aws_lambda_function_url" "identity_health" {
#   function_name      = "${var.environment}-guidedmentor-identity"
#   authorization_type = "NONE"  # Health check doesn't need auth
#
#   cors {
#     allow_origins = ["*"]
#     allow_methods = ["GET"]
#   }
# }
#
# resource "aws_lambda_function_url" "mentoring_health" {
#   function_name      = "${var.environment}-guidedmentor-mentoring"
#   authorization_type = "NONE"
#
#   cors {
#     allow_origins = ["*"]
#     allow_methods = ["GET"]
#   }
# }
#
# resource "aws_lambda_function_url" "content_health" {
#   function_name      = "${var.environment}-guidedmentor-content"
#   authorization_type = "NONE"
#
#   cors {
#     allow_origins = ["*"]
#     allow_methods = ["GET"]
#   }
# }
#
# resource "aws_lambda_function_url" "engagement_health" {
#   function_name      = "${var.environment}-guidedmentor-engagement"
#   authorization_type = "NONE"
#
#   cors {
#     allow_origins = ["*"]
#     allow_methods = ["GET"]
#   }
# }
#
# output "health_check_urls" {
#   description = "Direct Lambda Function URLs for health checks (bypasses API Gateway)"
#   value = {
#     identity   = aws_lambda_function_url.identity_health.function_url
#     mentoring  = aws_lambda_function_url.mentoring_health.function_url
#     content    = aws_lambda_function_url.content_health.function_url
#     engagement = aws_lambda_function_url.engagement_health.function_url
#   }
# }
