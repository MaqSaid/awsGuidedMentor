# Identity Context Module
# Manages: Cognito User Pool, Users DynamoDB table
# Resources for auth, role management, and user identity

locals {
  service_name    = "Identity"
  table_prefix    = "${var.environment}-guidedmentor"
  enable_google   = var.google_client_id != ""
  deletion_protection = var.environment == "prod"

  # Identity providers list: always include COGNITO, conditionally add Google
  supported_identity_providers = local.enable_google ? ["COGNITO", "Google"] : ["COGNITO"]

  common_tags = {
    Environment    = var.environment
    Service        = local.service_name
    BoundedContext = "Identity"
  }
}

# =============================================================================
# DynamoDB Users Table
# =============================================================================

resource "aws_dynamodb_table" "users" {
  name         = "${local.table_prefix}-users"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "userId"

  attribute {
    name = "userId"
    type = "S"
  }

  attribute {
    name = "email"
    type = "S"
  }

  # GSI-Email: Login lookups by email address
  global_secondary_index {
    name            = "GSI-Email"
    hash_key        = "email"
    projection_type = "ALL"
  }

  point_in_time_recovery {
    enabled = true
  }

  # Conditional encryption: CMK for staging/prod, AWS-managed for dev
  dynamic "server_side_encryption" {
    for_each = var.enable_cmk ? [1] : []
    content {
      enabled     = true
      kms_key_arn = var.kms_key_arn
    }
  }

  lifecycle {
    prevent_destroy = true
  }

  tags = merge(local.common_tags, {
    Name               = "${local.table_prefix}-users"
    DataClassification = "confidential"
  })
}

# =============================================================================
# Cognito User Pool
# =============================================================================

resource "aws_cognito_user_pool" "main" {
  name = "${local.table_prefix}"

  # Password policy: minimum 12 chars, require uppercase, lowercase, numbers, symbols
  password_policy {
    minimum_length                   = 12
    require_uppercase                = true
    require_lowercase                = true
    require_numbers                  = true
    require_symbols                  = true
    temporary_password_validity_days = 7
  }

  # Account recovery: email only
  account_recovery_setting {
    recovery_mechanism {
      name     = "verified_email"
      priority = 1
    }
  }

  # Auto-verify email
  auto_verified_attributes = ["email"]

  # MFA configuration: optional with software token (TOTP)
  mfa_configuration = "OPTIONAL"

  software_token_mfa_configuration {
    enabled = true
  }

  # Deletion protection: enabled in prod only
  deletion_protection = local.deletion_protection ? "ACTIVE" : "INACTIVE"

  # Email configuration (Cognito default email sender)
  email_configuration {
    email_sending_account = "COGNITO_DEFAULT"
  }

  # Schema attributes
  schema {
    name                     = "email"
    attribute_data_type      = "String"
    required                 = true
    mutable                  = true

    string_attribute_constraints {
      min_length = 1
      max_length = 256
    }
  }

  schema {
    name                     = "activeRole"
    attribute_data_type      = "String"
    required                 = false
    mutable                  = true

    string_attribute_constraints {
      min_length = 0
      max_length = 10
    }
  }

  # Username configuration
  username_attributes = ["email"]

  username_configuration {
    case_sensitive = false
  }

  lifecycle {
    prevent_destroy = true
  }

  tags = merge(local.common_tags, {
    Name = "${local.table_prefix}"
  })
}

# =============================================================================
# Cognito App Client
# =============================================================================

resource "aws_cognito_user_pool_client" "web" {
  name         = "${local.table_prefix}-web"
  user_pool_id = aws_cognito_user_pool.main.id

  # SPA client — no secret
  generate_secret = false

  # Token validity
  access_token_validity  = 15   # 15 minutes
  id_token_validity      = 60   # 60 minutes
  refresh_token_validity = 7    # 7 days

  token_validity_units {
    access_token  = "minutes"
    id_token      = "minutes"
    refresh_token = "days"
  }

  # Supported identity providers
  supported_identity_providers = local.supported_identity_providers

  # OAuth configuration
  allowed_oauth_flows                  = ["code"]
  allowed_oauth_flows_user_pool_client = true
  allowed_oauth_scopes                 = ["openid", "email", "profile"]
  callback_urls                        = var.callback_urls
  logout_urls                          = var.logout_urls

  # Explicit auth flows for email/password
  explicit_auth_flows = [
    "ALLOW_REFRESH_TOKEN_AUTH",
    "ALLOW_USER_SRP_AUTH",
    "ALLOW_USER_PASSWORD_AUTH"
  ]

  # Prevent user existence errors (security: generic error messages)
  prevent_user_existence_errors = "ENABLED"

  depends_on = [aws_cognito_identity_provider.google]
}

# =============================================================================
# Google Identity Provider (conditional)
# =============================================================================

resource "aws_cognito_identity_provider" "google" {
  count = local.enable_google ? 1 : 0

  user_pool_id  = aws_cognito_user_pool.main.id
  provider_name = "Google"
  provider_type = "Google"

  provider_details = {
    client_id                     = var.google_client_id
    client_secret                 = var.google_client_secret
    authorize_scopes              = "openid email profile"
    attributes_url                = "https://people.googleapis.com/v1/people/me?personFields="
    attributes_url_add_attributes = "true"
    authorize_url                 = "https://accounts.google.com/o/oauth2/v2/auth"
    oidc_issuer                   = "https://accounts.google.com"
    token_request_method          = "POST"
    token_url                     = "https://www.googleapis.com/oauth2/v4/token"
  }

  attribute_mapping = {
    email    = "email"
    name     = "name"
    username = "sub"
  }

  lifecycle {
    ignore_changes = [provider_details["client_secret"]]
  }
}

# =============================================================================
# Cognito User Pool Domain (required for hosted UI / OAuth flows)
# =============================================================================

resource "aws_cognito_user_pool_domain" "main" {
  domain       = "${var.environment}-guidedmentor"
  user_pool_id = aws_cognito_user_pool.main.id
}
