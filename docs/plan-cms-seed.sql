-- Seed / update CMS-managed plans (basic, standard, premium, pro). Run after plan-cms-create-tables.sql.

DECLARE @now DATETIME2 = SYSUTCDATETIME();

-- basic
IF NOT EXISTS (SELECT 1 FROM dbo.Plans WHERE Code = N'basic')
BEGIN
  INSERT INTO dbo.Plans (Code, Name, Description, BaseAmountPaise, GstPercent, GstPaise, TotalAmountPaise, Currency, DurationDays, IsActive, CreatedAt, UpdatedAt,
    IconEmoji, BadgeClass, Tagline, IsFeatured, SortOrder, DisplayPriceSuffix, CtaLabel)
  VALUES (N'basic', N'MSME Basic', N'For any MSME with Udyam Aadhaar — start your growth journey.', 99900, 18, 17982, 117882, N'INR', 365, 1, @now, @now,
    CONVERT(NVARCHAR(20), 0x3CD831DF), N'free', N'For any MSME with Udyam Aadhaar — start your growth journey.', 0, 1, N'/ year + 18% GST', N'Get MSME Basic');
END
ELSE
BEGIN
  UPDATE dbo.Plans SET
    Name = N'MSME Basic',
    Tagline = N'For any MSME with Udyam Aadhaar — start your growth journey.',
    IconEmoji = CONVERT(NVARCHAR(20), 0x3CD831DF), BadgeClass = N'free', SortOrder = 1,
    DisplayPriceSuffix = N'/ year + 18% GST', CtaLabel = N'Get MSME Basic', UpdatedAt = @now
  WHERE Code = N'basic';
END

-- premium
IF NOT EXISTS (SELECT 1 FROM dbo.Plans WHERE Code = N'premium')
BEGIN
  INSERT INTO dbo.Plans (Code, Name, Description, BaseAmountPaise, GstPercent, GstPaise, TotalAmountPaise, Currency, DurationDays, IsActive, CreatedAt, UpdatedAt,
    IconEmoji, BadgeClass, Tagline, IsFeatured, SortOrder, IncludesPlanCode, DisplayPriceSuffix, CtaLabel, PopularLabel)
  VALUES (N'premium', N'MSME Premium', N'Perfect for MSMEs ready to build financial credibility.', 999900, 18, 179982, 1179882, N'INR', 365, 1, @now, @now,
    CONVERT(NVARCHAR(20), 0x3CD8C6DF), N'gold', N'Perfect for MSMEs ready to build financial credibility.', 1, 2, N'basic', N'/ year + 18% GST', N'Get MSME Premium', N'Most Popular');
END
ELSE
BEGIN
  UPDATE dbo.Plans SET
    Name = N'MSME Premium',
    Tagline = N'Perfect for MSMEs ready to build financial credibility.',
    IconEmoji = CONVERT(NVARCHAR(20), 0x3CD8C6DF), BadgeClass = N'gold', IsFeatured = 1, SortOrder = 2, IncludesPlanCode = N'basic',
    DisplayPriceSuffix = N'/ year + 18% GST', CtaLabel = N'Get MSME Premium', PopularLabel = N'Most Popular', UpdatedAt = @now
  WHERE Code = N'premium';
END

-- pro
IF NOT EXISTS (SELECT 1 FROM dbo.Plans WHERE Code = N'pro')
BEGIN
  INSERT INTO dbo.Plans (Code, Name, Description, BaseAmountPaise, GstPercent, GstPaise, TotalAmountPaise, Currency, DurationDays, IsActive, CreatedAt, UpdatedAt,
    IconEmoji, BadgeClass, Tagline, IsFeatured, SortOrder, IncludesPlanCode, DisplayPriceSuffix, CtaLabel)
  VALUES (N'pro', N'MSME Pro', N'For MSMEs targeting government contracts and maximum growth.', 1999900, 18, 359982, 2359882, N'INR', 365, 1, @now, @now,
    CONVERT(NVARCHAR(20), 0x3DD88EDC), N'platinum', N'For MSMEs targeting government contracts and maximum growth.', 0, 3, N'premium', N'/ year + 18% GST', N'Get MSME Pro');
END
ELSE
BEGIN
  UPDATE dbo.Plans SET
    Name = N'MSME Pro',
    Tagline = N'For MSMEs targeting government contracts and maximum growth.',
    IconEmoji = CONVERT(NVARCHAR(20), 0x3DD88EDC), BadgeClass = N'platinum', SortOrder = 3, IncludesPlanCode = N'premium',
    DisplayPriceSuffix = N'/ year + 18% GST', CtaLabel = N'Get MSME Pro', UpdatedAt = @now
  WHERE Code = N'pro';
END

-- standard (legacy tier — CMS-managed if present)
IF NOT EXISTS (SELECT 1 FROM dbo.Plans WHERE Code = N'standard')
BEGIN
  INSERT INTO dbo.Plans (Code, Name, Description, BaseAmountPaise, GstPercent, GstPaise, TotalAmountPaise, Currency, DurationDays, IsActive, CreatedAt, UpdatedAt,
    IconEmoji, BadgeClass, Tagline, IsFeatured, SortOrder, DisplayPriceSuffix, CtaLabel)
  VALUES (N'standard', N'MSME Standard', N'Website + WhatsApp automation for growing MSMEs.', 499900, 18, 89982, 589882, N'INR', 365, 0, @now, @now,
    CONVERT(NVARCHAR(20), 0x3CD810DF), N'gold', N'Website + WhatsApp automation for growing MSMEs.', 0, 4, N'/ year + 18% GST', N'Get MSME Standard');
END

