export type EventSlug =
  | 'national-msme-summit-2024'
  | 'digital-transformation-webinar'
  | 'msme-loan-workshop'
  | 'govt-schemes-masterclass'
  | 'women-entrepreneur-summit'
  | 'export-import-workshop';

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
  'national-msme-summit-2024': {
    title: 'National MSME Summit 2024',
    crumb: 'National Summit',
    subtitle: "India's largest MSME gathering — March 15–16, 2024, BKC Mumbai",
    emoji: '🏆',
    date: 'March 15–16, 2024',
    venue: 'BKC Convention Centre, Mumbai',
    type: 'Summit',
    price: '₹499 (Early Bird)',
    body:
      "<h2>About the Event</h2><p>The National MSME Summit is India's premier platform for MSME entrepreneurs, government officials and industry leaders to connect, collaborate and grow. The 4th edition brings together 1,200+ participants from across India.</p><h2>Agenda Highlights</h2><p>Day 1 focuses on Finance & Government Schemes — keynote on Union Budget impact, panel on CGTMSE reforms, working session on PMEGP applications. Day 2 covers Technology & Growth — digital transformation showcase, IPO readiness workshop, and networking dinner.</p><h2>Speakers</h2><p>Guest of Honour: Secretary, Ministry of MSME | Keynote: MD & CEO, SIDBI | Panel: Zone Directors of major banks | Tech Session: Founders of MSME tech startups</p>",
    regNote: 'Limited seats — register early to avoid disappointment'
  },
  'digital-transformation-webinar': {
    title: 'Digital Transformation for MSMEs',
    crumb: 'Digital Webinar',
    subtitle: 'Free weekly webinar every Saturday 11 AM on Zoom',
    emoji: '💻',
    date: 'Every Saturday, 11:00 AM – 12:30 PM',
    venue: 'Zoom (Online)',
    type: 'Webinar',
    price: 'FREE',
    body:
      "<h2>About the Webinar</h2><p>A free weekly webinar series covering how MSMEs can leverage digital technology to grow faster. Topics rotate weekly covering websites, social media marketing, ERP systems, e-commerce, digital payments and more.</p><h2>What You'll Learn</h2><p>How to build a professional website in 7 days, use Google Ads to get 50+ leads/month, implement a basic ERP system, sell online on Amazon/Flipkart, and automate your business operations with free tools.</p><h2>Who Should Attend</h2><p>MSME owners, managers and entrepreneurs who want to embrace digital transformation. No technical background required — sessions are designed for business owners.</p>",
    regNote: 'Free entry — Join via Zoom link after registration'
  },
  'msme-loan-workshop': {
    title: 'MSME Loan & Finance Workshop',
    crumb: 'Loan Workshop',
    subtitle: 'Full-day workshop on MSME loans — Feb 28, 2024, New Delhi',
    emoji: '💰',
    date: 'February 28, 2024 | 10:00 AM – 4:00 PM',
    venue: 'Hotel Metropolitan, Connaught Place, New Delhi',
    type: 'Workshop',
    price: '₹299 inclusive of lunch',
    body:
      '<h2>About the Workshop</h2><p>A comprehensive full-day workshop covering everything an MSME owner needs to know about securing business loans. From documentation to bank selection to CIBIL improvement — all covered in one day with live case studies.</p><h2>Workshop Modules</h2><p>Module 1: Understanding your loan eligibility (2 hours) | Module 2: Documentation mastery — complete checklist (2 hours) | Module 3: Choosing the right bank and product (1 hour) | Module 4: Government credit guarantee schemes — CGTMSE, MUDRA (1 hour)</p>',
    regNote: 'Only 50 seats available — register now'
  },
  'govt-schemes-masterclass': {
    title: 'Government Schemes Masterclass',
    crumb: 'Schemes Masterclass',
    subtitle: 'Free masterclass every Wednesday 3 PM — PMEGP, CGTMSE, MUDRA',
    emoji: '🏛️',
    date: 'Every Wednesday, 3:00 PM – 4:30 PM',
    venue: 'Google Meet (Online)',
    type: 'Masterclass',
    price: 'FREE',
    body:
      '<h2>About the Masterclass</h2><p>A free online masterclass series dedicated to helping MSMEs understand, identify and apply for government schemes. Each session focuses on one or two specific schemes with step-by-step application guidance and live Q&A.</p>',
    regNote: 'Free — join via Google Meet link'
  },
  'women-entrepreneur-summit': {
    title: 'Women Entrepreneur Summit 2024',
    crumb: 'Women Summit',
    subtitle: 'Empowering women-led MSMEs — April 8, 2024, Pune',
    emoji: '👩‍💼',
    date: 'April 8, 2024 | 9:30 AM – 6:00 PM',
    venue: 'Pune International Centre, Pune',
    type: 'Summit',
    price: '₹299',
    body:
      '<h2>About the Summit</h2><p>A dedicated summit for women entrepreneurs and women-led MSMEs featuring special government schemes for women, mentorship from successful women business leaders, and networking with investors interested in women-led businesses.</p>',
    regNote: 'Special schemes for women MSMEs highlighted'
  },
  'export-import-workshop': {
    title: 'Export & Import Workshop',
    crumb: 'Export Workshop',
    subtitle: 'Hybrid workshop on export-import — March 5, 2024',
    emoji: '🌐',
    date: 'March 5, 2024 | 11:00 AM – 4:00 PM',
    venue: 'Hotel ITC, Mumbai + Online',
    type: 'Workshop',
    price: '₹499 (in-person) | ₹199 (online)',
    body:
      '<h2>About the Workshop</h2><p>Comprehensive workshop on how to start and scale export-import business. Covers IEC registration, documentation, finding international buyers, trade finance and government export promotion schemes.</p>',
    regNote: 'Both in-person and online participation available'
  }
};

