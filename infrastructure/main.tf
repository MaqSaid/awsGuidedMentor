provider "aws" {
  region = var.aws_region

  default_tags {
    tags = {
      Project     = "GuidedMentor"
      Environment = var.environment
      ManagedBy   = "Terraform"
    }
  }
}

# --- Bounded Context Modules ---

module "identity" {
  source = "./modules/identity"

  environment          = var.environment
  aws_region           = var.aws_region
  enable_cmk           = var.enable_cmk
  enable_waf           = var.enable_waf
  enable_alarms        = var.enable_alarms
  kms_key_arn          = var.kms_key_arn
  google_client_id     = var.google_client_id
  google_client_secret = var.google_client_secret
  callback_urls        = var.callback_urls
  logout_urls          = var.logout_urls
}

module "mentoring" {
  source = "./modules/mentoring"

  environment               = var.environment
  aws_region                = var.aws_region
  enable_cmk                = var.enable_cmk
  enable_alarms             = var.enable_alarms
}

module "content" {
  source = "./modules/content"

  environment               = var.environment
  aws_region                = var.aws_region
  enable_cmk                = var.enable_cmk
  enable_bedrock_guardrails = var.enable_bedrock_guardrails
  enable_alarms             = var.enable_alarms
}

module "engagement" {
  source = "./modules/engagement"

  environment          = var.environment
  aws_region           = var.aws_region
  enable_cmk           = var.enable_cmk
  enable_alarms        = var.enable_alarms
  cognito_user_pool_id = module.identity.user_pool_id
}

module "security" {
  source = "./modules/security"

  environment = var.environment
  aws_region  = var.aws_region
  enable_cmk  = var.enable_cmk
  enable_waf  = var.enable_waf
}

module "networking" {
  source = "./modules/networking"

  environment           = var.environment
  aws_region            = var.aws_region
  enable_waf            = var.enable_waf
  cognito_user_pool_id  = module.identity.user_pool_id
  cognito_user_pool_arn = module.identity.user_pool_arn
  waf_web_acl_arn       = module.security.waf_web_acl_arn
}

module "analytics" {
  source = "./modules/analytics"
  count  = var.enable_aurora ? 1 : 0

  environment      = var.environment
  aws_region       = var.aws_region
  enable_cmk       = var.enable_cmk
  kms_key_arn      = var.kms_key_arn
  enable_alarms    = var.enable_alarms
  aurora_multi_az  = var.aurora_multi_az
  aurora_min_acu   = var.aurora_min_acu
  aurora_max_acu   = var.aurora_max_acu
  enable_rds_proxy = var.enable_rds_proxy
}

module "events" {
  source = "./modules/events"

  environment  = var.environment
  aws_region   = var.aws_region
  enable_aurora = var.enable_aurora
}

module "observability" {
  source = "./modules/observability"

  environment          = var.environment
  aws_region           = var.aws_region
  enable_alarms        = var.enable_alarms
  budget_amount        = var.budget_amount
  alarm_email          = var.alarm_email
  dynamodb_table_names = [
    "${var.environment}-guidedmentor-users",
    "${var.environment}-guidedmentor-mentors",
    "${var.environment}-guidedmentor-mentees",
    "${var.environment}-guidedmentor-sessions",
    "${var.environment}-guidedmentor-notifications",
  ]
  dlq_names = [
    "${var.environment}-guidedmentor-dlq-lock-cleanup",
    "${var.environment}-guidedmentor-dlq-notification-digest",
    "${var.environment}-guidedmentor-dlq-availability-reminder",
    "${var.environment}-guidedmentor-dlq-analytics-aggregation",
  ]
}
