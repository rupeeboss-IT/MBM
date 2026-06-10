export type ServiceSlug =
  | 'msme-loans-finance'
  | 'company-formation'
  | 'government-subsidies'
  | 'technology-solutions'
  | 'sales-digital-marketing'
  | 'ipo-investment-banking'
  | 'coaching-consulting'
  | 'legal-compliance'
  | 'research-development'
  | 'export-import'
  | 'hr-recruitment'
  | 'accounting-cfo';

export type ServiceCategory = 'Finance' | 'Legal' | 'Technology' | 'Marketing' | 'Growth';

export interface ServiceModel {
  title: string;
  subtitle: string;
  emoji: string;
  cat: ServiceCategory;
  benefits: string[];
  targets: string[];
  process: string;
  related: ServiceSlug[];
}

export const SERVICES_DATA: Record<ServiceSlug, ServiceModel> = {
  'msme-loans-finance': {
    title: 'MSME Loans & Finance',
    subtitle: 'Business loans from ₹10 Lakh to ₹10 Crore via 100+ Banks & NBFCs',
    emoji: '🪙',
    cat: 'Finance',
    benefits: [
      'Business Loan up to ₹10 Crore',
      'Working Capital Loan',
      'Machinery & Equipment Finance',
      'Unsecured Business Loans',
      'Credit Rectification',
      'Project Finance up to ₹1000 Crore',
      'Letter of Credit & Bank Guarantee',
      'Overdraft Facility'
    ],
    targets: [
      '🏭 Manufacturers',
      '🛒 Traders',
      '💼 Service Providers',
      '👩‍💼 Professionals',
      '🌾 Agri-based MSMEs',
      '🏗️ Real Estate MSMEs'
    ],
    process:
      'Our team assesses your business profile and requirements, identifies the best-fit lenders from our panel of 100+ banks and NBFCs, prepares your documentation, and provides doorstep service for the entire loan process.',
    related: ['company-formation', 'government-subsidies', 'coaching-consulting']
  },
  'company-formation': {
    title: 'Company Formation & Registration',
    subtitle: 'Complete legal structure setup from Proprietorship to Pvt Ltd Company',
    emoji: '🏢',
    cat: 'Legal',
    benefits: [
      'Private Limited Company (Pvt Ltd)',
      'Limited Liability Partnership (LLP)',
      'One Person Company (OPC)',
      'Sole Proprietorship',
      'Partnership Firm',
      'Section 8 (NGO/Trust)',
      'Udyam / MSME Registration',
      'Startup India Registration'
    ],
    targets: ['🆕 New Entrepreneurs', '🔄 Business Restructuring', '👥 Multiple Partners', '💼 Solo Founders'],
    process:
      'We handle the entire company formation process — name reservation, document preparation, ROC filing, PAN/TAN application, GST registration and all post-incorporation compliance. End-to-end in 7-15 working days.',
    related: ['legal-compliance', 'government-subsidies', 'accounting-cfo']
  },
  'government-subsidies': {
    title: 'Government Subsidies & Schemes',
    subtitle: 'PMEGP, CGTMSE, MUDRA and 50+ central & state schemes',
    emoji: '🏛️',
    cat: 'Finance',
    benefits: [
      'PMEGP — up to 35% capital subsidy',
      'CGTMSE — collateral-free loans to ₹2 Cr',
      'MUDRA Yojana — up to ₹10 Lakh',
      'Digital MSME cloud subsidies',
      'Trade Fair participation subsidy',
      'Design Clinic — 50-75% subsidy',
      'Technology Upgradation Fund',
      'State-specific schemes'
    ],
    targets: ['🆕 New MSME Units', '📈 Expanding MSMEs', '🏭 Manufacturing Units', '💻 Technology MSMEs'],
    process:
      'We identify all applicable schemes for your business profile, check eligibility, prepare complete documentation, file applications and follow up with government authorities. We have 100% success rate in PMEGP applications.',
    related: ['msme-loans-finance', 'company-formation', 'legal-compliance']
  },
  'technology-solutions': {
    title: 'Technology Solutions for MSMEs',
    subtitle: 'ERP, websites, mobile apps and complete digital transformation',
    emoji: '💻',
    cat: 'Technology',
    benefits: [
      'ERP & CRM Implementation',
      'Custom Website Development',
      'Mobile App Development',
      'Cloud Migration & Hosting',
      'Inventory Management Software',
      'Point of Sale (POS) Systems',
      'E-commerce Store Setup',
      'Cybersecurity Solutions'
    ],
    targets: ['🏭 Manufacturing MSMEs', '🛒 Retail Businesses', '💼 Service Companies', '🏥 Healthcare MSMEs'],
    process:
      'Our technology team assesses your current tech stack, recommends the right solutions, handles implementation with minimal business disruption and provides ongoing training and support.',
    related: ['sales-digital-marketing', 'research-development', 'coaching-consulting']
  },
  'sales-digital-marketing': {
    title: 'Sales & Digital Marketing',
    subtitle: 'SEO, Google Ads, social media and comprehensive lead generation',
    emoji: '📣',
    cat: 'Marketing',
    benefits: [
      'Search Engine Optimization (SEO)',
      'Google Ads & PPC Campaigns',
      'Social Media Marketing',
      'Content Marketing & Blogging',
      'Email Marketing Campaigns',
      'Lead Generation Systems',
      'Brand Identity & Design',
      'Video Marketing & YouTube'
    ],
    targets: ['🛒 B2C Businesses', '💼 B2B Services', '🏭 Manufacturers', '🌐 Export-Oriented MSMEs'],
    process:
      'We audit your current digital presence, develop a customized marketing strategy, execute campaigns across channels and provide monthly reporting with ROI tracking and continuous optimization.',
    related: ['technology-solutions', 'coaching-consulting', 'ipo-investment-banking']
  },
  'ipo-investment-banking': {
    title: 'IPO & Investment Banking',
    subtitle: 'SME IPO listing, fund raising and M&A advisory',
    emoji: '📈',
    cat: 'Growth',
    benefits: [
      'SME IPO on NSE Emerge & BSE SME',
      'Private Equity Fund Raising',
      'Venture Capital Connections',
      'Mergers & Acquisitions (M&A)',
      'Corporate Restructuring',
      'ESOP Design & Implementation',
      'Investment Due Diligence',
      'Investor Relations Management'
    ],
    targets: ['📈 High-Growth MSMEs', '🪙 Seeking Major Funding', '🏢 Established Companies', '🌐 Export-Ready Businesses'],
    process:
      'We assess your IPO readiness, help you prepare required documentation, connect you with merchant bankers and investors, guide you through SEBI compliance and support post-IPO investor relations.',
    related: ['coaching-consulting', 'accounting-cfo', 'legal-compliance']
  },
  'coaching-consulting': {
    title: 'Business Coaching & Consulting',
    subtitle: 'Strategy, leadership, management consulting and executive training',
    emoji: '🎓',
    cat: 'Growth',
    benefits: [
      'Business Strategy Development',
      'Leadership Coaching Programs',
      'Financial Planning & CFO Advisory',
      'Operational Excellence',
      'Export Strategy',
      'HR & Team Building',
      'Succession Planning',
      'Board Advisory Services'
    ],
    targets: ['👔 MSME Owners', '👩‍💼 Professionals', '🆕 First-Time Founders', '📈 Scaling Businesses'],
    process:
      'Our expert coaches and consultants work closely with MSME owners and teams through structured programs, workshops and one-on-one sessions tailored to your specific business challenges and growth objectives.',
    related: ['ipo-investment-banking', 'technology-solutions', 'hr-recruitment']
  },
  'legal-compliance': {
    title: 'Legal & Compliance Services',
    subtitle: 'GST, income tax, trademark, ISO and all regulatory compliance',
    emoji: '⚖️',
    cat: 'Legal',
    benefits: [
      'GST Registration & Returns',
      'Income Tax Filing',
      'Trademark Registration',
      'ISO Certification (9001, 14001)',
      'FSSAI License',
      'MSME Udyam Registration',
      'Import Export Code (IEC)',
      'Labour Law Compliance'
    ],
    targets: ['🏭 All MSMEs', '🛒 Food & FSSAI businesses', '🌐 Exporters', '🏢 Corporate MSMEs'],
    process:
      'Our compliance team handles all registrations, filing and renewals. We send timely reminders for due dates, prepare all documents and represent you before authorities when required.',
    related: ['company-formation', 'government-subsidies', 'accounting-cfo']
  },
  'research-development': {
    title: 'R&D & Innovation Support',
    subtitle: 'Product development, patent filing and innovation funding',
    emoji: '🔬',
    cat: 'Technology',
    benefits: [
      'R&D Tax Benefit (Section 35)',
      'DSIR Recognition',
      'Patent Filing & Protection',
      'Technology Transfer',
      'Product Development Support',
      'Quality Testing & Certification',
      'Innovation Fund Applications',
      'DST/DBT Grant Applications'
    ],
    targets: ['🏭 Tech Manufacturers', '💊 Pharma MSMEs', '🌾 Agri-tech Startups', '💡 Innovation-driven MSMEs'],
    process:
      'We help MSMEs identify and apply for R&D funding, assist with DSIR recognition for tax benefits, support patent applications, and connect you with technology institutions for knowledge transfer.',
    related: ['technology-solutions', 'government-subsidies', 'ipo-investment-banking']
  },
  'export-import': {
    title: 'Export & Import Services',
    subtitle: 'IEC registration, export documentation and international trade finance',
    emoji: '🌐',
    cat: 'Growth',
    benefits: [
      'IEC Registration',
      'Export Documentation',
      'Letter of Credit (LC)',
      'Bank Guarantee',
      'ECGC Insurance',
      'Trade Finance',
      'International Market Research',
      'Buyer-Seller Match Making'
    ],
    targets: ['🏭 Manufacturers', '🌾 Agricultural Exporters', '💎 Handicraft MSMEs', '💊 Pharmaceutical MSMEs'],
    process:
      'We assist with complete export-import registration, documentation preparation, freight forwarding arrangements and trade finance solutions to make your international business operations seamless.',
    related: ['legal-compliance', 'msme-loans-finance', 'government-subsidies']
  },
  'hr-recruitment': {
    title: 'HR & Recruitment Services',
    subtitle: 'Talent acquisition, payroll, PF/ESI and complete HR management',
    emoji: '👥',
    cat: 'Growth',
    benefits: [
      'Talent Acquisition & Hiring',
      'HR Policy Development',
      'Payroll Management',
      'PF & ESI Compliance',
      'Performance Management',
      'Training & Development',
      'Employee Benefits Administration',
      'Labour Law Compliance'
    ],
    targets: ['📈 Growing MSMEs', '🏭 Manufacturing Units', '💼 Service Companies', '🏥 Healthcare MSMEs'],
    process:
      'We provide end-to-end HR services from recruitment to compliance. Our HR professionals handle all people-related functions allowing you to focus on business growth.',
    related: ['coaching-consulting', 'legal-compliance', 'accounting-cfo']
  },
  'accounting-cfo': {
    title: 'Accounting & Virtual CFO',
    subtitle: 'Bookkeeping, financial statements, MIS and strategic financial planning',
    emoji: '📊',
    cat: 'Finance',
    benefits: [
      'Daily Bookkeeping',
      'Monthly P&L & Balance Sheet',
      'GST Reconciliation',
      'MIS & Dashboard Reporting',
      'Annual Financial Audit',
      'Budget & Forecasting',
      'Cash Flow Management',
      'Virtual CFO Services'
    ],
    targets: ['🆕 New Businesses', '📈 Growing MSMEs', '🪙 Loan-seeking Businesses', '🌐 Export MSMEs'],
    process:
      'Our accounting team provides regular bookkeeping, timely financial statements and strategic financial advice. Virtual CFO service gives you access to senior financial expertise at a fraction of the cost of a full-time CFO.',
    related: ['legal-compliance', 'company-formation', 'coaching-consulting']
  }
};

