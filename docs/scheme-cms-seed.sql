-- Scheme CMS seed — exact copy of former static schemes.data.ts content.
-- Run AFTER docs/scheme-cms-create-tables.sql. Safe to re-run (skips existing slugs).
-- Uses GETDATE() (server local time, not UTC).

SET NOCOUNT ON;
DECLARE @Now DATETIME2 = GETDATE();

IF COL_LENGTH(N'dbo.Schemes', N'HomeDescription') IS NULL
BEGIN
  ALTER TABLE dbo.Schemes
    ADD HomeDescription NVARCHAR(2000) NOT NULL
      CONSTRAINT DF_Schemes_HomeDescription DEFAULT (N'') WITH VALUES;
END
GO

SET NOCOUNT ON;
DECLARE @Now DATETIME2 = GETDATE();

/* ── Categories ── */
IF NOT EXISTS (SELECT 1 FROM dbo.SchemeCategories WHERE Slug = N'central')
  INSERT INTO dbo.SchemeCategories (Slug, Name, SortOrder, IsActive, ShowInFilter, CreatedAt, UpdatedAt)
  VALUES (N'central', N'Central Govt', 1, 1, 1, @Now, @Now);

IF NOT EXISTS (SELECT 1 FROM dbo.SchemeCategories WHERE Slug = N'credit')
  INSERT INTO dbo.SchemeCategories (Slug, Name, SortOrder, IsActive, ShowInFilter, CreatedAt, UpdatedAt)
  VALUES (N'credit', N'Credit', 2, 1, 1, @Now, @Now);

IF NOT EXISTS (SELECT 1 FROM dbo.SchemeCategories WHERE Slug = N'technology')
  INSERT INTO dbo.SchemeCategories (Slug, Name, SortOrder, IsActive, ShowInFilter, CreatedAt, UpdatedAt)
  VALUES (N'technology', N'Technology', 3, 1, 1, @Now, @Now);

IF NOT EXISTS (SELECT 1 FROM dbo.SchemeCategories WHERE Slug = N'marketing')
  INSERT INTO dbo.SchemeCategories (Slug, Name, SortOrder, IsActive, ShowInFilter, CreatedAt, UpdatedAt)
  VALUES (N'marketing', N'Marketing', 4, 1, 1, @Now, @Now);

GO

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @SchemeId INT;

/* ── PMEGP ── */
IF NOT EXISTS (SELECT 1 FROM dbo.Schemes WHERE Slug = N'pmegp')
BEGIN
  INSERT INTO dbo.Schemes (
    Slug, Name, Crumb, Tagline, ShortDescription, ContentHtml, CategorySlug,
    PrimaryBadgeText, PrimaryBadgeClass, SecondaryBadgeText, SecondaryBadgeClass,
    HomeTitle, HomeBadgeText, HomeBadgeClass, HomeDescription,
    IsPublished, IsFeatured, SortOrder, CreatedAt, UpdatedAt
  ) VALUES (
    N'pmegp',
    N'PMEGP — Prime Minister''s Employment Generation Programme',
    N'PMEGP',
    N'Up to 35% capital subsidy for setting up new micro enterprises',
    N'Financial assistance in the form of subsidy for setting up new micro enterprises in non-farm sector. Subsidy up to 25% in urban areas and 35% in rural areas.',
    N'<h2>About PMEGP</h2><p>The Prime Minister''s Employment Generation Programme (PMEGP) is a credit-linked subsidy programme administered by the Ministry of MSME. It provides financial assistance in the form of subsidy to set up new micro enterprises in the non-farm sector.</p><h2>Eligibility</h2><p>Any individual above 18 years of age can apply. For projects above ₹10 Lakh in manufacturing and ₹5 Lakh in service sector, the applicant must have passed at least 8th standard. Self Help Groups, Charitable Trusts and Co-operative Societies are also eligible.</p><h2>Documents Required</h2><p>Aadhaar card, PAN card, educational certificate, project report, caste certificate (if applicable), passport-size photograph, and bank account details.</p>',
    N'central',
    N'Central Govt', N'badge-green', N'Up to 35% subsidy', N'badge-orange',
    N'PMEGP Scheme', N'Central', N'badge-green',
    N'Prime Minister''s Employment Generation Programme — up to 35% subsidy for new enterprises. Max project cost ₹50 Lakh for manufacturing.',
    1, 1, 1, @Now, @Now
  );
  SET @SchemeId = SCOPE_IDENTITY();
  INSERT INTO dbo.SchemeCardHighlights (SchemeId, Text, SortOrder) VALUES
    (@SchemeId, N'Max project cost: ₹50 Lakh (manufacturing)', 1),
    (@SchemeId, N'Max project cost: ₹20 Lakh (service)', 2),
    (@SchemeId, N'Beneficiary contribution: 5-10% only', 3);
  INSERT INTO dbo.SchemeBenefits (SchemeId, Text, SortOrder) VALUES
    (@SchemeId, N'Subsidy up to 35% in rural areas, 25% in urban areas', 1),
    (@SchemeId, N'Max project cost ₹50 Lakh for manufacturing', 2),
    (@SchemeId, N'Max project cost ₹20 Lakh for service sector', 3),
    (@SchemeId, N'Only 5-10% own contribution required', 4),
    (@SchemeId, N'Available for new enterprises only', 5),
    (@SchemeId, N'Applied through KVIC, State KVIBs and DIC', 6);
