# AI Risk Assessment

> GuidedMentor Platform — ISO/IEC 42001 (AI Management System) Compliance
>
> Last reviewed: 2025-01-15 | Next review: 2025-07-15

## 1. Scope

This assessment covers the two AI-powered features within the GuidedMentor platform:

1. **Session Plan Generator** — Generates personalised 35-minute mentorship session plans using Amazon Bedrock (Claude Sonnet 4) via the Converse API
2. **AI Help Assistant** — Provides contextual platform help via a floating chat interface using Vercel AI SDK `useChat()` backed by Bedrock Claude

## 2. AI System Inventory

| Component | Model | Provider | Deployment | Purpose |
|-----------|-------|----------|-----------|---------|
| Session Plan Generator | Claude Sonnet 4 | Amazon Bedrock | ap-southeast-2 | Generate structured session agendas from mentee/mentor profiles |
| AI Help Assistant | Claude Sonnet 4 | Amazon Bedrock | ap-southeast-2 | Answer platform usage questions in natural language |

## 3. Risk Assessment

### 3.1 Session Plan Generator

| Risk Category | Risk | Likelihood | Impact | Mitigation | Residual Risk |
|--------------|------|-----------|--------|-----------|---------------|
| **Output Quality** | Generated plan does not meet 35-minute constraint or schema | Medium | Low | Schema validation with retry (3 attempts); plan rejected if invalid | Low |
| **Harmful Content** | Model generates offensive, biased, or inappropriate content | Low | High | Bedrock Guardrails (content filters at HIGH threshold); output validation before persist | Very Low |
| **PII Leakage** | Model echoes or generates PII in session plan output | Low | High | Bedrock Guardrails PII redaction (ANONYMIZE); OutputValidator checks before persist | Very Low |
| **Prompt Injection** | Malicious input in mentee goals manipulates model behaviour | Medium | Medium | InputSanitizer strips control characters, escapes delimiters, enforces 2000-char max per field | Low |
| **Off-Topic Content** | Model generates content unrelated to AWS mentorship | Low | Low | Denied topic filters (financial, medical, political, religious, non-AWS) | Very Low |
| **Availability** | Bedrock API unavailable or rate-limited | Low | Medium | Polly v8 resilience pipeline (3 retries with exponential backoff + circuit breaker); fallback to pending-plan state with EventBridge async retry | Low |
| **Cost Overrun** | Unexpected token usage spikes | Low | Medium | Token usage logged as CloudWatch metrics; budget alerts at 50%/80%/100% thresholds | Low |
| **Model Drift** | Model behaviour changes with provider updates | Low | Medium | Model version tracking per session plan; human oversight (admin review/flag); alerts on quality degradation | Low |
| **Overreliance** | Users treat AI-generated plans as authoritative without review | Medium | Low | Plans presented as suggestions; mentors can modify; human oversight via admin flagging | Low |

### 3.2 AI Help Assistant

| Risk Category | Risk | Likelihood | Impact | Mitigation | Residual Risk |
|--------------|------|-----------|--------|-----------|---------------|
| **Harmful Content** | Model generates inappropriate help responses | Low | Medium | Bedrock Guardrails (same config as session plans); response filtering | Very Low |
| **PII Leakage** | Assistant reveals other users' data in responses | Very Low | High | Assistant has no access to user data; only platform documentation context | Very Low |
| **Hallucination** | Assistant provides incorrect platform guidance | Medium | Low | Constrained to platform documentation context; responses clearly labelled as AI-generated | Low |
| **Prompt Injection** | User attempts to manipulate assistant behaviour | Medium | Low | InputSanitizer applied; no tool-use or data access capabilities | Very Low |
| **Availability** | Bedrock API unavailable | Low | Low | Graceful degradation: "Assistant temporarily unavailable" message; no business-critical dependency | Very Low |

## 4. Controls Summary

### 4.1 Input Controls

| Control | Implementation | Requirement |
|---------|---------------|-------------|
| Input sanitization | `InputSanitizer.Sanitize()` — strips control chars, escapes delimiters | 7.10 |
| Input length enforcement | Max 2000 characters per user-provided field | 7.10 |
| Content filtering (input) | Bedrock Guardrails content policy (HIGH threshold) | 7.11 |
| Denied topics | Financial, medical, political, religious, non-AWS technology | 7.11 |

### 4.2 Output Controls

| Control | Implementation | Requirement |
|---------|---------------|-------------|
| Schema validation | `SessionPlan.IsValid()` — enforces all business invariants | 7.3, 7.4 |
| PII detection | Bedrock Guardrails PII redaction + `OutputValidator` check | 7.11, 7.12 |
| Harmful content filter | Bedrock Guardrails content filters (output) | 7.11, 7.12 |
| Output validation | `OutputValidator.Validate()` — no PII, no harmful content, schema conformance | 7.12 |

### 4.3 Operational Controls

| Control | Implementation | Requirement |
|---------|---------------|-------------|
| Model version tracking | `bedrockModelVersion` recorded per session plan in Sessions_Table | 21.17 |
| Human oversight | Super_Admin can review and flag AI-generated session plans | 21.17 |
| AI decision audit trail | Every Bedrock invocation logged: input hash, output hash, model version, latency | 21.17 |
| Token usage monitoring | CloudWatch custom metrics: input/output tokens per invocation | 7.9 |
| Resilience | Polly v8: retry 3x (2s/4s/8s backoff) + circuit breaker (5 failures/30s → 60s break) | 7.5, 24.5 |
| Async retry | EventBridge scheduled retry for failed generations (5-minute delay) | 7.6 |

### 4.4 Governance Controls

| Control | Implementation | Requirement |
|---------|---------------|-------------|
| This risk assessment document | Reviewed every 6 months | 21.17 (ISO 42001 — 6.1) |
| Model change review process | Any model version change requires staging validation + admin approval | 21.17 (ISO 42001 — 7.2) |
| Incident response for AI failures | Runbook: escalate if error rate > 10% or quality reports flagged | 21.17 (ISO 42001 — 9.1) |
| User feedback mechanism | Mentors can report unsatisfactory plans; admins review flagged content | 21.17 (ISO 42001 — 8.4) |

## 5. Responsible AI Principles

| Principle | GuidedMentor Approach |
|-----------|---------------------|
| **Transparency** | Users informed that session plans are AI-generated; model version visible to admins |
| **Fairness** | No demographic data used in plan generation; only skills/goals/expertise |
| **Privacy** | PII redacted from outputs; inputs sanitized; no user data stored in model |
| **Accountability** | Full audit trail; human oversight; admin flagging; model version tracking |
| **Safety** | Content filters; denied topics; output validation before persistence |
| **Human control** | Admins can review/flag/remove plans; mentors can modify plans; system never autonomous |

## 6. Incident Response

### AI-Specific Escalation Triggers

1. **Bedrock error rate > 10% over 5 minutes** → Auto-alarm → Ops team investigation
2. **Multiple admin flags on generated content** → Review prompt templates and guardrail config
3. **Model version change detected** → Trigger staging regression tests before production rollout
4. **Token usage anomaly (> 3x baseline)** → Investigate for prompt injection or abuse
5. **PII detected in output (post-guardrail)** → Immediate session plan removal + incident report

## 7. Review Schedule

This AI risk assessment is reviewed:
- Every 6 months as part of the AI governance cycle
- When AI model versions are updated by the provider
- When new AI features are introduced to the platform
- After any AI-related incident or near-miss
- When Bedrock Guardrails configuration changes
