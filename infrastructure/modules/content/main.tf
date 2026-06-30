# Content Context Module
# Manages: Bedrock model access, Guardrails configuration, Lambda functions
#           for session plan generation and streaming

# ─────────────────────────────────────────────────────────────────────────────
# Amazon Bedrock Guardrails
# ─────────────────────────────────────────────────────────────────────────────
# Configured with:
# - Content filters blocking harmful/toxic content (HIGH threshold)
# - Denied topic filters preventing off-platform discussion
# - PII redaction on outputs (email, phone, address, SSN, credit card)
# - Word filters blocking profanity
#
# Environment-conditional:
#   dev  → guardrails disabled (enable_bedrock_guardrails = false)
#   staging/prod → fully active (enable_bedrock_guardrails = true)

resource "aws_bedrock_guardrail" "content_guardrail" {
  count = var.enable_bedrock_guardrails ? 1 : 0

  name                      = "guidedmentor-content-guardrail-${var.environment}"
  description               = "Content safety guardrail for GuidedMentor session plan generation. Blocks harmful content, denies non-mentoring topics, and redacts PII from AI outputs."
  blocked_input_messaging   = "Your input contains content that cannot be processed. Please rephrase your request to focus on AWS mentorship topics."
  blocked_outputs_messaging = "The AI response was filtered because it contained inappropriate content. A new response will be generated."

  # ── Content Filters ──────────────────────────────────────────────────────
  # Block harmful content across all categories at HIGH threshold
  content_policy_config {
    filters_config {
      type            = "HATE"
      input_strength  = "HIGH"
      output_strength = "HIGH"
    }
    filters_config {
      type            = "INSULTS"
      input_strength  = "HIGH"
      output_strength = "HIGH"
    }
    filters_config {
      type            = "SEXUAL"
      input_strength  = "HIGH"
      output_strength = "HIGH"
    }
    filters_config {
      type            = "VIOLENCE"
      input_strength  = "HIGH"
      output_strength = "HIGH"
    }
    filters_config {
      type            = "MISCONDUCT"
      input_strength  = "HIGH"
      output_strength = "HIGH"
    }
    filters_config {
      type            = "PROMPT_ATTACK"
      input_strength  = "HIGH"
      output_strength = "NONE"
    }
  }

  # ── Denied Topics ────────────────────────────────────────────────────────
  # Prevent off-platform and non-mentoring discussions
  topic_policy_config {
    topics_config {
      name       = "financial-advice"
      type       = "DENY"
      definition = "Providing specific financial advice, investment recommendations, tax guidance, or personal financial planning that is not directly related to AWS career development or salary negotiation context within mentoring."
      examples = [
        "What stocks should I invest in?",
        "How should I manage my retirement fund?",
        "Is cryptocurrency a good investment?"
      ]
    }
    topics_config {
      name       = "medical-advice"
      type       = "DENY"
      definition = "Providing medical diagnoses, treatment recommendations, mental health counselling, or any health-related professional advice."
      examples = [
        "What medication should I take for anxiety?",
        "Can you diagnose my symptoms?",
        "Should I see a doctor about this?"
      ]
    }
    topics_config {
      name       = "political-discussion"
      type       = "DENY"
      definition = "Engaging in political debate, endorsing political parties or candidates, discussing government policies unrelated to cloud computing or technology regulation."
      examples = [
        "Which political party has better tech policies?",
        "What do you think about the current government?",
        "Should I vote for this candidate?"
      ]
    }
    topics_config {
      name       = "religious-discussion"
      type       = "DENY"
      definition = "Discussing religious beliefs, promoting or criticising religious practices, or providing spiritual guidance."
      examples = [
        "Which religion is the best?",
        "Should I pray before my certification exam?",
        "What does your faith say about technology?"
      ]
    }
    topics_config {
      name       = "non-aws-technology"
      type       = "DENY"
      definition = "Providing detailed guidance, tutorials, or recommendations for non-AWS cloud platforms (Azure, GCP) or technologies completely unrelated to the AWS ecosystem and cloud computing mentorship."
      examples = [
        "How do I set up Azure DevOps?",
        "Teach me Google Cloud Platform basics",
        "How do I configure a Salesforce integration?"
      ]
    }
  }

  # ── PII Redaction (Sensitive Information) ────────────────────────────────
  # Redact PII from AI outputs to protect user privacy
  sensitive_information_policy_config {
    pii_entities_config {
      type   = "EMAIL"
      action = "ANONYMIZE"
    }
    pii_entities_config {
      type   = "PHONE"
      action = "ANONYMIZE"
    }
    pii_entities_config {
      type   = "ADDRESS"
      action = "ANONYMIZE"
    }
    pii_entities_config {
      type   = "US_SOCIAL_SECURITY_NUMBER"
      action = "ANONYMIZE"
    }
    pii_entities_config {
      type   = "CREDIT_DEBIT_CARD_NUMBER"
      action = "ANONYMIZE"
    }
  }

  # ── Word Filters ─────────────────────────────────────────────────────────
  word_policy_config {
    managed_word_lists_config {
      type = "PROFANITY"
    }
  }

  tags = {
    Environment    = var.environment
    Service        = "Content"
    BoundedContext = "Content"
  }
}

# Create a version of the guardrail for use in Bedrock invocations
resource "aws_bedrock_guardrail_version" "content_guardrail_version" {
  count = var.enable_bedrock_guardrails ? 1 : 0

  guardrail_arn = aws_bedrock_guardrail.content_guardrail[0].guardrail_arn
  description   = "Initial version - content filters, denied topics, PII redaction"
}

# ─────────────────────────────────────────────────────────────────────────────
# Placeholder resources (to be implemented in subsequent tasks)
# ─────────────────────────────────────────────────────────────────────────────

# Amazon Bedrock model access will be configured here
# - Claude Sonnet 4 via Converse API (direct inference)
# - Model invocation logging to CloudWatch

# Lambda functions will be defined here
# - Session plan generator (Semantic Kernel + IChatClient)
# - Plan streaming handler (SSE for useObject())

# EventBridge rules will be defined here
# - Retry events for failed plan generation
# - Plan generation completion events

# SQS Dead Letter Queue will be defined here
# - Failed plan generation retries

# API Gateway integration will be defined here
# - /v1/sessions/{id}/generate-plan, /v1/sessions/{id}/plan/stream
