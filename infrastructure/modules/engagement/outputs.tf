output "notifications_table_name" {
  description = "DynamoDB Notifications table name"
  value       = aws_dynamodb_table.notifications.name
}

output "notifications_table_arn" {
  description = "DynamoDB Notifications table ARN"
  value       = aws_dynamodb_table.notifications.arn
}

output "meetups_table_name" {
  description = "DynamoDB Meetups table name"
  value       = aws_dynamodb_table.meetups.name
}

output "meetups_table_arn" {
  description = "DynamoDB Meetups table ARN"
  value       = aws_dynamodb_table.meetups.arn
}

output "engagement_events_table_name" {
  description = "DynamoDB EngagementEvents table name"
  value       = aws_dynamodb_table.engagement_events.name
}

output "engagement_events_table_arn" {
  description = "DynamoDB EngagementEvents table ARN"
  value       = aws_dynamodb_table.engagement_events.arn
}

output "appsync_api_id" {
  description = "AppSync GraphQL API ID for real-time subscriptions"
  value       = aws_appsync_graphql_api.notifications.id
}

output "appsync_api_url" {
  description = "AppSync GraphQL API URL"
  value       = aws_appsync_graphql_api.notifications.uris["GRAPHQL"]
}

output "appsync_api_realtime_url" {
  description = "AppSync GraphQL real-time WebSocket URL"
  value       = aws_appsync_graphql_api.notifications.uris["REALTIME"]
}
