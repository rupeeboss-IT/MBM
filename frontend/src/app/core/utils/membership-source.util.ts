export type MembershipSource = 'scheme-discovery' | 'premium-report' | 'government-scheme';

export type RecommendedPlanCode = 'premium' | 'pro';

export interface MembershipSourceGuidance {
  source: MembershipSource;
  featureLabel: string;
  bannerIntro: string;
  bannerRecommendation: string;
  bannerBasicNote: string;
  basicConfirmMessage: string;
  premiumBadge: string;
  proBadge: string;
}

const GUIDANCE: Record<MembershipSource, MembershipSourceGuidance> = {
  'scheme-discovery': {
    source: 'scheme-discovery',
    featureLabel: 'Scheme Discovery Report',
    bannerIntro: 'You are here because you selected Scheme Discovery Report.',
    bannerRecommendation:
      'To access this feature, a Pro or Premium Membership is recommended.',
    bannerBasicNote: 'Basic Membership may not include access to this feature.',
    basicConfirmMessage:
      'You originally selected Scheme Discovery Report.\n\nThe Basic Membership may not include access to Scheme Discovery benefits.\n\nWould you still like to continue with Basic Membership?',
    premiumBadge: '✓ Recommended',
    proBadge: '✓ Best for Scheme Discovery',
  },
  'government-scheme': {
    source: 'government-scheme',
    featureLabel: 'Government Scheme Report',
    bannerIntro: 'You are here because you selected a Government Scheme Report.',
    bannerRecommendation:
      'To access this feature, a Pro or Premium Membership is recommended.',
    bannerBasicNote: 'Basic Membership may not include access to this feature.',
    basicConfirmMessage:
      'You originally selected a Government Scheme Report.\n\nThe Basic Membership may not include access to government scheme report benefits.\n\nWould you still like to continue with Basic Membership?',
    premiumBadge: '✓ Recommended',
    proBadge: '✓ Best for Scheme Reports',
  },
  'premium-report': {
    source: 'premium-report',
    featureLabel: 'Premium Report',
    bannerIntro: 'You are here because you selected a Premium Report feature.',
    bannerRecommendation:
      'To access premium reports and related benefits, a Pro or Premium Membership is recommended.',
    bannerBasicNote: 'Basic Membership may not include access to this feature.',
    basicConfirmMessage:
      'You originally selected a Premium Report feature.\n\nThe Basic Membership may not include access to premium report benefits.\n\nWould you still like to continue with Basic Membership?',
    premiumBadge: '✓ Recommended',
    proBadge: '✓ Best Value',
  },
};

const PREMIUM_OFFERING_SLUGS = new Set([
  'trust-score',
  'bank-statement-analyzer',
  'loan-audit',
  'insurance-audit',
  'credit-report',
  'relationship-manager',
  'gem-registration',
  'msme-events',
  'practo-insurance',
]);

const SESSION_SOURCE_KEY = 'mbm_membership_source';

export function parseMembershipSource(raw: string | null | undefined): MembershipSource | null {
  const value = (raw ?? '').trim().toLowerCase();
  if (value === 'scheme-discovery' || value === 'premium-report' || value === 'government-scheme') {
    return value;
  }
  return null;
}

export function getMembershipSourceGuidance(
  source: MembershipSource | null | undefined,
): MembershipSourceGuidance | null {
  if (!source) return null;
  return GUIDANCE[source] ?? null;
}

export function membershipSourceFromOfferingSlug(slug: string): MembershipSource | null {
  const key = (slug ?? '').trim().toLowerCase();
  if (key === 'scheme-discovery') return 'scheme-discovery';
  if (PREMIUM_OFFERING_SLUGS.has(key)) return 'premium-report';
  return null;
}

/** Sources that resume the Scheme Discovery report journey after Pro/Premium purchase. */
export function isSchemeDiscoveryMembershipSource(source: MembershipSource | null | undefined): boolean {
  return source === 'scheme-discovery' || source === 'government-scheme';
}

export function membershipSourceQuery(source: MembershipSource | null): { source?: string } {
  return source ? { source } : {};
}

export function stashMembershipSource(source: MembershipSource | null): void {
  if (typeof window === 'undefined' || !source) return;
  try {
    window.sessionStorage.setItem(SESSION_SOURCE_KEY, source);
  } catch {
    // ignore
  }
}

export function consumeStashedMembershipSource(): MembershipSource | null {
  if (typeof window === 'undefined') return null;
  try {
    const raw = window.sessionStorage.getItem(SESSION_SOURCE_KEY);
    window.sessionStorage.removeItem(SESSION_SOURCE_KEY);
    return parseMembershipSource(raw);
  } catch {
    return null;
  }
}

export function isRecommendedPlan(code: string, source: MembershipSource | null): boolean {
  if (!source) return false;
  const normalized = code.toLowerCase();
  return normalized === 'premium' || normalized === 'pro';
}

export function shouldConfirmPlanChoice(code: string, source: MembershipSource | null): boolean {
  if (!source) return false;
  const normalized = code.toLowerCase();
  return normalized === 'basic' || normalized === 'standard';
}

export function getPlanConfirmMessage(
  guidance: MembershipSourceGuidance,
  planCode: 'basic' | 'standard',
): string {
  const planName = planCode === 'basic' ? 'Basic Membership' : 'Standard Membership';
  const accessNote =
    guidance.source === 'scheme-discovery'
      ? 'Scheme Discovery benefits'
      : guidance.source === 'government-scheme'
        ? 'government scheme report benefits'
        : 'premium report benefits';

  return `You originally selected ${guidance.featureLabel}.\n\nThe ${planName} may not include access to ${accessNote}.\n\nWould you still like to continue with ${planName}?`;
}

export function getPlanConfirmContinueLabel(planCode: 'basic' | 'standard'): string {
  return planCode === 'basic' ? 'Continue with Basic' : 'Continue with Standard';
}
