/**
 * formatErrorMessage — Converts API error codes and technical messages
 * to friendly, non-technical error messages for users.
 *
 * Requirements: 25.9
 */

export interface FriendlyError {
  /** User-facing message (non-technical) */
  message: string;
  /** Whether the error is retryable */
  canRetry: boolean;
  /** Optional "Learn more" link */
  learnMoreHref?: string;
}

/**
 * Map of HTTP status codes to friendly error messages.
 */
const statusMessageMap: Record<number, FriendlyError> = {
  400: {
    message: 'Some of the information provided is incorrect. Please check your inputs and try again.',
    canRetry: false,
  },
  401: {
    message: 'Your session has expired. Please sign in again to continue.',
    canRetry: false,
  },
  403: {
    message: "You don't have permission to perform this action.",
    canRetry: false,
  },
  404: {
    message: "We couldn't find what you're looking for. It may have been moved or removed.",
    canRetry: false,
  },
  409: {
    message: 'This action conflicts with a recent change. Please refresh and try again.',
    canRetry: true,
  },
  429: {
    message: "You're making too many requests. Please wait a moment and try again.",
    canRetry: true,
  },
  500: {
    message: 'Something went wrong on our end. Please try again in a moment.',
    canRetry: true,
  },
  502: {
    message: "We're having trouble connecting to our servers. Please try again shortly.",
    canRetry: true,
  },
  503: {
    message: "The service is temporarily unavailable. We're working on it — please try again soon.",
    canRetry: true,
  },
  504: {
    message: 'The request took too long to process. Please try again.',
    canRetry: true,
  },
};

/**
 * Map of common error code strings to friendly messages.
 */
const errorCodeMap: Record<string, FriendlyError> = {
  NETWORK_ERROR: {
    message: "Unable to connect. Please check your internet connection and try again.",
    canRetry: true,
  },
  TIMEOUT: {
    message: 'The request took too long. Please try again.',
    canRetry: true,
  },
  MENTOR_AT_CAPACITY: {
    message: "This mentor has reached their maximum number of mentees. Try another mentor or check back later.",
    canRetry: false,
  },
  SESSION_LOCKED: {
    message: "This session is currently being viewed by someone else. Please try again in a moment.",
    canRetry: true,
  },
  AI_UNAVAILABLE: {
    message: "AI features are temporarily unavailable. Your request has been queued and will be processed shortly.",
    canRetry: true,
  },
  FILE_TOO_LARGE: {
    message: "The file is too large to upload. Please choose a smaller file (max 5 MB).",
    canRetry: false,
  },
  INVALID_FILE_TYPE: {
    message: "This file type isn't supported. Please upload a PDF or DOCX file.",
    canRetry: false,
  },
  RATE_LIMIT_EXCEEDED: {
    message: "You've made too many requests. Please wait a moment before trying again.",
    canRetry: true,
  },
};

export interface FormatErrorOptions {
  /** HTTP status code from the API response */
  statusCode?: number;
  /** Error code string from the API (e.g., "NETWORK_ERROR") */
  errorCode?: string;
  /** Raw error message from the API (used as fallback context) */
  rawMessage?: string;
}

/**
 * Converts API errors to friendly, non-technical user-facing messages.
 * Prioritizes error code over status code for specificity.
 */
export function formatErrorMessage(options: FormatErrorOptions): FriendlyError {
  const { statusCode, errorCode, rawMessage } = options;

  // First, check specific error codes (most specific)
  if (errorCode && errorCode in errorCodeMap) {
    return errorCodeMap[errorCode]!;
  }

  // Then check HTTP status codes
  if (statusCode && statusCode in statusMessageMap) {
    return statusMessageMap[statusCode]!;
  }

  // Check for network-level errors from the raw message
  if (rawMessage) {
    const lowerMessage = rawMessage.toLowerCase();
    if (lowerMessage.includes('network') || lowerMessage.includes('fetch')) {
      return errorCodeMap['NETWORK_ERROR'] as FriendlyError;
    }
    if (lowerMessage.includes('timeout') || lowerMessage.includes('timed out')) {
      return errorCodeMap['TIMEOUT'] as FriendlyError;
    }
  }

  // Default fallback: assume retryable server error
  return {
    message: 'Something went wrong. Please try again.',
    canRetry: true,
  };
}
