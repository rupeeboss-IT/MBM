export type OfferingSlug =
  | 'whatsapp-platform'
  | 'basic-website'
  | 'practo-insurance'
  | 'credit-report'
  | 'bank-statement-analyzer'
  | 'relationship-manager'
  | 'loan-audit'
  | 'insurance-audit'
  | 'scheme-discovery'
  | 'gem-registration'
  | 'trust-score'
  | 'msme-events';

export interface OfferingModel {
  title: string;
  crumb: string;
  icon: string;
  gradient: string;
  tagline: string;
  tags: string[];
  plans: string[];
  highlights: string[];
  ctaTitle: string;
  ctaDesc: string;
  content: string; // HTML string (rendered in template)
}

export const OFFERINGS_DATA: Record<OfferingSlug, OfferingModel> = {
  'whatsapp-platform': {
    title: 'WhatsApp Campaign Platform',
    crumb: 'WhatsApp Platform',
    icon: '💬',
    gradient: 'linear-gradient(135deg,#25d366,#128c7e)',
    tagline: 'Reach your customers directly where they are — on WhatsApp',
    tags: ['Digital Marketing', 'Lead Generation', 'Customer Engagement', 'By MSME Bharat Manch'],
    plans: ['MSME Basic', 'MSME Standard', 'MSME Premium', 'MSME Pro'],
    highlights: [
      '📨 High open rates vs email',
      '💸 Cost-effective marketing',
      '⚡ Real-time customer chat',
      '📊 Campaign analytics dashboard'
    ],
    ctaTitle: 'Start WhatsApp Marketing',
    ctaDesc: 'Available across all MSME Bharat Manch plans',
    content: `
      <h2 style="color:#25d366">What is the WhatsApp Campaign Platform?</h2>
      <p>The <strong>WhatsApp Campaign Platform by MSME Bharat Manch</strong> is a powerful in-house digital marketing solution built specifically to help MSMEs promote their products and services directly to customers through WhatsApp. With 500+ million Indians using WhatsApp daily, it is one of the highest-engagement marketing channels available.</p>
      <p>Our platform empowers small and medium businesses to run professional WhatsApp campaigns — sending product updates, promotions, offers, and business messages directly to customers — without requiring any technical expertise.</p>
      <h2 style="color:#25d366">Why WhatsApp Marketing for MSMEs?</h2>
      <div class="offering-benefit-grid">
        <div class="offering-benefit"><div class="ob-icon" style="background:#e8f5e9">📨</div><div class="ob-text"><strong>High Open Rates</strong><span>WhatsApp messages get 5–7× more opens than email marketing campaigns</span></div></div>
        <div class="offering-benefit"><div class="ob-icon" style="background:#e8f0fe">💸</div><div class="ob-text"><strong>Cost-Effective</strong><span>Far more affordable than traditional advertising channels like print or TV</span></div></div>
        <div class="offering-benefit"><div class="ob-icon" style="background:#fff3e0">⚡</div><div class="ob-text"><strong>Instant Reach</strong><span>Messages delivered instantly — no spam filters, no delays</span></div></div>
        <div class="offering-benefit"><div class="ob-icon" style="background:#fff5f5">🤝</div><div class="ob-text"><strong>Build Trust</strong><span>Personalized communication builds stronger customer relationships</span></div></div>
        <div class="offering-benefit"><div class="ob-icon" style="background:#e8f5e9">🎯</div><div class="ob-text"><strong>Direct Customer Reach</strong><span>Send promotions, offers, and updates straight to customer phones</span></div></div>
        <div class="offering-benefit"><div class="ob-icon" style="background:#e8f0fe">📊</div><div class="ob-text"><strong>Campaign Analytics</strong><span>Track message delivery, open rates, and customer responses in real time</span></div></div>
      </div>
      <h2 style="color:#25d366">How It Works — 4 Simple Steps</h2>
      <div class="offering-steps">
        <div class="offering-step"><div class="offering-step-num">1</div><div class="offering-step-body"><strong>Create a Meta Business Account</strong><p>Set up your Meta (Facebook) Business Account — required for the official WhatsApp Business API.</p></div></div>
        <div class="offering-step"><div class="offering-step-num">2</div><div class="offering-step-body"><strong>Connect Your Business Number</strong><p>Link your official business mobile number to the WhatsApp platform for verified business identity.</p></div></div>
        <div class="offering-step"><div class="offering-step-num">3</div><div class="offering-step-body"><strong>Access Your Campaign Dashboard</strong><p>Get full access to the MSME Bharat Manch WhatsApp Campaign Dashboard to manage all marketing activities.</p></div></div>
        <div class="offering-step"><div class="offering-step-num">4</div><div class="offering-step-body"><strong>Launch Your Campaign</strong><p>Upload customer contacts, create message templates with images/offers, schedule campaigns, and track results — all in one place.</p></div></div>
      </div>
      <h2 style="color:#25d366">Platform Features</h2>
      <ul class="offering-checklist">
        <li>Bulk WhatsApp message sending to customer contact lists</li>
        <li>Rich media support — images, PDFs, product catalogues</li>
        <li>Message template creation and approval management</li>
        <li>Campaign scheduling — send at the right time</li>
        <li>Automated FAQ replies and basic routing</li>
        <li>WhatsApp + Call button integration on your website</li>
        <li>Delivery and read receipt tracking</li>
        <li>Customer opt-in/opt-out management (TRAI compliant)</li>
      </ul>`
  },
  'basic-website': {
    title: 'Business Website Development',
    crumb: 'Basic Website',
    icon: '🌐',
    gradient: 'linear-gradient(135deg,#0f3460,#1a56db)',
    tagline: 'A professional website for your MSME — built, hosted, and maintained for you',
    tags: ['Website', 'Hosting Included', 'qobo.dev subdomain', 'Mobile-Friendly'],
    plans: ['MSME Basic', 'MSME Standard', 'MSME Premium', 'MSME Pro'],
    highlights: ['🌐 Free subdomain on qobo.dev', '📱 Mobile-friendly design', '⚡ Fast loading speed', '🔒 SSL + Secure hosting'],
    ctaTitle: 'Get Your Website',
    ctaDesc: 'Basic site in Basic plan; full development in Standard+',
    content: `
      <h2 style="color:#1a56db">Your MSME Deserves a Professional Online Presence</h2>
      <p>In today's digital world, a website is your most important business asset. Customers Google you before they call you. MSME Bharat Manch provides complete website development — design, development, hosting, and ongoing maintenance — so you focus on your business while we handle your digital presence.</p>
      <h2 style="color:#1a56db">What's Included</h2>
      <div class="offering-benefit-grid">
        <div class="offering-benefit"><div class="ob-icon" style="background:#e8f0fe">🎨</div><div class="ob-text"><strong>Professional Design</strong><span>Business/Service/Portfolio/E-commerce — designed for your industry</span></div></div>
        <div class="offering-benefit"><div class="ob-icon" style="background:#e8f5e9">🌐</div><div class="ob-text"><strong>Free Subdomain</strong><span>Your business at yourname.qobo.dev — free domain mapping available</span></div></div>
        <div class="offering-benefit"><div class="ob-icon" style="background:#fff3e0">🖥️</div><div class="ob-text"><strong>Hosting + Server + DB</strong><span>Complete hosting infrastructure included in your annual plan</span></div></div>
        <div class="offering-benefit"><div class="ob-icon" style="background:#fff5f5">📱</div><div class="ob-text"><strong>Mobile-Friendly</strong><span>Responsive design that works perfectly on all devices</span></div></div>
        <div class="offering-benefit"><div class="ob-icon" style="background:#e8f5e9">💬</div><div class="ob-text"><strong>WhatsApp + Call Button</strong><span>Customers can reach you directly from your website</span></div></div>
        <div class="offering-benefit"><div class="ob-icon" style="background:#e8f0fe">⚙️</div><div class="ob-text"><strong>Setup + Yearly Maintenance</strong><span>One-time setup plus annual hosting and maintenance included</span></div></div>
      </div>
      <h2 style="color:#1a56db">Website Types Available</h2>
      <ul class="offering-checklist">
        <li><strong>Business/Company Website</strong> — About, services, contact, team page</li>
        <li><strong>Service Website</strong> — Service listings, enquiry forms, booking</li>
        <li><strong>Portfolio Website</strong> — Showcase your work and projects</li>
        <li><strong>E-commerce Website</strong> — Product catalogue, cart, payment integration</li>
      </ul>
      <h2 style="color:#1a56db">Standard Plan: Advanced Website Features</h2>
      <ul class="offering-checklist">
        <li>FAQ automation — answer common questions automatically</li>
        <li>Lead capture forms integrated with WhatsApp notifications</li>
        <li>Domain mapping — connect your own domain (e.g. www.yourbusiness.com)</li>
        <li>Basic routing and notification workflows</li>
        <li>Fast loading speed — optimized for Indian internet speeds</li>
        <li>SSL security certificate included</li>
      </ul>`
  },
  'practo-insurance': {
    title: 'Practo Health + Protection Plan',
    crumb: 'Practo Insurance',
    icon: '🛡️',
    gradient: 'linear-gradient(135deg,#2d6a4f,#52b788)',
    tagline: 'Comprehensive healthcare and financial protection for your family — powered by Practo',
    tags: ['Health Insurance', 'Accident Cover', 'Practo Network', 'Family Protection'],
    plans: ['MSME Basic', 'MSME Standard'],
    highlights: [
      '👨‍👩‍👧 Covers 2 adults + 1 child',
      '💊 Unlimited doctor consults',
      '🏥 Hospitalization cash support',
      '🔬 20% off diagnostics'
    ],
    ctaTitle: 'Get Covered Now',
    ctaDesc: 'Included free with MSME Basic & Standard plans',
    content: `
      <h2 style="color:#2d6a4f">Comprehensive Healthcare for You & Your Family</h2>
      <p>The <strong>Practo Health + Protection Plan</strong> gives MSME members and their families access to quality healthcare combined with accident and hospitalization financial protection — all at a highly subsidized rate through MSME Bharat Manch.</p>
      <h2 style="color:#2d6a4f">Plan Options</h2>
      <div style="display:grid;grid-template-columns:1fr 1fr;gap:1rem;margin-bottom:1.5rem">
        <div style="background:#e8f5e9;border:2px solid #c8e6c9;border-radius:12px;padding:1.3rem">
          <div style="font-weight:800;color:#2d6a4f;font-size:1rem;margin-bottom:.3rem">🟦 Combo 1 — ₹10 Lakh Cover</div>
          <div style="font-size:.82rem;color:#555;margin-bottom:.8rem">Premium: ₹710 + GST/year</div>
          <ul style="font-size:.83rem;color:#444;padding-left:1rem">
            <li>Unlimited doctor consultations</li><li>1 free in-person OPD/year</li><li>Accidental Death: ₹10 Lakh</li><li>Hospital Cash: ₹500/day</li><li>ICU Cash: ₹1,000/day</li>
          </ul>
        </div>
        <div style="background:#e8f5e9;border:2px solid #2d6a4f;border-radius:12px;padding:1.3rem;position:relative">
          <div style="position:absolute;top:-10px;left:50%;transform:translateX(-50%);background:#2d6a4f;color:#fff;font-size:.72rem;font-weight:700;padding:.2rem .8rem;border-radius:20px;white-space:nowrap">Included in Premium/Pro</div>
          <div style="font-weight:800;color:#2d6a4f;font-size:1rem;margin-bottom:.3rem">🟩 Combo 4 — ₹15 Lakh Cover</div>
          <div style="font-size:.82rem;color:#555;margin-bottom:.8rem">Premium: ₹1,020 + GST/year</div>
          <ul style="font-size:.83rem;color:#444;padding-left:1rem">
            <li>Unlimited doctor consultations</li><li>1 free in-person OPD/year</li><li>Accidental Death: ₹15 Lakh</li><li>Hospital Cash: ₹500/day</li><li>ICU Cash: ₹1,000/day</li>
          </ul>
        </div>
      </div>
      <h2 style="color:#2d6a4f">What You Get — Health Coverage</h2>
      <ul class="offering-checklist">
        <li>Covers <strong>up to 2 adults + 1 child</strong> per family</li>
        <li><strong>Unlimited doctor consultations</strong> via chat, audio, and video</li>
        <li>1 free <strong>in-person OPD consultation</strong> per year at Practo cashless network</li>
        <li><strong>20% discount</strong> on all diagnostic tests</li>
        <li><strong>5% discount</strong> on medicines</li>
      </ul>
      <h2 style="color:#2d6a4f">What You Get — Accident & Hospitalization Cover</h2>
      <ul class="offering-checklist">
        <li>Accidental Death Benefit: ₹10 Lakh (Basic/Standard) or ₹15 Lakh (Premium/Pro)</li>
        <li>Permanent Partial Disability (PPD) coverage</li>
        <li>Permanent Total Disability (PTD) coverage</li>
        <li>Daily Hospital Cash: ₹500/day for normal hospitalization</li>
        <li>ICU Daily Cash: ₹1,000/day — coverage up to 30 days per year</li>
        <li>Entry age: 18 to 65 years</li>
      </ul>`
  },
  'credit-report': {
    title: 'Free Credit Report (CIBIL)',
    crumb: 'Free Credit Report',
    icon: '📊',
    gradient: 'linear-gradient(135deg,#e63946,#c1121f)',
    tagline: 'Know your CIBIL score and full credit history — free with every MSME Bharat Manch plan',
    tags: ['CIBIL Score', 'Credit Health', 'Financial Insights', 'Free with All Plans'],
    plans: ['MSME Basic', 'MSME Standard', 'MSME Premium', 'MSME Pro'],
    highlights: ['📊 Instant credit report access', '🔄 Monthly updates included', '💳 Full account history', '🔍 Detailed credit analysis'],
    ctaTitle: 'Check Your Credit Score',
    ctaDesc: 'Free with every plan — check yours today',
    content: `
      <h2 style="color:var(--red)">What is a Credit Report (CIBIL)?</h2>
      <p>Your <strong>CIBIL report</strong> is a detailed record of your entire credit history. It includes information about all your loans, credit cards, repayment behavior, and overall creditworthiness. It also contains your <strong>credit score</strong> — a 3-digit number (300–900) that lenders use to decide whether to approve your loan application and at what interest rate.</p>
      <p>A score of <strong>750 or above</strong> is considered excellent and gives you the best loan approvals and lowest interest rates.</p>
      <h2 style="color:var(--red)">Why Your Credit Score Matters for MSMEs</h2>
      <div class="offering-benefit-grid">
        <div class="offering-benefit"><div class="ob-icon" style="background:#fff5f5">✅</div><div class="ob-text"><strong>Loan Approvals</strong><span>Higher score = higher chances of getting business loans approved</span></div></div>
        <div class="offering-benefit"><div class="ob-icon" style="background:#e8f5e9">💰</div><div class="ob-text"><strong>Lower Interest Rates</strong><span>Better scores unlock significantly lower interest rates — saving lakhs</span></div></div>
        <div class="offering-benefit"><div class="ob-icon" style="background:#e8f0fe">💳</div><div class="ob-text"><strong>Credit Card Eligibility</strong><span>Access to premium business credit cards with higher limits</span></div></div>
        <div class="offering-benefit"><div class="ob-icon" style="background:#fff3e0">⚡</div><div class="ob-text"><strong>Faster Processing</strong><span>Reduced documentation requirements and quicker loan disbursals</span></div></div>
      </div>
      <h2 style="color:var(--red)">What's in Your Credit Report</h2>
      <ul class="offering-checklist">
        <li><strong>Personal Information</strong> — Name, DOB, PAN details, contact information</li>
        <li><strong>Credit Accounts</strong> — All credit cards, personal loans, home loans, business loans</li>
        <li><strong>Account Status</strong> — Active, closed, or defaulted accounts</li>
        <li><strong>Repayment History</strong> — On-time payments, delays, and missed EMIs</li>
        <li><strong>Credit Inquiries</strong> — All instances where lenders have pulled your report</li>
        <li><strong>Credit Utilization</strong> — How much of your available credit you're using</li>
        <li><strong>Overdue Amounts</strong> — Any outstanding amounts flagged as overdue</li>
      </ul>
      <h2 style="color:var(--red)">What MSME Bharat Manch Provides</h2>
      <ul class="offering-checklist">
        <li>✅ Instant credit report access — get your report within minutes</li>
        <li>✅ Accurate, up-to-date CIBIL score</li>
        <li>✅ Detailed credit analysis with easy-to-read summaries</li>
        <li>✅ Monthly updates to track changes in your score</li>
        <li>✅ User-friendly dashboard with clear insights</li>
        <li>✅ Tips to improve your credit score over time</li>
      </ul>`
  },
  'bank-statement-analyzer': {
    title: 'Bank Statement Analyzer',
    crumb: 'Bank Statement Analyzer',
    icon: '🏦',
    gradient: 'linear-gradient(135deg,#1a56db,#0f3460)',
    tagline: 'Transform raw bank statements into powerful financial insights for faster loan approvals',
    tags: ['Financial Analysis', 'Loan Readiness', 'Cash Flow Insights', 'Smart Analytics'],
    plans: ['MSME Premium', 'MSME Pro'],
    highlights: ['⚡ Automated analysis', '💰 Faster loan processing', '📈 Cash flow visibility', '🎯 Loan readiness score'],
    ctaTitle: 'Analyze Your Statements',
    ctaDesc: 'Available in MSME Premium & Pro plans',
    content: `
      <h2 style="color:#1a56db">Smart Financial Insights for Your MSME</h2>
      <p>The <strong>Bank Statement Analyzer</strong> by MSME Bharat Manch is an intelligent tool that automatically reads and analyzes your bank statements to generate meaningful financial reports and insights. What used to take weeks of manual work now happens in minutes.</p>
      <p>For MSMEs applying for loans, having organized and analyzed financial data can be the difference between approval and rejection. Our analyzer converts your raw transaction data into structured reports that banks and NBFCs trust.</p>
      <h2 style="color:#1a56db">How This Helps Your MSME</h2>
      <div class="offering-benefit-grid">
        <div class="offering-benefit"><div class="ob-icon" style="background:#e8f0fe">⚡</div><div class="ob-text"><strong>Faster Loan Processing</strong><span>Structured financial data helps lenders evaluate your application faster and more accurately</span></div></div>
        <div class="offering-benefit"><div class="ob-icon" style="background:#e8f5e9">📈</div><div class="ob-text"><strong>Cash Flow Visibility</strong><span>Understand monthly inflows and outflows to manage working capital effectively</span></div></div>
        <div class="offering-benefit"><div class="ob-icon" style="background:#fff3e0">🎯</div><div class="ob-text"><strong>Better Financial Planning</strong><span>Analyze spending patterns and income trends to make smarter business decisions</span></div></div>
        <div class="offering-benefit"><div class="ob-icon" style="background:#fff5f5">🏦</div><div class="ob-text"><strong>Improved Credit Access</strong><span>Even businesses without formal financial statements can demonstrate financial health</span></div></div>
      </div>
      <h2 style="color:#1a56db">What the Analyzer Detects</h2>
      <ul class="offering-checklist">
        <li>Total monthly credits (income/revenue) and debits (expenses)</li>
        <li>Average monthly balance and balance trends over 12 months</li>
        <li>EMI and loan repayment identification</li>
        <li>Salary credits, business income, and irregular income patterns</li>
        <li>Bounce/return transactions — flags for lenders</li>
        <li>Cash withdrawal patterns and high-value transactions</li>
        <li>Business revenue cycles and seasonality detection</li>
        <li>Debt service coverage ratio (DSCR) calculation</li>
      </ul>
      <h2 style="color:#1a56db">Output Report Includes</h2>
      <ul class="offering-checklist">
        <li>Automated financial summary dashboard</li>
        <li>Monthly cash flow chart (inflows vs outflows)</li>
        <li>Risk indicators and financial health score</li>
        <li>Loan eligibility estimate based on cash flows</li>
        <li>Lender-ready formatted report for bank submissions</li>
      </ul>`
  },
  'relationship-manager': {
    title: 'Free Relationship Manager',
    crumb: 'Relationship Manager',
    icon: '👔',
    gradient: 'linear-gradient(135deg,#6d28d9,#4c1d95)',
    tagline: 'Your dedicated personal financial expert — one call away for all your MSME needs',
    tags: ['Personal RM', 'Financial Guidance', 'Free Service', 'By Rupeeboss'],
    plans: ['MSME Premium', 'MSME Pro'],
    highlights: ['👤 Dedicated personal RM', '📞 Single point of contact', '🏦 Access to 100+ lenders', '⚡ Faster loan approvals'],
    ctaTitle: 'Get Your Personal RM',
    ctaDesc: 'Dedicated RM assigned with Premium & Pro plans',
    content: `
      <h2 style="color:#6d28d9">What is a Free Relationship Manager?</h2>
      <p>A <strong>Free Relationship Manager (RM) from Rupeeboss</strong> is a dedicated financial expert assigned personally to you. Instead of navigating loans, insurance, and government schemes on your own, you get one trusted expert who handles everything — completely free of charge.</p>
      <p>Managing multiple financial products across different banks and institutions can be time-consuming and complex. Your RM simplifies this by becoming your single point of contact for all financial matters.</p>
      <h2 style="color:#6d28d9">What Your RM Does for You</h2>
      <div class="offering-benefit-grid">
        <div class="offering-benefit"><div class="ob-icon" style="background:#ede9fe">🎯</div><div class="ob-text"><strong>Personalized Financial Guidance</strong><span>Your RM understands your business goals and recommends the right loans, insurance, and investments</span></div></div>
        <div class="offering-benefit"><div class="ob-icon" style="background:#e8f5e9">📞</div><div class="ob-text"><strong>Single Point of Contact</strong><span>One dedicated expert handles all your financial queries — no more running between banks</span></div></div>
        <div class="offering-benefit"><div class="ob-icon" style="background:#fff3e0">⚡</div><div class="ob-text"><strong>Faster Loan Processing</strong><span>RM coordinates directly with lenders to speed up approvals and documentation</span></div></div>
        <div class="offering-benefit"><div class="ob-icon" style="background:#fff5f5">🏦</div><div class="ob-text"><strong>Multi-Lender Access</strong><span>Compare products from 100+ banks, NBFCs and insurance providers to get the best deal</span></div></div>
      </div>
      <h2 style="color:#6d28d9">Services Your RM Assists With</h2>
      <ul class="offering-checklist">
        <li><strong>Loan Assistance</strong> — Home loans, business loans, working capital, machinery finance</li>
        <li><strong>Insurance Guidance</strong> — Right policy selection, coverage optimization, renewal support</li>
        <li><strong>Government Schemes</strong> — Identify applicable schemes and guide through applications</li>
        <li><strong>Financial Portfolio Review</strong> — Track loans, EMIs, insurance, and investments in one view</li>
        <li><strong>Documentation Support</strong> — Application submissions, bank coordination, status updates</li>
        <li><strong>Credit Score Improvement</strong> — Action plan to improve CIBIL score for better loan rates</li>
      </ul>
      <h2 style="color:#6d28d9">How It Works</h2>
      <div class="offering-steps">
        <div class="offering-step"><div class="offering-step-num">1</div><div class="offering-step-body"><strong>Plan Activation</strong><p>Activate your MSME Premium or Pro plan and your RM will be assigned within 24 hours.</p></div></div>
        <div class="offering-step"><div class="offering-step-num">2</div><div class="offering-step-body"><strong>Initial Consultation</strong><p>Your RM calls you to understand your financial goals, current situation, and immediate requirements.</p></div></div>
        <div class="offering-step"><div class="offering-step-num">3</div><div class="offering-step-body"><strong>Personalized Plan</strong><p>RM evaluates your profile and presents tailored recommendations for loans, insurance, and schemes.</p></div></div>
        <div class="offering-step"><div class="offering-step-num">4</div><div class="offering-step-body"><strong>Ongoing Support</strong><p>Your RM remains available for continuous assistance, follow-ups, and portfolio management.</p></div></div>
      </div>`
  },
  'loan-audit': {
    title: 'Free Loan Audit',
    crumb: 'Free Loan Audit',
    icon: '🔍',
    gradient: 'linear-gradient(135deg,#e65100,#bf360c)',
    tagline: 'Discover hidden savings in your existing loans — reduce EMIs, lower interest, improve cash flow',
    tags: ['Loan Optimization', 'EMI Reduction', 'Balance Transfer', 'By Rupeeboss'],
    plans: ['MSME Premium', 'MSME Pro'],
    highlights: ['💰 Identify interest savings', '📉 Reduce EMI burden', '🔄 Balance transfer support', '📋 Free detailed audit report'],
    ctaTitle: 'Start Your Loan Audit',
    ctaDesc: 'Free with MSME Premium & Pro plans',
    content:
      '<h2 style="color:#e65100">What is a Free Loan Audit?</h2><p>A <strong>Free Loan Audit by Rupeeboss</strong> is a professional financial review of all your existing loans. Many businesses continue paying high interest rates simply because they don\'t know better options are available. Our Loan Audit finds those opportunities and shows you exactly how much you can save.</p>'
  },
  'insurance-audit': {
    title: 'Free Insurance Audit',
    crumb: 'Free Insurance Audit',
    icon: '🛡️',
    gradient: 'linear-gradient(135deg,#0277bd,#01579b)',
    tagline: 'Ensure you have the right coverage at the right price — eliminate gaps, reduce premiums',
    tags: ['Insurance Review', 'Premium Optimization', 'Coverage Gaps', 'By Rupeeboss'],
    plans: ['MSME Premium', 'MSME Pro'],
    highlights: ['💸 Reduce premium costs', '🎯 Fix coverage gaps', '🔄 Eliminate duplicates', '📋 Free audit report'],
    ctaTitle: 'Audit Your Insurance',
    ctaDesc: 'Free with MSME Premium & Pro plans',
    content:
      '<h2 style="color:#0277bd">What is a Free Insurance Audit?</h2><p>An <strong>Insurance Audit by Rupeeboss</strong> is a professional review of all insurance policies held by your business or family.</p>'
  },
  'scheme-discovery': {
    title: 'Company Scheme Discovery Report',
    crumb: 'Scheme Discovery',
    icon: '🏛️',
    gradient: 'linear-gradient(135deg,#2d6a4f,#1b4332)',
    tagline: 'Personalized government scheme report based on your Udyam Aadhaar — unlock benefits worth crores',
    tags: ['Government Schemes', 'Udyam Aadhaar', 'Personalized Report', 'MSME Benefits'],
    plans: ['MSME Premium', 'MSME Pro'],
    highlights: ['📋 Personalized scheme list', '🏛️ Central + State schemes', '✅ Eligibility pre-checked', '📞 Application guidance'],
    ctaTitle: 'Discover Your Schemes',
    ctaDesc: 'Personalized report with Premium & Pro plans',
    content:
      '<h2 style="color:#2d6a4f">What is the Government Schemes Discovery Report?</h2><p>MSME Bharat Manch\'s <strong>Government Schemes Discovery Report</strong> is a personalized service that analyzes your MSME\'s Udyam Aadhaar profile.</p>'
  },
  'gem-registration': {
    title: 'GeM Portal Registration & Support',
    crumb: 'GeM Registration',
    icon: '🏛️',
    gradient: 'linear-gradient(135deg,#1565c0,#0d47a1)',
    tagline: 'Sell directly to the Government of India — end-to-end GeM registration and listing management',
    tags: ['GeM Portal', 'Government Sales', 'MSME Procurement', 'Expert Assistance'],
    plans: ['MSME Pro'],
    highlights: ['🏛️ Sell to government buyers', '25% MSME procurement quota', '🚫 No EMD for MSMEs', '⚡ Timely government payments'],
    ctaTitle: 'Register on GeM',
    ctaDesc: 'Expert GeM support included with MSME Pro',
    content:
      '<h2 style="color:#1565c0">What is the GeM Portal?</h2><p>The <strong>Government e-Marketplace (GeM)</strong> is India\'s dedicated online marketplace for public procurement.</p>'
  },
  'trust-score': {
    title: 'Infomerics Trust Score & Business Verification',
    crumb: 'Trust Score',
    icon: '⭐',
    gradient: 'linear-gradient(135deg,#7b1fa2,#4a148c)',
    tagline: 'Get your business credibility verified by SEBI-registered Infomerics — build trust with banks and partners',
    tags: ['Infomerics Verified', 'SEBI Registered', 'Credit Rating Agency', 'Business Credibility'],
    plans: ['MSME Premium', 'MSME Pro'],
    highlights: ['⭐ SEBI-registered agency', '🏦 Banks trust Infomerics', '📊 Full business analysis', '🤝 Partner confidence boost'],
    ctaTitle: 'Get Verified by Infomerics',
    ctaDesc: 'Included in MSME Premium & Pro plans',
    content:
      '<h2 style="color:#7b1fa2">About Infomerics</h2><p><strong>Infomerics Analytics and Research Pvt. Ltd.</strong> is a professional analytics organization that provides business evaluation, risk assessment, grading, scoring, and certification services.</p>'
  },
  'msme-events': {
    title: 'MSME Events Access',
    crumb: 'MSME Events',
    icon: '🎟️',
    gradient: 'linear-gradient(135deg,#e63946,#c1121f)',
    tagline: 'Network with buyers, partners, and investors at premium MSME events across India',
    tags: ['Trade Fairs', 'Networking', 'Exhibitions', 'Knowledge Sessions'],
    plans: ['MSME Premium (2 events)', 'MSME Pro (4 events)'],
    highlights: ['🤝 Meet 1000+ MSMEs', '🛒 Connect with buyers', '🎓 Expert seminars', '🌍 Pan India events'],
    ctaTitle: 'Get Event Access',
    ctaDesc: '2 events (Premium) or 4 events (Pro) included',
    content:
      '<h2 style="color:var(--red)">Why MSME Events Matter</h2><p>For any MSME, <strong>visibility and the right connections</strong> are as important as the quality of your product or service.</p>'
  }
};

