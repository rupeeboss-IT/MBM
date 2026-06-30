import {
  isSchemeDiscoveryMembershipSource,
  parseMembershipSource,
  type MembershipSource,
} from './membership-source.util';

const RETURN_URL_KEY = 'mbm_scheme_discovery_return';

const DEFAULT_RETURN_URL = '/offering/scheme-discovery';

export function setSchemeDiscoveryReturnUrl(url: string): void {
  if (typeof window === 'undefined') return;
  const path = (url ?? '').trim().split('?')[0];
  if (!path || path === '/membership' || path === '/login' || path === '/register') return;
  try {
    window.sessionStorage.setItem(RETURN_URL_KEY, path);
  } catch {
    // ignore
  }
}

export function peekSchemeDiscoveryReturnUrl(): string {
  if (typeof window === 'undefined') return DEFAULT_RETURN_URL;
  try {
    const raw = window.sessionStorage.getItem(RETURN_URL_KEY);
    return raw && raw.startsWith('/') ? raw : DEFAULT_RETURN_URL;
  } catch {
    return DEFAULT_RETURN_URL;
  }
}

export function consumeSchemeDiscoveryReturnUrl(): string {
  const url = peekSchemeDiscoveryReturnUrl();
  if (typeof window === 'undefined') return url;
  try {
    window.sessionStorage.removeItem(RETURN_URL_KEY);
  } catch {
    // ignore
  }
  return url;
}

export function isPremiumOrProPlanCode(planCode: string | null | undefined): boolean {
  const code = (planCode ?? '').trim().toLowerCase();
  return code === 'premium' || code === 'pro';
}

export const SCHEME_REPORT_ONE_TIME_PLAN_CODE = 'scheme-report-onetime';

export function isOneTimeSchemeReportPlanCode(planCode: string | null | undefined): boolean {
  return (planCode ?? '').trim().toLowerCase() === SCHEME_REPORT_ONE_TIME_PLAN_CODE;
}

export function shouldResumeSchemeDiscoveryAfterMembership(
  planCode: string | null | undefined,
  hasIntent: boolean,
  membershipSource: MembershipSource | null,
): boolean {
  if (!isPremiumOrProPlanCode(planCode)) return false;
  if (hasIntent) return true;
  return isSchemeDiscoveryMembershipSource(membershipSource);
}

export function membershipSourceFromQuery(raw: string | null | undefined): MembershipSource | null {
  return parseMembershipSource(raw);
}
