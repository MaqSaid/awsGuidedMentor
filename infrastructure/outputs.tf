output "identity_outputs" {
  description = "Outputs from the Identity bounded context module"
  value       = module.identity
}

output "mentoring_outputs" {
  description = "Outputs from the Mentoring bounded context module"
  value       = module.mentoring
}

output "content_outputs" {
  description = "Outputs from the Content bounded context module"
  value       = module.content
}

output "engagement_outputs" {
  description = "Outputs from the Engagement bounded context module"
  value       = module.engagement
}

output "networking_outputs" {
  description = "Outputs from the Networking module"
  value       = module.networking
}

output "analytics_outputs" {
  description = "Outputs from the Analytics module (only when Aurora is enabled)"
  value       = var.enable_aurora ? module.analytics[0] : null
}

output "observability_outputs" {
  description = "Outputs from the Observability module"
  value       = module.observability
}
