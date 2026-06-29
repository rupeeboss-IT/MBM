const STORAGE_KEY = 'mbm_registration_mode';
const PENDING_PLAN_KEY = 'mbm_pending_plan';
const QUERY_PARAM = 'mode';
const FREE_MODE = 'free';

/** Capture `?mode=free` from the register URL; clear mode when param is absent or different. */
export function captureRegistrationModeFromUrl(search: string): void {
  if (typeof window === 'undefined') return;
  const params = new URLSearchParams(search.startsWith('?') ? search : `?${search}`);
  if (params.get(QUERY_PARAM) === FREE_MODE) {
    sessionStorage.setItem(STORAGE_KEY, FREE_MODE);
    try {
      window.localStorage.removeItem(PENDING_PLAN_KEY);
    } catch {
      // ignore
    }
    return;
  }
  sessionStorage.removeItem(STORAGE_KEY);
}

export function isFreeRegistrationMode(): boolean {
  if (typeof window === 'undefined') return false;
  return sessionStorage.getItem(STORAGE_KEY) === FREE_MODE;
}

export function clearRegistrationMode(): void {
  if (typeof window === 'undefined') return;
  sessionStorage.removeItem(STORAGE_KEY);
}

export const FREE_REGISTER_QUERY_PARAMS = { mode: FREE_MODE } as const;
