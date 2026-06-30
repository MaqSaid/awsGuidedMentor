# ==============================================================================
# Networking Module
# Manages: API Gateway REST API, CloudFront, S3 (SPA + Resumes), WAF association
# ==============================================================================

locals {
  spa_bucket_name    = "${var.environment}-guidedmentor-spa"
  resume_bucket_name = "${var.environment}-guidedmentor-resumes"
  api_name           = "${var.environment}-guidedmentor-api"

  allowed_origins = [
    "https://guidedmentor.dev",
    "http://localhost:3000"
  ]
}

# ==============================================================================
# API Gateway REST API
# ==============================================================================

resource "aws_api_gateway_rest_api" "main" {
  name        = local.api_name
  description = "GuidedMentor Platform API"

  endpoint_configuration {
    types = ["REGIONAL"]
  }

  tags = merge(var.tags, {
    Name = local.api_name
  })
}

# /v1 resource (base path for all endpoints)
resource "aws_api_gateway_resource" "v1" {
  rest_api_id = aws_api_gateway_rest_api.main.id
  parent_id   = aws_api_gateway_rest_api.main.root_resource_id
  path_part   = "v1"
}

# Cognito authorizer for JWT validation
resource "aws_api_gateway_authorizer" "cognito" {
  name            = "${var.environment}-cognito-authorizer"
  rest_api_id     = aws_api_gateway_rest_api.main.id
  type            = "COGNITO_USER_POOLS"
  identity_source = "method.request.header.Authorization"

  provider_arns = [var.cognito_user_pool_arn]
}

# API Gateway deployment
resource "aws_api_gateway_deployment" "main" {
  rest_api_id = aws_api_gateway_rest_api.main.id

  # Redeploy when resources change
  triggers = {
    redeployment = sha1(jsonencode([
      aws_api_gateway_resource.v1.id,
    ]))
  }

  lifecycle {
    create_before_destroy = true
  }
}

# API Gateway stage
resource "aws_api_gateway_stage" "main" {
  rest_api_id   = aws_api_gateway_rest_api.main.id
  deployment_id = aws_api_gateway_deployment.main.id
  stage_name    = var.environment

  access_log_settings {
    destination_arn = aws_cloudwatch_log_group.api_gateway.arn
    format = jsonencode({
      requestId      = "$context.requestId"
      ip             = "$context.identity.sourceIp"
      caller         = "$context.identity.caller"
      user           = "$context.identity.user"
      requestTime    = "$context.requestTime"
      httpMethod     = "$context.httpMethod"
      resourcePath   = "$context.resourcePath"
      status         = "$context.status"
      protocol       = "$context.protocol"
      responseLength = "$context.responseLength"
    })
  }

  tags = merge(var.tags, {
    Name = "${var.environment}-guidedmentor-api-stage"
  })
}

# CloudWatch log group for API Gateway execution logs
resource "aws_cloudwatch_log_group" "api_gateway" {
  name              = "/aws/apigateway/${local.api_name}"
  retention_in_days = var.environment == "prod" ? 90 : 14

  tags = merge(var.tags, {
    Name = "${local.api_name}-logs"
  })
}

# API Gateway method settings (logging + throttling)
resource "aws_api_gateway_method_settings" "all" {
  rest_api_id = aws_api_gateway_rest_api.main.id
  stage_name  = aws_api_gateway_stage.main.stage_name
  method_path = "*/*"

  settings {
    logging_level      = "INFO"
    data_trace_enabled = var.environment != "prod"
    metrics_enabled    = true

    throttling_rate_limit  = 100
    throttling_burst_limit = 100
  }
}

# Usage plan: 100 requests/second throttle, 100 burst
resource "aws_api_gateway_usage_plan" "main" {
  name        = "${var.environment}-guidedmentor-usage-plan"
  description = "GuidedMentor API usage plan - 100 req/s throttle"

  api_stages {
    api_id = aws_api_gateway_rest_api.main.id
    stage  = aws_api_gateway_stage.main.stage_name
  }

  throttle_settings {
    rate_limit  = 100
    burst_limit = 100
  }

  tags = merge(var.tags, {
    Name = "${var.environment}-guidedmentor-usage-plan"
  })
}

# CORS configuration for API Gateway (gateway responses)
resource "aws_api_gateway_gateway_response" "cors_4xx" {
  rest_api_id   = aws_api_gateway_rest_api.main.id
  response_type = "DEFAULT_4XX"

  response_parameters = {
    "gatewayresponse.header.Access-Control-Allow-Origin"  = "'*'"
    "gatewayresponse.header.Access-Control-Allow-Headers" = "'Content-Type,Authorization,X-Correlation-Id'"
    "gatewayresponse.header.Access-Control-Allow-Methods" = "'GET,POST,PUT,DELETE,OPTIONS'"
  }
}

resource "aws_api_gateway_gateway_response" "cors_5xx" {
  rest_api_id   = aws_api_gateway_rest_api.main.id
  response_type = "DEFAULT_5XX"

  response_parameters = {
    "gatewayresponse.header.Access-Control-Allow-Origin"  = "'*'"
    "gatewayresponse.header.Access-Control-Allow-Headers" = "'Content-Type,Authorization,X-Correlation-Id'"
    "gatewayresponse.header.Access-Control-Allow-Methods" = "'GET,POST,PUT,DELETE,OPTIONS'"
  }
}

# ==============================================================================
# S3 Bucket — SPA Assets (CloudFront origin)
# ==============================================================================

resource "aws_s3_bucket" "spa" {
  bucket = local.spa_bucket_name

  tags = merge(var.tags, {
    Name    = local.spa_bucket_name
    Purpose = "SPA static assets hosting"
  })
}

