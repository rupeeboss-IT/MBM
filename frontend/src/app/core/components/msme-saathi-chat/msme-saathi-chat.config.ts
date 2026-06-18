export interface MbmChip {
  label: string;
  intent?: string;
  url?: string;
}

export interface MbmIntent {
  keywords?: string[];
  reply: string;
  chips?: MbmChip[];
}

export const MBM_CHAT_CONFIG = {
  botName: 'MSME Saathi',
  brand: 'MSME Bharat Manch',
  waNumber: '917982218029',
  registerUrl: 'https://www.msmebharatmanch.com/register',
} as const;

const WA = `https://wa.me/${MBM_CHAT_CONFIG.waNumber}`;

export function waLink(msg?: string): string {
  return WA + (msg ? `?text=${encodeURIComponent(msg)}` : '');
}

export const CHIPS_MAIN: MbmChip[] = [
  { label: '💰 Loans & Finance', intent: 'loans' },
  { label: '🏛️ Govt Schemes', intent: 'schemes' },
  { label: '🪪 Membership', intent: 'membership' },
  { label: '🧰 Business Services', intent: 'services' },
  { label: '🎁 Free Audits', intent: 'free' },
  { label: '📞 Talk to us', intent: 'contact' },
];

export const GREETINGS = [
  'hi',
  'hello',
  'hey',
  'namaste',
  'namaskar',
  'hii',
  'helo',
  'good morning',
  'good evening',
  'good afternoon',
  'yo',
  'start',
];