DECLARE @basicId INT = (SELECT PlanId FROM dbo.Plans WHERE Code = N'basic');
DECLARE @premiumId INT = (SELECT PlanId FROM dbo.Plans WHERE Code = N'premium');
DECLARE @proId INT = (SELECT PlanId FROM dbo.Plans WHERE Code = N'pro');
DECLARE @standardId INT = (SELECT PlanId FROM dbo.Plans WHERE Code = N'standard');

-- Replace features for basic
IF @basicId IS NOT NULL
BEGIN
  DELETE FROM dbo.PlanFeatures WHERE PlanId = @basicId;
  INSERT INTO dbo.PlanFeatures (PlanId, Text, Description, OfferingSlug, IsIncludesLine, SortOrder) VALUES
    (@basicId, N'Join with Udyam Aadhaar', N'Join as an MSME member and start your growth journey.', NULL, 0, 1),
    (@basicId, N'National MSME network', N'Connect with the national MSME ecosystem.', NULL, 0, 2),
    (@basicId, N'Knowledge & events access', N'Access MSME learning resources and events.', N'msme-events', 0, 3),
    (@basicId, N'Growth ecosystem access', N'Access the MSME growth ecosystem.', NULL, 0, 4),
    (@basicId, N'WhatsApp Business Platform', N'Start customer engagement and communication.', N'whatsapp-platform', 0, 5),
    (@basicId, N'Basic Website', N'Get a simple web presence to represent your business online.', N'basic-website', 0, 6),
    (@basicId, N'Business Diagnostic + 30-Min Coach Session', N'A business health check with a 30-minute coaching session.', N'business-diagnostic', 0, 7),
    (@basicId, N'Practo Insurance Basic', N'Basic insurance cover as per plan offering.', N'practo-insurance', 0, 8),
    (@basicId, N'Free Credit Report', N'Basic credit visibility to support financial planning.', N'credit-report', 0, 9);
END

IF @premiumId IS NOT NULL
BEGIN
  DELETE FROM dbo.PlanFeatures WHERE PlanId = @premiumId;
  INSERT INTO dbo.PlanFeatures (PlanId, Text, Description, OfferingSlug, IsIncludesLine, SortOrder) VALUES
    (@premiumId, N'Includes all Basic Plan benefits', NULL, NULL, 1, 1),
    (@premiumId, N'Infomerics IVerified Report', N'Infomerics IVerified Report.', N'trust-score', 0, 2),
    (@premiumId, N'Company Scheme Discovery report', N'Personalized government scheme discovery.', N'scheme-discovery', 0, 3),
    (@premiumId, N'Bank Statement Analyzer', N'Analyzer for financial insights.', N'bank-statement-analyzer', 0, 4),
    (@premiumId, N'Free Relationship Manager', N'Dedicated support (as per plan offering).', N'relationship-manager', 0, 5),
    (@premiumId, N'Free Loan Audit', N'Audit support to optimize loan outcomes.', N'loan-audit', 0, 6),
    (@premiumId, N'Free Insurance Audit', N'Audit support to optimize insurance outcomes.', N'insurance-audit', 0, 7),
    (@premiumId, N'2 MSME Events Access', N'MSME events access included.', N'msme-events', 0, 8),
    (@premiumId, N'₹10 Lakh Insurance Cover', N'Insurance cover as per plan offering.', N'practo-insurance', 0, 9);
END

IF @proId IS NOT NULL
BEGIN
  DELETE FROM dbo.PlanFeatures WHERE PlanId = @proId;
  INSERT INTO dbo.PlanFeatures (PlanId, Text, Description, OfferingSlug, IsIncludesLine, SortOrder) VALUES
    (@proId, N'Includes all Premium Plan benefits', NULL, NULL, 1, 1),
    (@proId, N'Infomerics TrustScore Report', N'Infomerics TrustScore Report.', N'trust-score', 0, 2),
    (@proId, N'Company Scheme Discovery report', N'Personalized government scheme discovery.', N'scheme-discovery', 0, 3),
    (@proId, N'GeM Portal Registration & Support', N'Registration & support for GeM portal.', N'gem-portal', 0, 4),
    (@proId, N'Bank Statement Analyzer', N'Analyzer for financial insights.', N'bank-statement-analyzer', 0, 5),
    (@proId, N'4 MSME Events Access', N'More events access included.', N'msme-events', 0, 6),
    (@proId, N'Business Diagnostic + 30-Min Coach Session', N'A business health check with a 30-minute coaching session.', N'business-diagnostic', 0, 7),
    (@proId, N'₹15 Lakh Insurance Cover', N'Insurance cover as per plan offering.', N'practo-insurance', 0, 8),
    (@proId, N'Free Relationship Manager', N'Dedicated support (as per plan offering).', N'relationship-manager', 0, 9);
END

IF @standardId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.PlanFeatures WHERE PlanId = @standardId)
BEGIN
  INSERT INTO dbo.PlanFeatures (PlanId, Text, Description, OfferingSlug, IsIncludesLine, SortOrder) VALUES
    (@standardId, N'Website development', N'Business/portfolio/e-commerce website setup.', NULL, 0, 1),
    (@standardId, N'Hosting + server + DB', N'Includes hosting, server, and database support.', NULL, 0, 2),
    (@standardId, N'Mobile-friendly fast site', N'Responsive + WhatsApp & call buttons.', NULL, 0, 3),
    (@standardId, N'WhatsApp automation', N'FAQ automation and basic routing/notifications.', N'whatsapp-platform', 0, 4),
    (@standardId, N'Yearly maintenance', N'Setup + yearly hosting/maintenance.', NULL, 0, 5),
    (@standardId, N'Knowledge & events', N'Access MSME learning resources and events.', N'msme-events', 0, 6),
    (@standardId, N'Free credit report', N'Credit visibility to support growth planning.', N'credit-report', 0, 7);
END
GO
