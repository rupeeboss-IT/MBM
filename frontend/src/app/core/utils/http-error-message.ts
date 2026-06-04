import { HttpErrorResponse } from '@angular/common/http';
import { API_USER_MESSAGES, resolveApiFallback } from './api-user-messages';

const TECHNICAL_PATTERNS: RegExp[] = [
  /Http failure response/i,
  /^\s*at\s+/m,
  /\bLINQ\b/i,
  /\bEntity Framework\b/i,
  /\bEF Core\b/i,
  /\bSqlException\b/i,
  /\bMicrosoft\./i,
  /\bSystem\./i,
  /\bDbContext\b/i,
  /\bRepository\b/i,
  /\bController\b/i,
  /could not be translated/i,
  /translation of/i,
  /inner exception/i,
  /stack\s*trace/i,
  /connection string/i,
  /object reference not set/i,
  /nullreference/i,
  /\.cs:?\d*/i,
  /\bline\s+\d+\b/i,
  /[A-Za-z]:\\[\w\\.-]+/,
  /\/[\w./-]+\.(cs|dll|sql)\b/i,
  /\bsql server\b/i,
  /\binvalid column\b/i,
  /\binvalid object name\b/i,
];

function readMessageFromBody(body: unknown): string | null {
  if (typeof body === 'string') {
    const text = body.trim();
    if (!text || text.startsWith('<')) return null;
    return text;
  }
  if (!body || typeof body !== 'object') return null;
  const o = body as Record<string, unknown>;
  for (const key of ['message', 'Message']) {
    const v = o[key];
    if (typeof v === 'string' && v.trim()) return v.trim();
  }
  return null;
}

/** Returns true if the text looks like a technical exception (must not be shown to users). */
export function isTechnicalErrorMessage(message: string): boolean {
  const text = message.trim();
  if (!text) return true;
  if (text.length > 320) return true;
  return TECHNICAL_PATTERNS.some((pattern) => pattern.test(text));
}

/** Sanitizes an API message; returns null if unsafe to display. */
export function sanitizeUserMessage(message: string | null | undefined): string | null {
  if (!message?.trim()) return null;
  const text = message.trim();
  if (isTechnicalErrorMessage(text)) return null;
  return text;
}

/** Returns a user-friendly message from an HTTP/API error. */
export function getHttpErrorMessage(
  error: unknown,
  fallback?: string,
  context?: { url?: string; method?: string }
): string {
  const url = context?.url ?? '';
  const method = context?.method ?? 'GET';
  const defaultFallback = fallback ?? resolveApiFallback(url, method);

  if (error instanceof HttpErrorResponse) {
    const fromBody = sanitizeUserMessage(readMessageFromBody(error.error));
    if (fromBody) return fromBody;

    if (error.status === 0) return API_USER_MESSAGES.network;
    if (error.status === 401) return API_USER_MESSAGES.unauthorized;
    if (error.status === 403) return API_USER_MESSAGES.forbidden;
    if (error.status === 404) return API_USER_MESSAGES.notFound;
    if (error.status === 408) return 'The request is taking longer than expected. Please try again.';
    if (error.status === 429) {
      return 'Too many requests. Please wait a few minutes and try again.';
    }
    if (error.status >= 500) return resolveApiFallback(url, method);
    if (error.status >= 400) return defaultFallback;
  }

  if (error && typeof error === 'object' && 'message' in error) {
    const raw = String((error as { message?: unknown }).message ?? '').trim();
    const safe = sanitizeUserMessage(raw);
    if (safe) return safe;
  }

  return defaultFallback;
}
