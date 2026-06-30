environment               = "prod"
aws_region                = "ap-southeast-2"

# Production environment: all features enabled, full capacity
enable_aurora             = true
enable_waf                = true
enable_cmk                = true
enable_rds_proxy          = true
enable_bedrock_guardrails = true
enable_alarms             = true
enable_s3_replication     = true

aurora_multi_az           = true
aurora_min_acu            = 0.5
aurora_max_acu            = 8
