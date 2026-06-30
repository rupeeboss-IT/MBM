import { hasSchemeDiscoveryIntent } from './scheme-discovery-intent';
import { peekStashedMembershipSource } from './membership-source.util';

const STORAGE_KEY = 'mbm_registration_mode';
const PENDING_PLAN_KEY = 'mbm_pending_plan';
const QUERY_PARAM = 'mode';
const FREE_MODE = 'free';

const PLAN_DISPLAY_NAMES: Record<string, string> = {
  basic: 'MSME Basic',
  premium: 'MSME Premium',
  pro: 'MSME Pro',
};

export interface LoginRegisterCta {
  queryParams: Record<string, string> | null;
  label: string;
  planCode: string | null;
  planLabel: string | null;
  isMembershipIntent: boolean;
}

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

export function peekPendingMembershipPlanCode(): string | null {
  if (typeof window === 'undefined') return null;
  try {
    const code = window.localStorage.getItem(PENDING_PLAN_KEY)?.trim().toLowerCase() ?? '';
    return code || null;
  } catch {
    return null;
  }
}

export function formatMembershipPlanLabel(planCode: string | null | undefined): string | null {
  const code = (planCode ?? '').trim().toLowerCase();
  if (!code) return null;
  return PLAN_DISPLAY_NAMES[code] ?? null;
}

/** True when the user should continue membership/paid registration — not free signup. */
export function hasMembershipRegistrationIntent(): boolean {
  if (peekPendingMembershipPlanCode()) return true;
  if (hasSchemeDiscoveryIntent()) return true;
  if (peekStashedMembershipSource()) return true;
  return false;
}

export function getLoginRegisterCta(): LoginRegisterCta {
  const planCode = peekPendingMembershipPlanCode();
  const planLabel = formatMembershipPlanLabel(planCode);

  if (planCode) {
    return {
      queryParams: null,
      label: 'Create account',
      planCode,
      planLabel,
      isMembershipIntent: true,
    };
  }

  if (hasMembershipRegistrationIntent()) {
    return {
      queryParams: null,
      label: 'Create account',
      planCode: null,
      planLabel: null,
      isMembershipIntent: true,
    };
  }

  return {
    queryParams: { ...FREE_REGISTER_QUERY_PARAMS },
    label: 'Create free account',
    planCode: null,
    planLabel: null,
    isMembershipIntent: false,
  };
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