END

/* ── CGTMSE ── */
IF NOT EXISTS (SELECT 1 FROM dbo.Schemes WHERE Slug = N'cgtmse')
BEGIN
  INSERT INTO dbo.Schemes (
    Slug, Name, Crumb, Tagline, ShortDescription, ContentHtml, CategorySlug,
    PrimaryBadgeText, PrimaryBadgeClass, SecondaryBadgeText, SecondaryBadgeClass,
    HomeTitle, HomeBadgeText, HomeBadgeClass, HomeDescription,
    IsPublished, IsFeatured, SortOrder, CreatedAt, UpdatedAt
  ) VALUES (
    N'cgtmse',
    N'CGTMSE — Credit Guarantee Fund Trust for MSMEs',
    N'CGTMSE',
    N'Collateral-free loans up to ₹2 Crore for micro and small enterprises',
    N'Provides collateral-free loans up to ₹2 Crore to micro and small enterprises. No third-party guarantee required from the borrower.',
    N'<h2>About CGTMSE</h2><p>The Credit Guarantee Fund Trust for Micro and Small Enterprises (CGTMSE) facilitates collateral-free loans to micro and small enterprises. The trust provides guarantee to banks who lend to MSMEs without requiring any collateral security.</p><h2>How It Works</h2><p>The lender (bank/NBFC) extends credit to the eligible borrower without any collateral or third-party guarantee. The credit facility extended is covered under CGTMSE''s guarantee scheme, providing the lender with a guarantee cover.</p><h2>Guarantee Coverage</h2><p>Coverage up to 85% for loans to micro enterprises up to ₹5 Lakh. 80% coverage for MSMEs in North East region and women entrepreneurs. 75% coverage for all other eligible borrowers.</p>',
    N'credit',
    N'Credit', N'badge-blue', N'Collateral Free', N'badge-green',
    N'CGTMSE Scheme', N'Credit', N'badge-blue',
    N'Collateral-free loans up to ₹2 Crore for MSMEs under Credit Guarantee Fund Trust. No third-party guarantee required.',
    1, 1, 2, @Now, @Now
  );
  SET @SchemeId = SCOPE_IDENTITY();
  INSERT INTO dbo.SchemeCardHighlights (SchemeId, Text, SortOrder) VALUES
    (@SchemeId, N'Loans up to ₹2 Crore without collateral', 1),
    (@SchemeId, N'Covers manufacturing and service sectors', 2),
    (@SchemeId, N'Both new and existing enterprises eligible', 3);
  INSERT INTO dbo.SchemeBenefits (SchemeId, Text, SortOrder) VALUES
    (@SchemeId, N'Loans up to ₹2 Crore without collateral', 1),
    (@SchemeId, N'No third-party guarantee required', 2),
    (@SchemeId, N'Covers manufacturing and service sectors', 3),
    (@SchemeId, N'Both new and existing enterprises eligible', 4),
    (@SchemeId, N'Guarantee fee partially subsidized for women/NE/vulnerable', 5),
    (@SchemeId, N'Available through all major banks', 6);
END

