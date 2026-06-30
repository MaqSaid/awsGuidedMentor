---
name: add-terraform-resource
description: Scaffolds a new Terraform resource following project conventions (module file + variables + outputs + tagging)
inclusion: manual
---

# Add Terraform Resource

## Input Required
- Module (observability, security, networking, identity, mentoring, engagement, content, events)
- Resource type (e.g., aws_cloudwatch_metric_alarm, aws_sqs_queue)
- Purpose

## Steps

1. **Create file** in `infrastructure/modules/{module}/{resource-name}.tf`
2. **Add variables** if configurable (in module's `variables.tf`)
3. **Add outputs** if consumed by other modules
4. **Apply conventions**:
   - Name: `${var.environment}-guidedmentor-{resource}`
   - Tags: `Environment`, `Service = "guidedmentor"`, `BoundedContext`, `Purpose`
   - Conditional: `count = var.enable_{feature} ? 1 : 0` for optional resources
   - `lifecycle { prevent_destroy = true }` on data stores
5. **Verify**: `terraform fmt -check && terraform validate`
