/**
 * Structured error response returned by all GuidedMentor API endpoints.
 * Mirrors the backend ApiErrorResponse DTO.
 */
export interface ApiErrorResponse {
  statusCode: number;
  error: string;
  message: string;
  correlationId: string;
  fieldErrors?: Record<string, string>;
}