export const KB: Record<string, MbmIntent> = {
  welcome: {
    reply:
      '🙏 <b>Namaste!</b> I\'m <b>' +
      MBM_CHAT_CONFIG.botName +
      '</b>, your business companion at ' +
      '<b>' +
      MBM_CHAT_CONFIG.brand +
      '</b> — a national initiative powered by RupeeBoss.<br><br>' +
      'I can help you with loans, government schemes &amp; subsidies, membership and our ' +
      '360° business services. What would you like to explore?',
    chips: CHIPS_MAIN,
  },

  loans: {
    keywords: [
      'loan',
      'loans',
      'finance',
      'funding',
      'fund',
      'credit',
      'capital',
      'money',
      'borrow',
      'bank',
      'nbfc',
      'emi',
      'mudra',
      'cash',
    ],
    reply:
      '💰 <b>Loans &amp; Finance</b><br>' +
      'Through RupeeBoss we connect you to <b>100+ banks &amp; NBFCs</b> and find the loan that ' +
      'fits your business — with <b>free doorstep service</b>.<br><br>' +
      'Popular options: Business Loan, Working Capital, Term Loan, Loan Against Property (LAP), ' +
      'Machinery / Equipment Finance, MSME &amp; Mudra loans, and Bill Discounting.',
    chips: [
      { label: 'Apply now', intent: 'apply_loan' },
      { label: 'Loan types', intent: 'loan_types' },
      { label: 'Documents needed', intent: 'loan_docs' },
      { label: 'Free loan audit', intent: 'free' },
    ],
  },

  loan_types: {
    keywords: [
      'type of loan',
      'loan type',
      'kind of loan',
      'working capital',
      'term loan',
      'lap',
      'property loan',
      'machinery',
      'equipment',
      'bill discounting',
    ],
    reply:
      'Here are the main loan types we arrange:<br><br>' +
      '• <b>Business / Unsecured Loan</b> — quick funds, no collateral<br>' +
      '• <b>Working Capital</b> — CC / OD limits for day-to-day cash flow<br>' +
      '• <b>Term Loan</b> — expansion &amp; long-term needs<br>' +
      '• <b>Loan Against Property (LAP)</b> — higher amount against property<br>' +
      '• <b>Machinery / Equipment Finance</b><br>' +
      '• <b>MSME / Mudra</b> — government-backed small-business loans<br>' +
      '• <b>Bill / Invoice Discounting</b> — unlock money stuck in invoices',
    chips: [
      { label: 'Apply now', intent: 'apply_loan' },
      { label: 'Documents needed', intent: 'loan_docs' },
      { label: 'Back to menu', intent: 'welcome' },
    ],
  },

  apply_loan: {
    keywords: ['apply', 'application', 'get a loan', 'want loan', 'need loan', 'eligible', 'eligibility'],
    reply:
      'Applying is simple and the service is <b>free</b>. ✅<br><br>' +
      'Keep ready: business KYC, GST number, last 6–12 months bank statements and recent ITR. ' +
      'Our team reviews your profile and matches you with the right lender from 100+ options.<br><br>' +
      '👉 <b>Start your free application on WhatsApp</b> and we\'ll get back quickly.',
    chips: [
      {
        label: '📲 Apply on WhatsApp',
        url: waLink('Hi! I\'d like to apply for a business loan through MSME Bharat Manch.'),
      },
      { label: 'Documents needed', intent: 'loan_docs' },
      { label: 'Back to menu', intent: 'welcome' },
    ],
  },

  loan_docs: {
    keywords: ['document', 'documents', 'papers', 'kyc', 'required', 'need to submit', 'itr', 'gst return'],
    reply:
      '📄 <b>Documents usually needed</b> (varies by lender):<br><br>' +
      '• PAN &amp; Aadhaar of owner / directors<br>' +
      '• Business proof — GST, Udyam, shop licence<br>' +
      '• Last 6–12 months bank statements<br>' +
      '• Latest ITR &amp; financials (1–2 years)<br>' +
      '• For LAP — property documents<br><br>' +
      'Don\'t have everything? No problem — share what you have and we\'ll guide you.',
    chips: [
      {
        label: '📲 Apply on WhatsApp',
        url: waLink('Hi! I want to apply for a loan. Here are my details:'),
      },
      { label: 'Back to menu', intent: 'welcome' },
    ],
  },

  schemes: {
    keywords: [
      'scheme',
      'schemes',
      'subsidy',
      'subsidies',
      'government',
      'govt',
      'grant',
      'yojana',
      'sarkari',
      'incentive',
      'cgtmse',
      'pmegp',
      'clcs',
    ],
    reply:
      '🏛️ <b>Government Schemes &amp; Subsidies</b><br>' +
      'We help you discover and apply for the right schemes — Central, State and sector-specific — ' +
      'and make sure you claim the maximum benefit.<br><br>' +
      'Covers credit-guarantee (CGTMSE), capital subsidy (CLCS-TU), Digital MSME (ICT), PMEGP, ' +
      'PM Vishwakarma, the Samadhan portal for delayed payments, and many State incentives.',
    chips: [
      { label: 'Central schemes', intent: 'central' },
      { label: 'State schemes', intent: 'state' },
      { label: 'Samadhan (delayed payments)', intent: 'samadhan' },
      { label: 'Check eligibility', intent: 'scheme_apply' },
    ],
  },

  central: {
    keywords: [
      'central scheme',
      'central government',
      'cgtmse',
      'pmegp',
      'clcs-tu',
      'digital msme',
      'ict',
      'vishwakarma',
      'credit guarantee',
    ],
    reply:
      '🇮🇳 <b>Central Government schemes</b> we assist with:<br><br>' +
      '• <b>CGTMSE</b> — collateral-free credit guarantee<br>' +
      '• <b>CLCS-TU</b> — capital subsidy for technology upgradation<br>' +
      '• <b>Digital MSME (ICT)</b> — go digital with IT tools &amp; subsidy<br>' +
      '• <b>PMEGP</b> — margin-money subsidy for new units<br>' +
      '• <b>PM Vishwakarma</b> — support for artisans &amp; craftspeople<br>' +
      '• <b>Procurement &amp; Marketing Support</b> — trade-fair reimbursement',
    chips: [
      { label: 'Check my eligibility', intent: 'scheme_apply' },
      { label: 'State schemes', intent: 'state' },
      { label: 'Back to menu', intent: 'welcome' },
    ],
  },

  state: {
    keywords: [
      'state scheme',
      'state government',
      'state subsidy',
      'maharashtra',
      'gujarat',
      'karnataka',
      'district industries',
      'margin money',
      'erp subsidy',
    ],
    reply:
      '🗺️ <b>State Government schemes</b><br>' +
      'Every state offers its own MSME incentives — margin-money / seed-capital support, ' +
      'market development assistance, quality-certification (ZED) support, ERP / ICT subsidy ' +
      'and exhibition support.<br><br>' +
      'Tell us your <b>state</b> and <b>industry</b> and we\'ll shortlist what applies to you.',
    chips: [
      { label: 'Check my eligibility', intent: 'scheme_apply' },
      { label: 'Central schemes', intent: 'central' },
      { label: 'Back to menu', intent: 'welcome' },
    ],
  },

  samadhan: {
    keywords: [
      'samadhan',
      'delayed payment',
      'payment pending',
      'not paid',
      'stuck payment',
      'outstanding',
      'recovery',
    ],
    reply:
      '⚖️ <b>MSME Samadhan</b><br>' +
      'Stuck with delayed payments from buyers? The Ministry of MSME\'s Samadhan portal helps ' +
      'registered MSMEs recover dues. We guide you through filing and follow-up so you actually ' +
      'get paid.',
    chips: [
      {
        label: '📲 Get help on WhatsApp',
        url: waLink('Hi! I need help with delayed payments / MSME Samadhan.'),
      },
      { label: 'Back to menu', intent: 'welcome' },
    ],
  },

  scheme_apply: {
    keywords: ['scheme eligibility', 'apply scheme', 'avail subsidy', 'claim subsidy', 'how to get subsidy'],
    reply:
      'We\'ll match you to schemes you\'re actually eligible for — no guesswork. ✅<br><br>' +
      'Just share your <b>state</b>, <b>industry / activity</b> and <b>business type</b> ' +
      '(manufacturing or services), and our team takes it from there.',
    chips: [
      {
        label: '📲 Share details on WhatsApp',
        url: waLink(
          'Hi! Please check which government schemes / subsidies I\'m eligible for. My state & industry are:',
        ),
      },
      { label: 'Back to menu', intent: 'welcome' },
    ],
  },

  membership: {
    keywords: [
      'membership',
      'member',
      'join',
      'plan',
      'plans',
      'tier',
      'tiers',
      'subscribe',
      'subscription',
      'pricing',
      'price',
      'cost',
      'benefit',
    ],
    reply:
      '🪪 <b>MSME Bharat Manch Membership</b><br>' +
      'Membership unlocks our full ecosystem — credibility for your business, scheme discovery, ' +
      'GeM portal access, financial tools, networking and expert support.<br><br>' +
      'We offer four tiers — <b>Basic, Standard, Premium and Pro</b> — so you can start small ' +
      'and grow.',
    chips: [
      { label: 'Membership benefits', intent: 'membership_benefits' },
      { label: 'How to join', intent: 'join' },
      { label: 'Business services', intent: 'services' },
    ],
  },

  membership_benefits: {
    keywords: ['membership benefit', 'what do i get', 'perks', 'advantages', 'included'],
    reply:
      '✨ <b>What members get</b><br><br>' +
      '• Business <b>credibility</b> &amp; verified profile<br>' +
      '• <b>Scheme &amp; subsidy discovery</b> tailored to you<br>' +
      '• <b>GeM portal</b> registration &amp; access<br>' +
      '• Access to <b>Virtual CEO / CFO / CHRO / CTO / CMO</b> services<br>' +
      '• <b>Networking</b> across pan-India business forums<br>' +
      '• <b>Trade fairs &amp; exhibitions</b> with up to 80% reimbursement<br>' +
      '• Branding via our magazine, video features &amp; broadcasts<br>' +
      '• Free loan, insurance &amp; business-health audits',
    chips: [
      { label: 'How to join', intent: 'join' },
      { label: 'Back to menu', intent: 'welcome' },
    ],
  },

  join: {
    keywords: ['how to join', 'sign up', 'register', 'registration', 'become member', 'enroll', 'create account'],
    reply:
      'Becoming a member takes a few minutes. 🚀<br><br>' +
      'Register online and our team will help you pick the right tier and get you onboarded.',
    chips: [
      { label: '📝 Register now', url: MBM_CHAT_CONFIG.registerUrl },
      {
        label: '📲 Ask on WhatsApp',
        url: waLink('Hi! I\'d like to become a member of MSME Bharat Manch. Please guide me.'),
      },
      { label: 'Back to menu', intent: 'welcome' },
    ],
  },

  services: {
    keywords: [
      'service',
      'services',
      'business service',
      '360',
      'solution',
      'solutions',
      'help me grow',
      'what do you offer',
      'offerings',
    ],
    reply: '🧰 <b>360° Business Solutions</b><br>' + 'Beyond finance, we support your whole business journey:',
    chips: [
      { label: 'Company formation', intent: 'company' },
      { label: 'Digital marketing', intent: 'marketing' },
      { label: 'IPO / SME listing', intent: 'ipo' },
      { label: 'Technology & GeM', intent: 'tech' },
      { label: 'HR & hiring', intent: 'hr' },
      { label: 'Networking & forum', intent: 'network' },
    ],
  },

  company: {
    keywords: [
      'company formation',
      'register company',
      'incorporation',
      'pvt ltd',
      'llp',
      'proprietorship',
      'gst registration',
      'udyam',
      'start business',
      'new business',
    ],
    reply:
      '🏢 <b>Company Formation &amp; Registrations</b><br>' +
      'We set up and register your business end-to-end — Proprietorship, Partnership, LLP, ' +
      'Private Limited — plus GST, Udyam (MSME) and other licences. Start right, stay compliant.',
    chips: [
      {
        label: '📲 Get started',
        url: waLink('Hi! I need help with company formation / business registration.'),
      },
      { label: 'Back to services', intent: 'services' },
    ],
  },

  marketing: {
    keywords: [
      'marketing',
      'digital marketing',
      'social media',
      'website',
      'branding',
      'leads',
      'seo',
      'ads',
      'promotion',
    ],
    reply:
      '📣 <b>Digital Marketing &amp; Branding</b><br>' +
      'Build your brand and bring in customers — website, social media, lead generation, ' +
      'and member features in our magazine and broadcasts that reach a huge MSME network.',
    chips: [
      {
        label: '📲 Talk to us',
        url: waLink('Hi! I\'m interested in digital marketing / branding support.'),
      },
      { label: 'Back to services', intent: 'services' },
    ],
  },

  ipo: {
    keywords: [
      'ipo',
      'listing',
      'sme ipo',
      'stock exchange',
      'go public',
      'raise equity',
      'share market listing',
    ],
    reply:
      '📈 <b>IPO / SME Listing</b><br>' +
      'Ready to scale big? We guide eligible MSMEs through SME-exchange listing — readiness, ' +
      'advisory and the path to raising public capital.',
    chips: [
      {
        label: '📲 Explore IPO listing',
        url: waLink('Hi! I\'d like to know about SME IPO / listing support.'),
      },
      { label: 'Back to services', intent: 'services' },
    ],
  },

  tech: {
    keywords: ['technology', 'tech', 'software', 'gem', 'gem portal', 'digital', 'automation', 'it tools', 'app'],
    reply:
      '💻 <b>Technology &amp; GeM</b><br>' +
      'Go digital with the right tools, and get onto <b>GeM (Government e-Marketplace)</b> so you ' +
      'can sell to government buyers. We handle registration, catalogue setup and guidance.',
    chips: [
      {
        label: '📲 Get GeM help',
        url: waLink('Hi! I need help with GeM portal registration / technology support.'),
      },
      { label: 'Back to services', intent: 'services' },
    ],
  },

  hr: {
    keywords: ['hr', 'human resource', 'hiring', 'recruit', 'staff', 'employee', 'manpower', 'talent', 'labour'],
    reply:
      '👥 <b>Human Resources</b><br>' +
      'Employees implement your vision. We help you acquire, develop and retain the right people, ' +
      'plus labour-advisory support for your business.',
    chips: [
      { label: '📲 Talk to us', url: waLink('Hi! I need HR / hiring support for my business.') },
      { label: 'Back to services', intent: 'services' },
    ],
  },

  network: {
    keywords: ['network', 'networking', 'forum', 'discussion', 'community', 'connect', 'leadership league', 'events', 'webinar'],
    reply:
      '🤝 <b>Networking, Forum &amp; Events</b><br>' +
      'Join our discussion forum and connect across pan-India business networks. Showcase your ' +
      'story through the Business Leadership League, and join our webinars, seminars and summits.',
    chips: [
      {
        label: '📲 Join the network',
        url: waLink('Hi! I\'d like to join the MSME Bharat Manch network / forum.'),
      },
      { label: 'Back to services', intent: 'services' },
    ],
  },

  free: {
    keywords: ['free', 'audit', 'free service', 'free audit', 'insurance audit', 'business scan', 'health check', 'review', 'no cost'],
    reply:
      '🎁 <b>Free services for your business</b><br><br>' +
      '• <b>Free Loan Audit</b> — find savings &amp; fix issues in your existing loans<br>' +
      '• <b>Free Insurance Audit</b> — right cover at the best rates<br>' +
      '• <b>Free Business Scan Report</b> — clear view of your strengths &amp; gaps<br><br>' +
      'No cost, no obligation. Want one?',
    chips: [
      {
        label: '📲 Get my free audit',
        url: waLink('Hi! I\'d like a free Loan / Insurance / Business Scan audit.'),
      },
      { label: 'Back to menu', intent: 'welcome' },
    ],
  },

  about: {
    keywords: ['about', 'who are you', 'what is msme bharat manch', 'mbm', 'rupeeboss', 'mission', 'vision', 'about you'],
    reply:
      '<b>' +
      MBM_CHAT_CONFIG.brand +
      '</b> is a national initiative powered by RupeeBoss.com — a ' +
      '<b>one-stop platform to empower Indian MSMEs</b> and help them become self-reliant.<br><br>' +
      'Our mission spans finance, government schemes, marketing, consulting, HR, technology and ' +
      'more — supporting entrepreneurs from the first mile to large-enterprise scale.',
    chips: CHIPS_MAIN,
  },

  contact: {
    keywords: ['contact', 'talk', 'human', 'call', 'phone', 'whatsapp', 'reach', 'support', 'email', 'help'],
    reply:
      '📞 <b>We\'re here to help.</b><br><br>' +
      'The fastest way to reach a real person is WhatsApp — tap below and our team will assist you.',
    chips: [
      { label: '💬 Chat on WhatsApp', url: waLink('Hi MSME Bharat Manch team! I have a question.') },
      { label: '📝 Register', url: MBM_CHAT_CONFIG.registerUrl },
    ],
  },

  thanks: {
    keywords: ['thank', 'thanks', 'thank you', 'great', 'awesome', 'good', 'nice', 'helpful', 'dhanyavad', 'shukriya'],
    reply:
      'You\'re most welcome! 🙏 Wishing your business great success. ' +
      'Is there anything else I can help you with?',
    chips: CHIPS_MAIN,
  },

  bye: {
    keywords: ['bye', 'goodbye', 'see you', 'that\'s all', 'nothing', 'no thanks', 'exit'],
    reply:
      'Thank you for visiting <b>' +
      MBM_CHAT_CONFIG.brand +
      '</b>! 🙏 ' +
      'Come back anytime — and reach us on WhatsApp whenever you need us.',
    chips: [
      { label: '💬 WhatsApp us', url: waLink('Hi! I have a question for MSME Bharat Manch.') },
      { label: 'Back to menu', intent: 'welcome' },
    ],
  },

  fallback: {
    reply:
      'I want to make sure I help you correctly. 🙂 I can assist with any of these — ' +
      'or tap <b>Talk to us</b> to reach a person on WhatsApp:',
    chips: CHIPS_MAIN,
  },
};

export function normalize(text: string): string {
  return ' ' + text.toLowerCase().replace(/[^a-z0-9\s]/g, ' ').replace(/\s+/g, ' ') + ' ';
}

export function matchIntent(text: string): string {
  const t = normalize(text);
  const trimmed = text.toLowerCase().trim();
  if (GREETINGS.includes(trimmed)) return 'welcome';

  let best: string | null = null;
  let bestScore = 0;

  for (const key of Object.keys(KB)) {
    const intent = KB[key];
    if (!intent.keywords) continue;
    let score = 0;
    for (const kw of intent.keywords) {
      if (t.indexOf(' ' + kw + ' ') !== -1 || t.indexOf(' ' + kw) !== -1) {
        score += kw.indexOf(' ') !== -1 ? 3 : 1 + kw.length * 0.02;
      }
    }
    if (score > bestScore) {
      bestScore = score;
      best = key;
    }
  }

  return bestScore > 0 ? best! : 'fallback';
}
