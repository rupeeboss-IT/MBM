/** JWT clock skew aligned with backend TokenValidationParameters.ClockSkew (1 minute). */
const DEFAULT_SKEW_MS = 60_000;

function decodeJwtPayload(token: string): Record<string, unknown> | null {
  const segment = token.split('.')[1];
  if (!segment) return null;

  try {
    const base64 = segment.replace(/-/g, '+').replace(/_/g, '/');
    const json = atob(base64);
    const payload = JSON.parse(json) as Record<string, unknown>;
    return payload && typeof payload === 'object' ? payload : null;
  } catch {
    return null;
  }
}

/** Returns JWT expiry in milliseconds since epoch, or null when missing or invalid. */
export function getJwtExpiryMs(token: string | null | undefined): number | null {
  if (!token?.trim()) return null;
  const payload = decodeJwtPayload(token.trim());
  const exp = payload?.['exp'];
  return typeof exp === 'number' ? exp * 1000 : null;
}

/** True when the token is missing, malformed, or past its expiry (with optional skew). */
export function isJwtExpired(
  token: string | null | undefined,
  skewMs: number = DEFAULT_SKEW_MS,
): boolean {
  if (!token?.trim()) return true;
  const expMs = getJwtExpiryMs(token);
  if (expMs == null) return true;
  return Date.now() >= expMs - skewMs;
}
