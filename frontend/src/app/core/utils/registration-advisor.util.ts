const STORAGE_KEY = 'mbm_registration_advisor_code';
const QUERY_PARAMS = ['advisor', 'referral'] as const;

/** Normalize advisor / referral code (RB* employee, GP* RBA broker). */
export function normalizeAdvisorCode(value: string | null | undefined): string | null {
  const v = (value ?? '').trim();
  return v.length > 0 ? v : null;
}

/** Persist optional advisor code from URL query (?advisor= or ?referral=). */
export function captureRegistrationAdvisorFromUrl(search: string): void {
  if (typeof window === 'undefined') return;
  const params = new URLSearchParams(search.startsWith('?') ? search : `?${search}`);
  for (const key of QUERY_PARAMS) {
    const code = normalizeAdvisorCode(params.get(key));
    if (code) {
      sessionStorage.setItem(STORAGE_KEY, code);
      return;
    }
  }
}

export function getRegistrationAdvisorCode(): string | null {
  if (typeof window === 'undefined') return null;
  return normalizeAdvisorCode(sessionStorage.getItem(STORAGE_KEY));
}

export function setRegistrationAdvisorCode(value: string | null | undefined): void {
  if (typeof window === 'undefined') return;
  const code = normalizeAdvisorCode(value);
  if (code) sessionStorage.setItem(STORAGE_KEY, code);
  else sessionStorage.removeItem(STORAGE_KEY);
}

export function clearRegistrationAdvisorCode(): void {
  if (typeof window === 'undefined') return;
  sessionStorage.removeItem(STORAGE_KEY);
}
