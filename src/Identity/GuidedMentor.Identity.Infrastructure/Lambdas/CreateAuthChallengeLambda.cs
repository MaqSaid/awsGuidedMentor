namespace GuidedMentor.Identity.Infrastructure.Lambdas;

/// <summary>
/// Placeholder — Cognito Lambda triggers are no longer used in the self-hosted stack.
/// Magic link auth is handled directly by the API using PostgreSQL auth_tokens table
/// and Gmail SMTP for email delivery.
/// This file is kept for project structure compatibility.
/// </summary>
public sealed class CreateAuthChallengePlaceholder
{
    // No-op: Magic link flow is now handled by:
    // 1. API generates token → saves to auth_tokens table (PostgreSQL)
    // 2. API sends email via GmailSmtpEmailSender (MailKit)
    // 3. Verification endpoint checks token in PostgreSQL
}