resource "aws_s3_bucket_versioning" "spa" {
  bucket = aws_s3_bucket.spa.id

  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "spa" {
  bucket = aws_s3_bucket.spa.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
    bucket_key_enabled = true
  }
}

resource "aws_s3_bucket_public_access_block" "spa" {
  bucket = aws_s3_bucket.spa.id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_cors_configuration" "spa" {
  bucket = aws_s3_bucket.spa.id

  cors_rule {
    allowed_headers = ["*"]
    allowed_methods = ["GET"]
    allowed_origins = local.allowed_origins
    expose_headers  = ["ETag"]
    max_age_seconds = 3600
  }
}

# ==============================================================================
# S3 Bucket — Resume Storage
# ==============================================================================

resource "aws_s3_bucket" "resumes" {
  bucket = local.resume_bucket_name

  tags = merge(var.tags, {
    Name    = local.resume_bucket_name
    Purpose = "Resume file storage"
  })
}

resource "aws_s3_bucket_versioning" "resumes" {
  bucket = aws_s3_bucket.resumes.id

  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "resumes" {
  bucket = aws_s3_bucket.resumes.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = var.enable_cmk ? "aws:kms" : "AES256"
    }
    bucket_key_enabled = true
  }
}

resource "aws_s3_bucket_public_access_block" "resumes" {
  bucket = aws_s3_bucket.resumes.id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_lifecycle_configuration" "resumes" {
  bucket = aws_s3_bucket.resumes.id

  rule {
    id     = "archive-noncurrent-versions"
    status = "Enabled"

    noncurrent_version_transition {
      noncurrent_days = 30
      storage_class   = "GLACIER"
    }
  }
}

# ==============================================================================
# CloudFront Distribution — SPA Hosting
# ==============================================================================

# Origin Access Identity for S3
resource "aws_cloudfront_origin_access_identity" "spa" {
  comment = "OAI for ${local.spa_bucket_name}"
}

# S3 bucket policy allowing CloudFront OAI access
resource "aws_s3_bucket_policy" "spa" {
  bucket = aws_s3_bucket.spa.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid       = "AllowCloudFrontOAI"
        Effect    = "Allow"
        Principal = {
          AWS = aws_cloudfront_origin_access_identity.spa.iam_arn
        }
        Action   = "s3:GetObject"
        Resource = "${aws_s3_bucket.spa.arn}/*"
      }
    ]
  })
}

# Response headers policy: CSP, HSTS, X-Frame-Options, X-Content-Type-Options
resource "aws_cloudfront_response_headers_policy" "security_headers" {
  name    = "${var.environment}-guidedmentor-security-headers"
  comment = "Security headers for GuidedMentor SPA"

  security_headers_config {
    content_security_policy {
      content_security_policy = "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self' https:; connect-src 'self' https://*.amazonaws.com https://*.amazoncognito.com wss://*.appsync-api.ap-southeast-2.amazonaws.com; frame-ancestors 'none'"
      override = true
    }

    content_type_options {
      override = true
    }

    frame_options {
      frame_option = "DENY"
      override     = true
    }

    strict_transport_security {
      access_control_max_age_sec = 31536000
      include_subdomains         = true
      preload                    = true
      override                   = true
    }
  }
}

# CloudFront distribution
resource "aws_cloudfront_distribution" "spa" {
  enabled             = true
  is_ipv6_enabled     = true
  default_root_object = "index.html"
  comment             = "${var.environment} GuidedMentor SPA Distribution"
  price_class         = "PriceClass_100"
  wait_for_deployment = false

  origin {
    domain_name = aws_s3_bucket.spa.bucket_regional_domain_name
    origin_id   = "S3-${local.spa_bucket_name}"

    s3_origin_config {
      origin_access_identity = aws_cloudfront_origin_access_identity.spa.cloudfront_access_identity_path
    }
  }

  default_cache_behavior {
    allowed_methods  = ["GET", "HEAD", "OPTIONS"]
    cached_methods   = ["GET", "HEAD"]
    target_origin_id = "S3-${local.spa_bucket_name}"

    forwarded_values {
      query_string = false

      cookies {
        forward = "none"
      }
    }

    viewer_protocol_policy     = "redirect-to-https"
    min_ttl                    = 0
    default_ttl                = 86400
    max_ttl                    = 31536000
    compress                   = true
    response_headers_policy_id = aws_cloudfront_response_headers_policy.security_headers.id
  }

  # SPA routing: 403 (access denied for missing paths) → index.html
  custom_error_response {
    error_code            = 403
    response_code         = 200
    response_page_path    = "/index.html"
    error_caching_min_ttl = 10
  }

  # SPA routing: 404 → index.html
  custom_error_response {
    error_code            = 404
    response_code         = 200
    response_page_path    = "/index.html"
    error_caching_min_ttl = 10
  }

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  viewer_certificate {
    cloudfront_default_certificate = true
  }

  # WAF association (conditional)
  web_acl_id = var.enable_waf && var.waf_web_acl_arn != "" ? var.waf_web_acl_arn : null

  tags = merge(var.tags, {
    Name = "${var.environment}-guidedmentor-spa-cdn"
  })
}

# ==============================================================================
# WAF Association with API Gateway (conditional)
# ==============================================================================

resource "aws_wafv2_web_acl_association" "api_gateway" {
  count = var.enable_waf && var.waf_web_acl_arn != "" ? 1 : 0

  resource_arn = aws_api_gateway_stage.main.arn
  web_acl_arn  = var.waf_web_acl_arn
}
