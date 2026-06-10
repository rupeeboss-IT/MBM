export interface BllEventMeta {
  id: string;
  filename: string;
  imageUrl: string;
  title: string;
  subtitle?: string;
  date: string;
  time?: string;
  venue: string;
  shortDescription: string;
  description: string;
  highlights: string[];
  organizer: string;
  registrationInfo: string;
  price?: string;
  tagline?: string;
  association?: string;
}

/** Content derived from posters in /public/event — keyed by filename. */
export const BLL_EVENT_METADATA: Record<
  string,
  Omit<BllEventMeta, 'filename' | 'imageUrl'>
> = {
  'bll-event-1.jpg': {
    id: 'bll-business-topline-growth-meet-2026',
    title: 'Business Topline Growth Meet — 23rd Edition',
    subtitle: 'AAOGE TOH PAAOGE',
    date: '27th June 2026',
    time: '2:30 PM onwards',
    venue: 'Bombay Stock Exchange, Mumbai',
    tagline: 'AAOGE TOH PAAOGE',
    shortDescription:
      '23rd edition of BLL\'s flagship growth meet at the Bombay Stock Exchange — keynote sessions, networking, and topline growth strategies for business leaders.',
    description:
      'Join business leaders and entrepreneurs at the prestigious Bombay Stock Exchange for the 23rd edition of BLL\'s flagship Business Topline Growth Meet. Gain actionable insights on driving revenue growth, connect with industry peers, and learn proven strategies to scale your business topline.',
    highlights: [
      'Keynote sessions from industry experts on business growth',
      'Networking with business leaders and entrepreneurs',
      'Actionable topline growth strategies and best practices',
      'Held at the iconic Bombay Stock Exchange, Mumbai',
    ],
    organizer: 'BLL (Business Leadership League)',
    price: '₹1,499 + GST',
    registrationInfo:
      'Register at www.bll.org.in or scan the QR code on the event poster. Tickets priced at ₹1,499 + GST. Limited seats available — register early to secure your spot.',
  },
  'bll-event-2.jpg': {
    id: 'bll-psu-sme-access-summit-2026',
    title: 'Procurement Series: PSU SME Access Summit',
    subtitle: 'Thousands of Crores in PSU Tenders',
    date: '9th July 2026',
    venue: 'Jio World Convention Centre, BKC, Mumbai',
    association:
      'In association with MSME Development & Facilitation Office, Mumbai, Ministry of MSME, Govt. of India',
    shortDescription:
      'Connect your SME with India\'s largest PSUs — ONGC, IndianOil, NTPC & more — and unlock procurement opportunities worth thousands of crores.',
    description:
      'Get your SME in front of India\'s biggest buyers. This exclusive summit connects Small and Medium Enterprises with major Public Sector Undertakings including ONGC, IndianOil, NTPC, Bhakra Beas Management Board, and Western Railway — unlocking access to procurement opportunities worth thousands of crores.',
    highlights: [
      'PSU Procurement Strategy & Vendor Empanelment Process',
      'Open Tenders & Upcoming Pipeline for FY 2026–27',
      'How to qualify, register, and get shortlisted as a vendor',
      'Participating PSUs: ONGC, IndianOil, NTPC, BBMB, Western Railway',
      'Open to SMEs in Manufacturing, Services, Technology, Sustainability & Infrastructure',
    ],
    organizer: 'BLL (Business Leadership League)',
    registrationInfo:
      'Limited seats available. Apply for seats now via the Register Now link on the event poster or contact BLL for priority SME access.',
  },
};

export function buildFallbackMeta(filename: string): Omit<BllEventMeta, 'filename' | 'imageUrl'> {
  const label = filename.replace(/\.[^.]+$/, '').replace(/[-_]+/g, ' ');
  return {
    id: filename.replace(/\.[^.]+$/, ''),
    title: `BLL Event — ${label}`,
    date: 'Date to be announced',
    venue: 'Venue to be announced',
    shortDescription:
      'An upcoming BLL business event for industry leaders, entrepreneurs, and professionals.',
    description:
      'An upcoming BLL business event bringing together industry leaders, entrepreneurs, and professionals for networking and growth opportunities.',
    highlights: ['Industry networking', 'Expert-led sessions', 'Business growth insights'],
    organizer: 'BLL (Business Leadership League)',
    registrationInfo: 'Registration details will be announced soon. Visit www.bll.org.in for updates.',
  };
}

export function resolveBllEvent(filename: string): BllEventMeta {
  const meta = BLL_EVENT_METADATA[filename] ?? buildFallbackMeta(filename);
  return {
    ...meta,
    filename,
    imageUrl: `/event/${filename}`,
  };
}