/* ── MUDRA ── */
IF NOT EXISTS (SELECT 1 FROM dbo.Schemes WHERE Slug = N'mudra')
BEGIN
  INSERT INTO dbo.Schemes (
    Slug, Name, Crumb, Tagline, ShortDescription, ContentHtml, CategorySlug,
    PrimaryBadgeText, PrimaryBadgeClass, SecondaryBadgeText, SecondaryBadgeClass,
    HomeTitle, HomeBadgeText, HomeBadgeClass, HomeDescription,
    IsPublished, IsFeatured, SortOrder, CreatedAt, UpdatedAt
  ) VALUES (
    N'mudra',
    N'MUDRA Yojana — Micro Units Development & Refinance Agency',
    N'MUDRA',
    N'Loans up to ₹10 Lakh for non-corporate micro enterprises',
    N'Provides loans up to ₹10 Lakh to non-corporate, non-farm small and micro enterprises under three categories: Shishu, Kishore, and Tarun.',
    N'<h2>About MUDRA Yojana</h2><p>Pradhan Mantri MUDRA Yojana (PMMY) is a scheme to provide loans up to ₹10 Lakh to non-corporate, non-farm small/micro enterprises. Loans are classified under three categories based on the stage of growth of the beneficiary.</p><h2>Three Categories</h2><p><strong>Shishu</strong>: For businesses in initial stage — loans up to ₹50,000 with subsidized interest rates. <strong>Kishore</strong>: For businesses that have already started — ₹50,000 to ₹5 Lakh. <strong>Tarun</strong>: For established businesses seeking expansion — ₹5 Lakh to ₹10 Lakh.</p><h2>Eligible Activities</h2><p>Small manufacturing units, shopkeepers, fruit/vegetable vendors, truck operators, food service units, repair shops, machine operators, artisans, and other service sector activities.</p>',
    N'central',
    N'Central Govt', N'badge-red', N'Up to ₹10 Lakh', N'badge-orange',
    N'MUDRA Yojana', N'Central', N'badge-red',
    N'Loans up to ₹10 Lakh under Shishu, Kishore and Tarun categories for micro enterprises and small businesses.',
    1, 1, 3, @Now, @Now
  );
  SET @SchemeId = SCOPE_IDENTITY();
  INSERT INTO dbo.SchemeCardHighlights (SchemeId, Text, SortOrder) VALUES
    (@SchemeId, N'Shishu: Up to ₹50,000', 1),
    (@SchemeId, N'Kishore: ₹50,000 to ₹5 Lakh', 2),
    (@SchemeId, N'Tarun: ₹5 Lakh to ₹10 Lakh', 3);
  INSERT INTO dbo.SchemeBenefits (SchemeId, Text, SortOrder) VALUES
    (@SchemeId, N'Shishu: Loans up to ₹50,000', 1),
    (@SchemeId, N'Kishore: ₹50,000 to ₹5 Lakh', 2),
    (@SchemeId, N'Tarun: ₹5 Lakh to ₹10 Lakh', 3),
    (@SchemeId, N'No collateral required for most loans', 4),
    (@SchemeId, N'Available through banks, NBFCs, MFIs', 5),
    (@SchemeId, N'Covers non-farm income generating activities', 6);
END

