export type ArticleSlug =
  | 'budget-2024-msme'
  | 'rg-textiles-success'
  | 'top-msme-schemes-2024'
  | 'digital-transformation-msme'
  | 'msme-credit-policy'
  | 'women-entrepreneur-story';

export type ArticleCategory = 'news' | 'blog' | 'success';

export interface ArticleModel {
  title: string;
  crumb: string;
  meta: string;
  content: string; // HTML string (rendered in template)
  category: ArticleCategory;
  dateLabel: string;
  summary: string;
  badgeText: string;
  badgeClass: string;
  cardIcon: string;
  cardClass: string;
}

export const ARTICLES_DATA: Record<ArticleSlug, ArticleModel> = {
  'budget-2024-msme': {
    title: 'Union Budget 2024: Key Announcements for MSMEs',
    crumb: 'Budget 2024',
    meta: 'News · February 1, 2024 · 5 min read',
    content:
      "<h2>Budget Highlights for the MSME Sector</h2><p>Finance Minister Nirmala Sitharaman's Union Budget 2024 brought several welcome announcements for India's MSME sector, focusing on enhanced credit access, technology adoption and manufacturing growth.</p><h2>Key Announcements</h2><p>₹22,138 crore was allocated for the MSME sector, representing a 15% increase from the previous year. The Credit Guarantee for MSMEs was enhanced with a corpus of ₹9,000 crore, enabling an additional collateral-free credit of ₹2 Lakh crore over the next 5 years.</p><h2>Technology Upgrade Fund</h2><p>A new ₹6,000 crore Technology Upgrade Fund was announced to help MSMEs adopt modern manufacturing equipment and digital tools. This fund will provide interest subvention of 2% on loans for technology upgradation.</p><h2>Impact Analysis</h2><p>Industry experts estimate that the enhanced credit guarantee alone could help 3 lakh+ new MSME units get access to formal credit in the next 2 years. The technology fund is expected to directly benefit 50,000+ manufacturing MSMEs.</p>",
    category: 'news',
    dateLabel: 'Feb 1, 2024',
    summary:
      'Finance Minister announces ₹22,138 crore for MSME sector including credit guarantee enhancements and technology upgrade fund.',
    badgeText: 'News',
    badgeClass: 'badge badge-blue',
    cardIcon: '📰',
    cardClass: 'news-img cat-news'
  },
  'rg-textiles-success': {
    title: 'How RG Textiles Secured ₹50 Lakh Loan in 2 Weeks',
    crumb: 'RG Textiles Story',
    meta: 'Success Story · January 28, 2024 · 4 min read',
    content:
      "<h2>The Challenge</h2><p>Rajesh Gupta, owner of RG Textiles in Surat's textile hub, had been struggling for 6 months to secure a working capital loan. Despite having a profitable business with ₹3 Crore annual turnover, banks kept rejecting his application due to documentation issues.</p><h2>The MSME Bharat Manch Solution</h2><p>When Rajesh reached out to MSME Bharat Manch, our team conducted a comprehensive business assessment. We identified three documentation gaps and helped him prepare a professional project report. Within 3 days, we had submitted applications to 4 banks simultaneously.</p><h2>The Result</h2><p>HDFC Bank approved ₹50 Lakh working capital limit within 12 days of application — the fastest turnaround in Rajesh's experience. The loan came with competitive interest rates under CGTMSE guarantee, eliminating the need for collateral.</p><h2>What Rajesh Says</h2><p>\"I wasted 6 months on my own. MSME Bharat Manch's team knew exactly what banks needed. The dedicated relationship manager was available on call throughout the process. Highly recommend to every MSME owner.\"</p>",
    category: 'success',
    dateLabel: 'Jan 28, 2024',
    summary:
      "Surat-based textile manufacturer shares how MSME Bharat Manch helped them secure working capital and government subsidies.",
    badgeText: 'Success Story',
    badgeClass: 'badge badge-orange',
    cardIcon: '⭐',
    cardClass: 'news-img cat-success'
  },
  'top-msme-schemes-2024': {
    title: 'Top 10 Government Schemes Every MSME Must Know in 2024',
    crumb: 'Top Schemes 2024',
    meta: 'Blog · January 20, 2024 · 8 min read',
    content:
      "<h2>Why Government Schemes Matter for MSMEs</h2><p>India's government offers over 100 schemes worth hundreds of crores specifically for MSMEs. Yet most MSME owners leave these benefits unclaimed simply because they don't know where to look or how to apply. This comprehensive guide covers the top 10 schemes you must explore in 2024.</p><h2>1. PMEGP — Prime Minister's Employment Generation Programme</h2><p>The crown jewel of MSME schemes. Up to 35% capital subsidy for new manufacturing enterprises and 25% for service sector. Maximum project cost ₹50 Lakh for manufacturing. If you're starting a new business, this should be your first stop.</p><h2>2. CGTMSE — Collateral-Free Credit</h2><p>Access loans up to ₹2 Crore without any collateral through Credit Guarantee Fund Trust. Especially valuable for first-generation entrepreneurs who don't have assets to pledge.</p><h2>3. MUDRA Yojana</h2><p>Loans from ₹50,000 to ₹10 Lakh for micro enterprises under Shishu, Kishore and Tarun categories. Available through all major banks.</p><h2>4. Digital MSME Scheme</h2><p>Get subsidized access to cloud ERP, CRM and digital tools. Government pays 50-75% of subscription cost.</p><h2>5. Trade Fair Subsidy</h2><p>Participate in national and international trade fairs with 50% government subsidy on stall charges. Excellent for exporters and product-based MSMEs.</p>",
    category: 'blog',
    dateLabel: 'Jan 20, 2024',
    summary:
      'A comprehensive guide to the most beneficial central and state government schemes available to Indian MSMEs this year.',
    badgeText: 'Blog',
    badgeClass: 'badge badge-green',
    cardIcon: '📋',
    cardClass: 'news-img cat-blog'
  },
  'digital-transformation-msme': {
    title: 'How Technology Can 10x Your MSME Growth',
    crumb: 'Digital Transformation',
    meta: 'Blog · January 15, 2024 · 6 min read',
    content:
      "<h2>The Digital Divide in Indian MSMEs</h2><p>While India has the world's second-largest MSME ecosystem, less than 15% of MSMEs have embraced digital technologies beyond basic mobile banking. This digital gap is the single biggest growth constraint for most small businesses today.</p><h2>The Five Technology Pillars for MSME Growth</h2><p>Our experience working with 10,000+ MSMEs across India has shown that digital transformation happens in five distinct stages: Digital Presence (website + social), Digital Operations (ERP/accounting), Digital Marketing (SEO + Ads), Digital Finance (online banking + UPI), and Digital Analytics (data-driven decisions).</p><h2>Real Results from Digital Adoption</h2><p>Amit Patel of Patel Engineering in Ahmedabad implemented a basic ERP system and Google Ads campaign in 2023. Within 90 days, his monthly inquiries went from 20 to 500+. Annual revenue grew from ₹1.2 Crore to ₹3.8 Crore in 18 months.</p><h2>Getting Started: 30-Day Action Plan</h2><p>Week 1: Set up Google Business Profile and basic website. Week 2: Create social media accounts and post consistently. Week 3: Implement free accounting software (Tally/Vyapar). Week 4: Start Google Ads with ₹5,000 budget and track results.</p>",
    category: 'blog',
    dateLabel: 'Jan 15, 2024',
    summary:
      "Digital transformation isn't optional anymore. Here's how MSMEs are using ERP, AI and digital marketing to stay competitive.",
    badgeText: 'Blog',
    badgeClass: 'badge badge-green',
    cardIcon: '💡',
    cardClass: 'news-img cat-blog'
  },
  'msme-credit-policy': {
    title: 'RBI Announces Enhanced Credit Limit for MSME Classification',
    crumb: 'RBI Credit Policy',
    meta: 'News · January 10, 2024 · 3 min read',
    content:
      '<h2>RBI Enhances MSME Credit Framework</h2><p>The Reserve Bank of India has revised the priority sector lending norms for MSMEs, enhancing credit limits and improving credit availability for the sector. The revised framework is expected to enable additional credit flow of ₹1.5 Lakh crore to MSMEs annually.</p><h2>Key Changes</h2><p>The sub-limit for micro enterprises within the overall MSME target has been enhanced. Banks must now ensure that 40% of their MSME lending goes to micro enterprises — up from the previous 35%.</p>',
    category: 'news',
    dateLabel: 'Jan 10, 2024',
    summary:
      'Reserve Bank of India revises MSME credit limits under priority sector lending, benefiting thousands of enterprises across India.',
    badgeText: 'News',
    badgeClass: 'badge badge-blue',
    cardIcon: '🏛️',
    cardClass: 'news-img cat-news'
  },
  'women-entrepreneur-story': {
    title: "From ₹50K to ₹2 Crore: A Woman Entrepreneur's Journey",
    crumb: "Priya's Story",
    meta: 'Success Story · January 5, 2024 · 5 min read',
    content:
      "<h2>The Beginning: A Home Kitchen in Pune</h2><p>In 2019, Priya Sharma started Flavours Kitchen from her home in Kothrud, Pune with just ₹50,000 in savings. She made traditional Maharashtra snacks and sold them to neighbours and through WhatsApp groups. By year end, she was generating ₹30,000 monthly revenue.</p><h2>The PMEGP Turning Point</h2><p>In early 2021, Priya applied for PMEGP through MSME Bharat Manch with a ₹25 Lakh project cost. She received 35% subsidy — ₹8.75 Lakh — which funded her first commercial kitchen space and packaging machinery.</p><h2>Scaling to ₹2 Crore</h2><p>With professional packaging, FSSAI license, and a digital marketing campaign handled by our team, Flavours Kitchen launched on Amazon and Swiggy Stores in 2022. By 2023, monthly revenue crossed ₹16 Lakh — on track for ₹2 Crore annual revenue.</p><h2>Priya's Message to Women Entrepreneurs</h2><p>\"Don't let lack of money or knowledge stop you. MSME Bharat Manch made the entire government scheme and loan process simple. Today I employ 12 women from my community. Every woman with a skill can build a successful business.\"</p>",
    category: 'success',
    dateLabel: 'Jan 5, 2024',
    summary:
      "Priya Sharma of Pune's Flavours Kitchen shares her incredible journey from home kitchen to full-scale food processing unit.",
    badgeText: 'Success Story',
    badgeClass: 'badge badge-orange',
    cardIcon: '👩‍💼',
    cardClass: 'news-img cat-success'
  }
};

