import type { OfferingSlug } from './offerings.data';
import { OFFERING_EXPLORER_ORDER } from './offering-display-order';

export type OfferingExplorerCard = {
  slug: OfferingSlug;
  icon: string;
  iconBg: string;
  title: string;
  description: string;
  badge: string;
  badgeBg: string;
  badgeColor: string;
};

const CARD_BY_SLUG: Record<OfferingSlug, OfferingExplorerCard> = {
  'trust-score': {
    slug: 'trust-score',
    icon: '⭐',
    iconBg: 'linear-gradient(135deg,#7b1fa2,#4a148c)',
    title: 'Infomerics IVerified & TrustScore',
    description: 'SEBI-registered Infomerics reports — build credibility',
    badge: 'Premium & Pro',
    badgeBg: '#fff3e0',
    badgeColor: '#e65100',
  },
  'business-diagnostic': {
    slug: 'business-diagnostic',
    icon: '📈',
    iconBg: 'linear-gradient(135deg,#ED751C,#c45a10)',
    title: 'Business Diagnostic + Coach Session',
    description: 'Business health assessment + free 30-min growth strategy session',
    badge: 'All Plans',
    badgeBg: '#fde8d8',
    badgeColor: '#ED751C',
  },
  'scheme-discovery': {
    slug: 'scheme-discovery',
    icon: '🏛️',
    iconBg: 'linear-gradient(135deg,#2d6a4f,#1b4332)',
    title: 'Scheme Discovery Report',
    description: 'Personalized govt schemes list based on your Udyam profile',
    badge: 'Premium & Pro',
    badgeBg: '#fff3e0',
    badgeColor: '#e65100',
  },
  'gem-registration': {
    slug: 'gem-registration',
    icon: '🏛️',
    iconBg: 'linear-gradient(135deg,#1565c0,#0d47a1)',
    title: 'GeM Portal Registration',
    description: 'Sell to government buyers — 25% MSME quota, no EMD',
    badge: 'Pro Only',
    badgeBg: '#ede7f6',
    badgeColor: '#4a148c',
  },
  'bank-statement-analyzer': {
    slug: 'bank-statement-analyzer',
    icon: '🏦',
    iconBg: 'linear-gradient(135deg,#1a56db,#0f3460)',
    title: 'Bank Statement Analyzer',
    description: 'Automated cash flow analysis + loan eligibility report',
    badge: 'Premium & Pro',
    badgeBg: '#fff3e0',
    badgeColor: '#e65100',
  },
  'credit-report': {
    slug: 'credit-report',
    icon: '📊',
    iconBg: 'linear-gradient(135deg,#e63946,#c1121f)',
    title: 'Free Credit Report',
    description: 'CIBIL score + full credit history + monthly updates',
    badge: 'All Plans',
    badgeBg: '#e8f5e9',
    badgeColor: '#2d6a4f',
  },
  'basic-website': {
    slug: 'basic-website',
    icon: '🌐',
    iconBg: 'linear-gradient(135deg,#0f3460,#1a56db)',
    title: 'Website Development',
    description: 'Business/Portfolio/E-commerce site + hosting + qobo.dev subdomain',
    badge: 'All Plans',
    badgeBg: '#e8f5e9',
    badgeColor: '#2d6a4f',
  },
    'practo-insurance': {
    slug: 'practo-insurance',
    icon: '🛡️',
    iconBg: 'linear-gradient(135deg,#2d6a4f,#52b788)',
    title: 'Practo Health Plan',
    description: 'Family health cover + accident protection + hospital cash',
    badge: 'MSME Basic',
    badgeBg: '#e3f2fd',
    badgeColor: '#1565c0',
  },
  'whatsapp-platform': {
    slug: 'whatsapp-platform',
    icon: '💬',
    iconBg: 'linear-gradient(135deg,#25d366,#128c7e)',
    title: 'WhatsApp Platform',
    description: 'Run WhatsApp campaigns, automate replies, capture leads',
    badge: 'All Plans',
    badgeBg: '#e8f5e9',
    badgeColor: '#2d6a4f',
  },
  'relationship-manager': {
    slug: 'relationship-manager',
    icon: '👔',
    iconBg: 'linear-gradient(135deg,#6d28d9,#4c1d95)',
    title: 'Free Relationship Manager',
    description: 'Dedicated financial expert — single point of contact, free',
    badge: 'Premium & Pro',
    badgeBg: '#fff3e0',
    badgeColor: '#e65100',
  },
  'loan-audit': {
    slug: 'loan-audit',
    icon: '🔍',
    iconBg: 'linear-gradient(135deg,#e65100,#bf360c)',
    title: 'Free Loan Audit',
    description: 'Reduce EMIs, save on interest, restructure existing loans',
    badge: 'Premium & Pro',
    badgeBg: '#fff3e0',
    badgeColor: '#e65100',
  },
  'insurance-audit': {
    slug: 'insurance-audit',
    icon: '🛡️',
    iconBg: 'linear-gradient(135deg,#0277bd,#01579b)',
    title: 'Free Insurance Audit',
    description: 'Optimize premiums, fix coverage gaps, eliminate duplicates',
    badge: 'Premium & Pro',
    badgeBg: '#fff3e0',
    badgeColor: '#e65100',
  },
  'msme-events': {
    slug: 'msme-events',
    icon: '🎟️',
    iconBg: 'linear-gradient(135deg,#e63946,#c1121f)',
    title: 'MSME Events Access',
    description: 'Summits, trade fairs, workshops — meet buyers and investors',
    badge: 'Premium & Pro',
    badgeBg: '#fff3e0',
    badgeColor: '#e65100',
  },

};

export const OFFERING_EXPLORER_CARDS: OfferingExplorerCard[] = OFFERING_EXPLORER_ORDER.map(
  (slug) => CARD_BY_SLUG[slug],
);