/* ── Digital MSME ── */
IF NOT EXISTS (SELECT 1 FROM dbo.Schemes WHERE Slug = N'digital-msme')
BEGIN
  INSERT INTO dbo.Schemes (
    Slug, Name, Crumb, Tagline, ShortDescription, ContentHtml, CategorySlug,
    PrimaryBadgeText, PrimaryBadgeClass, SecondaryBadgeText, SecondaryBadgeClass,
    HomeTitle, HomeBadgeText, HomeBadgeClass, HomeDescription,
    IsPublished, IsFeatured, SortOrder, CreatedAt, UpdatedAt
  ) VALUES (
    N'digital-msme',
    N'Digital MSME Scheme',
    N'Digital MSME',
    N'Government-subsidized cloud computing and digital solutions for MSMEs',
    N'Promotes adoption of Information and Communication Technology (ICT) tools by MSMEs. Provides subsidized cloud services and digital transformation support.',
    N'<h2>About Digital MSME Scheme</h2><p>The Digital MSME Scheme of the Ministry of MSME promotes the use of cloud computing and digital tools by MSMEs. The scheme provides subsidized access to cloud-based software and services to help MSMEs improve productivity and competitiveness.</p><h2>Benefits</h2><p>MSMEs can access cloud-based Enterprise Resource Planning (ERP), Customer Relationship Management (CRM), accounting, HR and other business software at subsidized rates. The government covers 75% of the cloud subscription cost for micro enterprises and 50% for small enterprises.</p>',
    N'technology',
    N'Technology', N'badge-blue', N'Subsidy Available', N'badge-green',
    N'Digital MSME Scheme', N'Technology', N'badge-orange',
    N'Cloud computing, ERP and digital solutions with government subsidy under the Digital MSME Scheme.',
    1, 1, 4, @Now, @Now
  );
  SET @SchemeId = SCOPE_IDENTITY();
  INSERT INTO dbo.SchemeCardHighlights (SchemeId, Text, SortOrder) VALUES
    (@SchemeId, N'Subsidized cloud computing services', 1),
    (@SchemeId, N'ERP and digital solution support', 2),
    (@SchemeId, N'Training and capacity building', 3);
  INSERT INTO dbo.SchemeBenefits (SchemeId, Text, SortOrder) VALUES
    (@SchemeId, N'Subsidized cloud computing services', 1),
    (@SchemeId, N'ERP and digital solution support', 2),
    (@SchemeId, N'Training and capacity building', 3),
    (@SchemeId, N'Available for Micro and Small enterprises', 4),
    (@SchemeId, N'Coverage up to 75% of subscription cost', 5),
    (@SchemeId, N'Multiple approved cloud service providers', 6);
END

/* ── Trade Fair ── */
IF NOT EXISTS (SELECT 1 FROM dbo.Schemes WHERE Slug = N'trade-fair')
BEGIN
  INSERT INTO dbo.Schemes (
    Slug, Name, Crumb, Tagline, ShortDescription, ContentHtml, CategorySlug,
    PrimaryBadgeText, PrimaryBadgeClass, SecondaryBadgeText, SecondaryBadgeClass,
    HomeTitle, HomeBadgeText, HomeBadgeClass, HomeDescription,
    IsPublished, IsFeatured, SortOrder, CreatedAt, UpdatedAt
  ) VALUES (
    N'trade-fair',
    N'Trade Fair Subsidy Scheme',
    N'Trade Fair',
    N'Government assistance for MSME participation in national and international trade fairs',
    N'Government assistance for MSME participation in national and international trade fairs, exhibitions and buyer-seller meets to promote their products globally.',
    N'<h2>About Trade Fair Subsidy</h2><p>The Development Commissioner, Ministry of MSME provides financial assistance to MSMEs for participating in national and international trade fairs and exhibitions. This helps MSMEs access new markets and showcase their products.</p><h2>How to Apply</h2><p>Applications are accepted at the DC-MSME office before the trade fair. Applications must include MSME registration, details of the trade fair and nature of products. Contact our team for step-by-step guidance.</p>',
    N'marketing',
    N'Marketing', N'badge-orange', N'50% subsidy', N'badge-green',
    N'Trade Fair Subsidy', N'Marketing', N'badge-green',
    N'Government subsidized participation in national and international trade fairs for MSME exporters and manufacturers.',
    1, 1, 5, @Now, @Now
  );
  SET @SchemeId = SCOPE_IDENTITY();
  INSERT INTO dbo.SchemeCardHighlights (SchemeId, Text, SortOrder) VALUES
    (@SchemeId, N'50% subsidy on stall charges', 1),
    (@SchemeId, N'Domestic and international trade fairs', 2),
    (@SchemeId, N'Exporter MSMEs especially eligible', 3);
  INSERT INTO dbo.SchemeBenefits (SchemeId, Text, SortOrder) VALUES
    (@SchemeId, N'50% subsidy on stall charges for domestic fairs', 1),
    (@SchemeId, N'Air fare reimbursement for international fairs', 2),
    (@SchemeId, N'Transport charges for product samples reimbursed', 3),
    (@SchemeId, N'Available for manufacturing MSMEs', 4),
    (@SchemeId, N'Both domestic and international fairs covered', 5),
    (@SchemeId, N'Applied through DC-MSME office', 6);
END

