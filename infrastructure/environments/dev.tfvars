environment               = "dev"
aws_region                = "ap-southeast-2"

# Dev environment: cost-optimised, skip expensive services
enable_aurora             = false
enable_waf                = false
enable_cmk                = false
enable_rds_proxy          = false
enable_bedrock_guardrails = false
enable_alarms             = false
enable_s3_replication     = false

aurora_multi_az           = false
aurora_min_acu            = 0.5
aurora_max_acu            = 2
