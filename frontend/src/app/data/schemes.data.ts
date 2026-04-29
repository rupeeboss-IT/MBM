export type SchemeSlug =
  | 'pmegp'
  | 'cgtmse'
  | 'mudra'
  | 'digital-msme'
  | 'trade-fair'
  | 'design-clinic';

export interface SchemeModel {
  title: string;
  crumb: string;
  subtitle: string;
  benefits: string[];
  content: string; // HTML string (rendered in template)
}

export const SCHEMES_DATA: Record<SchemeSlug, SchemeModel> = {
  pmegp: {
    title: "PMEGP — Prime Minister's Employment Generation Programme",
    crumb: 'PMEGP',
    subtitle: 'Up to 35% capital subsidy for setting up new micro enterprises',
    benefits: [
      'Subsidy up to 35% in rural areas, 25% in urban areas',
      'Max project cost ₹50 Lakh for manufacturing',
      'Max project cost ₹20 Lakh for service sector',
      'Only 5-10% own contribution required',
      'Available for new enterprises only',
      'Applied through KVIC, State KVIBs and DIC'
    ],
    content:
      "<h2>About PMEGP</h2><p>The Prime Minister's Employment Generation Programme (PMEGP) is a credit-linked subsidy programme administered by the Ministry of MSME. It provides financial assistance in the form of subsidy to set up new micro enterprises in the non-farm sector.</p><h2>Eligibility</h2><p>Any individual above 18 years of age can apply. For projects above ₹10 Lakh in manufacturing and ₹5 Lakh in service sector, the applicant must have passed at least 8th standard. Self Help Groups, Charitable Trusts and Co-operative Societies are also eligible.</p><h2>Documents Required</h2><p>Aadhaar card, PAN card, educational certificate, project report, caste certificate (if applicable), passport-size photograph, and bank account details.</p>"
  },
  cgtmse: {
    title: 'CGTMSE — Credit Guarantee Fund Trust for MSMEs',
    crumb: 'CGTMSE',
    subtitle: 'Collateral-free loans up to ₹2 Crore for micro and small enterprises',
    benefits: [
      'Loans up to ₹2 Crore without collateral',
      'No third-party guarantee required',
      'Covers manufacturing and service sectors',
      'Both new and existing enterprises eligible',
      'Guarantee fee partially subsidized for women/NE/vulnerable',
      'Available through all major banks'
    ],
    content:
      "<h2>About CGTMSE</h2><p>The Credit Guarantee Fund Trust for Micro and Small Enterprises (CGTMSE) facilitates collateral-free loans to micro and small enterprises. The trust provides guarantee to banks who lend to MSMEs without requiring any collateral security.</p><h2>How It Works</h2><p>The lender (bank/NBFC) extends credit to the eligible borrower without any collateral or third-party guarantee. The credit facility extended is covered under CGTMSE's guarantee scheme, providing the lender with a guarantee cover.</p><h2>Guarantee Coverage</h2><p>Coverage up to 85% for loans to micro enterprises up to ₹5 Lakh. 80% coverage for MSMEs in North East region and women entrepreneurs. 75% coverage for all other eligible borrowers.</p>"
  },
  mudra: {
    title: 'MUDRA Yojana — Micro Units Development & Refinance Agency',
    crumb: 'MUDRA',
    subtitle: 'Loans up to ₹10 Lakh for non-corporate micro enterprises',
    benefits: [
      'Shishu: Loans up to ₹50,000',
      'Kishore: ₹50,000 to ₹5 Lakh',
      'Tarun: ₹5 Lakh to ₹10 Lakh',
      'No collateral required for most loans',
      'Available through banks, NBFCs, MFIs',
      'Covers non-farm income generating activities'
    ],
    content:
      "<h2>About MUDRA Yojana</h2><p>Pradhan Mantri MUDRA Yojana (PMMY) is a scheme to provide loans up to ₹10 Lakh to non-corporate, non-farm small/micro enterprises. Loans are classified under three categories based on the stage of growth of the beneficiary.</p><h2>Three Categories</h2><p><strong>Shishu</strong>: For businesses in initial stage — loans up to ₹50,000 with subsidized interest rates. <strong>Kishore</strong>: For businesses that have already started — ₹50,000 to ₹5 Lakh. <strong>Tarun</strong>: For established businesses seeking expansion — ₹5 Lakh to ₹10 Lakh.</p><h2>Eligible Activities</h2><p>Small manufacturing units, shopkeepers, fruit/vegetable vendors, truck operators, food service units, repair shops, machine operators, artisans, and other service sector activities.</p>"
  },
  'digital-msme': {
    title: 'Digital MSME Scheme',
    crumb: 'Digital MSME',
    subtitle: 'Government-subsidized cloud computing and digital solutions for MSMEs',
    benefits: [
      'Subsidized cloud computing services',
      'ERP and digital solution support',
      'Training and capacity building',
      'Available for Micro and Small enterprises',
      'Coverage up to 75% of subscription cost',
      'Multiple approved cloud service providers'
    ],
    content:
      '<h2>About Digital MSME Scheme</h2><p>The Digital MSME Scheme of the Ministry of MSME promotes the use of cloud computing and digital tools by MSMEs. The scheme provides subsidized access to cloud-based software and services to help MSMEs improve productivity and competitiveness.</p><h2>Benefits</h2><p>MSMEs can access cloud-based Enterprise Resource Planning (ERP), Customer Relationship Management (CRM), accounting, HR and other business software at subsidized rates. The government covers 75% of the cloud subscription cost for micro enterprises and 50% for small enterprises.</p>'
  },
  'trade-fair': {
    title: 'Trade Fair Subsidy Scheme',
    crumb: 'Trade Fair',
    subtitle:
      'Government assistance for MSME participation in national and international trade fairs',
    benefits: [
      '50% subsidy on stall charges for domestic fairs',
      'Air fare reimbursement for international fairs',
      'Transport charges for product samples reimbursed',
      'Available for manufacturing MSMEs',
      'Both domestic and international fairs covered',
      'Applied through DC-MSME office'
    ],
    content:
      '<h2>About Trade Fair Subsidy</h2><p>The Development Commissioner, Ministry of MSME provides financial assistance to MSMEs for participating in national and international trade fairs and exhibitions. This helps MSMEs access new markets and showcase their products.</p><h2>How to Apply</h2><p>Applications are accepted at the DC-MSME office before the trade fair. Applications must include MSME registration, details of the trade fair and nature of products. Contact our team for step-by-step guidance.</p>'
  },
  'design-clinic': {
    title: 'Design Clinic Scheme for MSMEs',
    crumb: 'Design Clinic',
    subtitle: '50-75% subsidy on professional design consultancy services',
    benefits: [
      '50% subsidy for individual MSME (min ₹40,000)',
      '75% subsidy for cluster of MSMEs',
      'Covers product design and packaging',
      'Both manufacturing and service sectors eligible',
      'Applied through National Institute of Design',
      'Improves product marketability and quality'
    ],
    content:
      '<h2>About Design Clinic Scheme</h2><p>The Design Clinic Scheme of Ministry of MSME brings design expertise to the Indian MSME sector to help them improve their products, packaging, and brand identity. The scheme bridges the gap between design professionals and MSMEs.</p><h2>What Design Services Are Covered</h2><p>Product design and development, packaging design, branding and identity design, interior design for retail, exhibition stall design, user interface/experience design, and other professional design services from empaneled design agencies.</p>'
  }
};

