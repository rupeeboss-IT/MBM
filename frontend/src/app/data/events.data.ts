export type EventSlug =
  | 'bll-business-topline-growth-meet-2026'
  | 'bll-psu-sme-access-summit-2026';

export interface EventModel {
  title: string;
  crumb: string;
  subtitle: string;
  emoji: string;
  date: string;
  venue: string;
  type: string;
  price: string;
  body: string; // HTML string (rendered in template)
  regNote?: string;
}

export const EVENTS_DATA: Record<EventSlug, EventModel> = {
  'bll-business-topline-growth-meet-2026': {
    title: 'Business Topline Growth Meet — 23rd Edition',
    crumb: 'BLL Growth Meet',
    subtitle: 'AAOGE TOH PAAOGE — 27th June 2026, Bombay Stock Exchange, Mumbai',
    emoji: '🏆',
    date: '27th June 2026 | 2:30 PM onwards',
    venue: 'Bombay Stock Exchange, Mumbai',
    type: 'BLL',
    price: '₹1,499 + GST',
    body:
      '<h2>About the Event</h2><p>Join business leaders and entrepreneurs at the prestigious Bombay Stock Exchange for the 23rd edition of BLL\'s flagship Business Topline Growth Meet. Gain actionable insights on driving revenue growth, connect with industry peers, and learn proven strategies to scale your business topline under the theme AAOGE TOH PAAOGE.</p><h2>Event Highlights</h2><p>Keynote sessions from industry experts on business growth · Networking with business leaders and entrepreneurs · Actionable topline growth strategies · Held at the iconic Bombay Stock Exchange, Mumbai</p>',
    regNote: 'Register at www.bll.org.in or scan the QR code. Tickets at ₹1,499 + GST.',
  },
  'bll-psu-sme-access-summit-2026': {
    title: 'Procurement Series: PSU SME Access Summit',
    crumb: 'PSU SME Access Summit',
    subtitle: 'Thousands of Crores in PSU Tenders — 9th July 2026, Jio World Convention Centre, BKC, Mumbai',
    emoji: '🏛️',
    date: '9th July 2026',
    venue: 'Jio World Convention Centre, BKC, Mumbai',
    type: 'BLL',
    price: 'Limited Seats',
    body:
      '<h2>About the Event</h2><p>Get your SME in front of India\'s biggest buyers. This exclusive summit connects Small and Medium Enterprises with major Public Sector Undertakings including ONGC, IndianOil, NTPC, Bhakra Beas Management Board, and Western Railway — unlocking access to procurement opportunities worth thousands of crores.</p><h2>Event Highlights</h2><p>PSU Procurement Strategy & Vendor Empanelment Process · Open Tenders & Upcoming Pipeline for FY 2026–27 · How to qualify, register, and get shortlisted as a vendor · Open to SMEs in Manufacturing, Services, Technology, Sustainability & Infrastructure</p><h2>In Association With</h2><p>MSME Development & Facilitation Office, Mumbai, Ministry of MSME, Govt. of India</p>',
    regNote: 'Limited seats available. Apply for seats now via BLL.',
  },
};
