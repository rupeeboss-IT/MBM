import type { OfferingSlug } from './offerings.data';

/** Canonical display order for core membership offerings (Plans / Services / Benefits). */
export const OFFERING_DISPLAY_ORDER: readonly OfferingSlug[] = [
  'trust-score',
  'business-diagnostic',
  'scheme-discovery',
  'gem-registration',
  'bank-statement-analyzer',
  'credit-report',
  'basic-website',
  'whatsapp-platform',
  'relationship-manager',
  'loan-audit',
  'insurance-audit',
  'msme-events',
] as const;

/** Explorer grid includes Practo after the core eleven. */
export const OFFERING_EXPLORER_ORDER: readonly OfferingSlug[] = [
  ...OFFERING_DISPLAY_ORDER,
  'practo-insurance',
];

export function offeringSortIndex(slug: string, order: readonly string[] = OFFERING_DISPLAY_ORDER): number {
  const i = order.indexOf(slug);
  return i === -1 ? Number.MAX_SAFE_INTEGER : i;
}

export function sortOfferingSlugs<T extends string>(slugs: readonly T[], order: readonly string[] = OFFERING_DISPLAY_ORDER): T[] {
  return [...slugs].sort((a, b) => offeringSortIndex(a, order) - offeringSortIndex(b, order));
}
