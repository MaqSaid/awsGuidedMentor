---
inclusion: fileMatch
fileMatchPattern: "**/*.tf"
---

# Terraform Conventions

## Module Structure
- One file per logical resource group (e.g., `cost-anomaly.tf`, `cloudtrail.tf`)
- Variables in `variables.tf`, outputs in `outputs.tf` per module
- Root module at `infrastructure/main.tf` composes all modules

## Naming
- Resources: `${var.environment}-guidedmentor-{resource-name}`
- Variables: snake_case with description and type
- Outputs: snake_case, include `description`

## Tagging (required on ALL resources)
- `Environment` = var.environment
- `Service` = "guidedmentor"
- `BoundedContext` = Identity | Mentoring | Content | Engagement | Platform

## Patterns
- Conditional resources: `count = var.enable_{feature} ? 1 : 0`
- `lifecycle { prevent_destroy = true }` on data stores
- DynamoDB: on-demand billing, PITR enabled, KMS in staging/prod
- All S3 buckets: block public access, versioning, encryption