/* ── Design Clinic ── */
IF NOT EXISTS (SELECT 1 FROM dbo.Schemes WHERE Slug = N'design-clinic')
BEGIN
  INSERT INTO dbo.Schemes (
    Slug, Name, Crumb, Tagline, ShortDescription, ContentHtml, CategorySlug,
    PrimaryBadgeText, PrimaryBadgeClass, SecondaryBadgeText, SecondaryBadgeClass,
    HomeTitle, HomeBadgeText, HomeBadgeClass, HomeDescription,
    IsPublished, IsFeatured, SortOrder, CreatedAt, UpdatedAt
  ) VALUES (
    N'design-clinic',
    N'Design Clinic Scheme for MSMEs',
    N'Design Clinic',
    N'50-75% subsidy on professional design consultancy services',
    N'Brings Design expertise to Indian MSMEs for improving design capability and to provide access to expert design solutions to enhance product quality and market reach.',
    N'<h2>About Design Clinic Scheme</h2><p>The Design Clinic Scheme of Ministry of MSME brings design expertise to the Indian MSME sector to help them improve their products, packaging, and brand identity. The scheme bridges the gap between design professionals and MSMEs.</p><h2>What Design Services Are Covered</h2><p>Product design and development, packaging design, branding and identity design, interior design for retail, exhibition stall design, user interface/experience design, and other professional design services from empaneled design agencies.</p>',
    N'marketing',
    N'Marketing', N'badge-blue', N'Up to 75% subsidy', N'badge-orange',
    N'Design Clinic Scheme', N'Design', N'badge-blue',
    N'50–75% subsidy for design consultancy services to improve product quality, packaging and marketability of MSME products.',
    1, 1, 6, @Now, @Now
  );
  SET @SchemeId = SCOPE_IDENTITY();
  INSERT INTO dbo.SchemeCardHighlights (SchemeId, Text, SortOrder) VALUES
    (@SchemeId, N'50-75% subsidy on design consultancy', 1),
    (@SchemeId, N'Product design and packaging', 2),
    (@SchemeId, N'Manufacturing and service sectors', 3);
  INSERT INTO dbo.SchemeBenefits (SchemeId, Text, SortOrder) VALUES
    (@SchemeId, N'50% subsidy for individual MSME (min ₹40,000)', 1),
    (@SchemeId, N'75% subsidy for cluster of MSMEs', 2),
    (@SchemeId, N'Covers product design and packaging', 3),
    (@SchemeId, N'Both manufacturing and service sectors eligible', 4),
    (@SchemeId, N'Applied through National Institute of Design', 5),
    (@SchemeId, N'Improves product marketability and quality', 6);
END

GO

-- Keep stored primary badge text aligned with category names (safe to re-run).
UPDATE s
SET s.PrimaryBadgeText = c.Name
FROM dbo.Schemes s
INNER JOIN dbo.SchemeCategories c ON c.Slug = s.CategorySlug;

-- Homepage card copy (original home.html text). Safe to re-run.
UPDATE dbo.Schemes SET HomeDescription = N'Prime Minister''s Employment Generation Programme — up to 35% subsidy for new enterprises. Max project cost ₹50 Lakh for manufacturing.' WHERE Slug = N'pmegp';
UPDATE dbo.Schemes SET HomeDescription = N'Collateral-free loans up to ₹2 Crore for MSMEs under Credit Guarantee Fund Trust. No third-party guarantee required.' WHERE Slug = N'cgtmse';
UPDATE dbo.Schemes SET HomeDescription = N'Loans up to ₹10 Lakh under Shishu, Kishore and Tarun categories for micro enterprises and small businesses.' WHERE Slug = N'mudra';
UPDATE dbo.Schemes SET HomeDescription = N'Cloud computing, ERP and digital solutions with government subsidy under the Digital MSME Scheme.' WHERE Slug = N'digital-msme';
UPDATE dbo.Schemes SET HomeDescription = N'Government subsidized participation in national and international trade fairs for MSME exporters and manufacturers.' WHERE Slug = N'trade-fair';
UPDATE dbo.Schemes SET HomeDescription = N'50–75% subsidy for design consultancy services to improve product quality, packaging and marketability of MSME products.' WHERE Slug = N'design-clinic';

GO

PRINT N'Scheme CMS seed complete.';
