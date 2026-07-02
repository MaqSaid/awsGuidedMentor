---
inclusion: fileMatch
fileMatchPattern: "**/Auth/**"
---

# Authentication Patterns

## Overview
GuidedMentor uses passwordless authentication:
- **Google OAuth** — primary, instant sign-in
- **Magic Link** — email-based, passwordless alternative (via Gmail SMTP)

## Magic Link Flow
1. POST /v1/auth/magic-link { email } → always returns 200 (prevents enumeration)
2. Email sent via Gmail SMTP with link: /auth/verify?token=UUID&email=encoded
3. POST /v1/auth/verify-magic-link { email, token } → returns JWT tokens or 400

## Token Rules
- UUID format (Guid.NewGuid())
- 10-minute expiry (checked in PostgreSQL query)
- Single-use (marked used=true on verification)
- Rate limited: 3 per email per 15 minutes
- Expired tokens cleaned by Hangfire job every 5 minutes

## JWT Tokens (self-issued)
- Access token: 15-minute expiry (signed with HMAC-SHA256)
- Refresh token: 7-day rotating
- Silent refresh via /v1/auth/refresh
- Secret key from configuration (`Jwt:Secret`)

## Security
- No passwords stored anywhere
- Generic responses (prevent email enumeration)
- Tokens are unguessable (122 bits of entropy)
- Expired/used tokens cleaned by Hangfire job (replaces DynamoDB TTL)

## Endpoints
- POST /v1/auth/magic-link — request magic link (anonymous)
- POST /v1/auth/verify-magic-link — verify token (anonymous)
- POST /v1/auth/google — Google OAuth (anonymous)
- POST /v1/auth/signout — invalidate tokens (authenticated)
- POST /v1/auth/refresh — silent token refresh (authenticated)

## Application Layer
- RequestMagicLinkCommand/Handler — validates email, checks rate limit, calls IMagicLinkService
- VerifyMagicLinkCommand/Handler — validates inputs, calls IMagicLinkService.VerifyAndAuthenticateAsync
- IMagicLinkService — interface in Application, implementation in Infrastructure

## Infrastructure Implementation
- MagicLinkService stores tokens in `auth_tokens` PostgreSQL table via EF Core
- Sends magic link email via `IEmailSender` (GmailSmtpEmailSender)
- JWT generation uses `System.IdentityModel.Tokens.Jwt`
- Rate limiting checked via COUNT query on auth_tokens table
