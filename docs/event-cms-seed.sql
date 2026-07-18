/* =========================================================
   Seed: recover original public events (run on MBM database)
   Safe to re-run — skips rows that already exist by Slug.
   ========================================================= */

/* Categories */
IF NOT EXISTS (SELECT 1 FROM dbo.EventCategories WHERE Slug = N'bll')
BEGIN
    INSERT INTO dbo.EventCategories (Slug, Name, ShortDescription, SortOrder, IsActive, ShowInFilter, CreatedAt, UpdatedAt)
    VALUES (N'bll', N'BLL', N'Business Leaders League events', 1, 1, 1, GETDATE(), GETDATE());
END

IF NOT EXISTS (SELECT 1 FROM dbo.EventCategories WHERE Slug = N'others')
BEGIN
    INSERT INTO dbo.EventCategories (Slug, Name, ShortDescription, SortOrder, IsActive, ShowInFilter, CreatedAt, UpdatedAt)
    VALUES (N'others', N'Others', N'Other events and webinars', 99, 1, 1, GETDATE(), GETDATE());
END

/* City */
IF NOT EXISTS (SELECT 1 FROM dbo.EventCities WHERE Slug = N'mumbai')
BEGIN
    INSERT INTO dbo.EventCities (Slug, Name, BadgeClass, SortOrder, IsActive, CreatedAt, UpdatedAt)
    VALUES (N'mumbai', N'Mumbai', N'badge-green', 1, 1, GETDATE(), GETDATE());
END

/* Event 1 */
IF NOT EXISTS (SELECT 1 FROM dbo.Events WHERE Slug = N'bll-business-topline-growth-meet-2026')
BEGIN
    INSERT INTO dbo.Events (
        Slug, Name, Crumb, Tagline, ShortDescription,
        AboutHtml, HighlightsHtml, CategorySlug, CitySlug,
        FeaturedImageUrl, DateDisplayText, TimeDisplayText,
        StartDate, EndDate, DateISO, AttendanceMode,
        LocationDisplayText, VenueName, VenueAddress, Landmark, CityName, State, Country,
        PriceDisplay, RegNote, IsPublished, IsFeatured, SortOrder, CreatedAt, UpdatedAt
    )
    VALUES (
        N'bll-business-topline-growth-meet-2026',
        N'Business Topline Growth Meet — 23rd Edition',
        N'BLL Growth Meet',
        N'AAOGE TOH PAAOGE — 27th June 2026, Bombay Stock Exchange, Mumbai',
        N'Join business leaders at the Bombay Stock Exchange for BLL''s flagship growth meet — keynote sessions, networking, and actionable topline growth strategies. AAOGE TOH PAAOGE.',
        N'<p>Join business leaders and entrepreneurs at the prestigious Bombay Stock Exchange for the 23rd edition of BLL''s flagship Business Topline Growth Meet. Gain actionable insights on driving revenue growth, connect with industry peers, and learn proven strategies to scale your business topline under the theme AAOGE TOH PAAOGE.</p>',
        N'',
        N'bll',
        N'mumbai',
        N'/event/bll-event-1.jpg',
        N'27th June 2026',
        N'2:30 PM onwards',
        '2026-06-27',
        '2026-06-27',
        N'2026-06-27',
        N'Offline',
        N'Bombay Stock Exchange, Mumbai',
        N'Bombay Stock Exchange',
        N'',
        N'',
        N'Mumbai',
        N'Maharashtra',
        N'India',
        N'₹1,499 + GST',
        N'Register at www.bll.org.in or scan the QR code. Tickets at ₹1,499 + GST.',
        1, 1, 1, GETDATE(), GETDATE()
    );

    DECLARE @E1 INT = SCOPE_IDENTITY();

    INSERT INTO dbo.EventHighlights (EventId, Text, SortOrder) VALUES
    (@E1, N'Keynote sessions from industry experts on business growth', 1),
    (@E1, N'Networking with business leaders and entrepreneurs', 2),
    (@E1, N'Actionable topline growth strategies', 3),
    (@E1, N'Held at the iconic Bombay Stock Exchange, Mumbai', 4);
END

/* Event 2 */
IF NOT EXISTS (SELECT 1 FROM dbo.Events WHERE Slug = N'bll-psu-sme-access-summit-2026')
BEGIN
    INSERT INTO dbo.Events (
        Slug, Name, Crumb, Tagline, ShortDescription,
        AboutHtml, HighlightsHtml, CategorySlug, CitySlug,
        FeaturedImageUrl, DateDisplayText, TimeDisplayText,
        StartDate, EndDate, DateISO, AttendanceMode,
        LocationDisplayText, VenueName, VenueAddress, Landmark, CityName, State, Country,
        PriceDisplay, RegNote, IsPublished, IsFeatured, SortOrder, CreatedAt, UpdatedAt
    )
    VALUES (
        N'bll-psu-sme-access-summit-2026',
        N'Procurement Series: PSU SME Access Summit',
        N'PSU SME Access Summit',
        N'Thousands of Crores in PSU Tenders — 9th July 2026, Jio World Convention Centre, BKC, Mumbai',
        N'Connect your SME with India''s largest PSUs — ONGC, IndianOil, NTPC & more — and unlock procurement opportunities worth thousands of crores in PSU tenders.',
        N'<p>Get your SME in front of India''s biggest buyers. This exclusive summit connects Small and Medium Enterprises with major Public Sector Undertakings including ONGC, IndianOil, NTPC, Bhakra Beas Management Board, and Western Railway — unlocking access to procurement opportunities worth thousands of crores.</p>',
        N'',
        N'bll',
        N'mumbai',
        N'/event/bll-event-2.jpg',
        N'9th July 2026',
        N'',
        '2026-07-09',
        '2026-07-09',
        N'2026-07-09',
        N'Offline',
        N'Jio World Convention Centre, BKC, Mumbai',
        N'Jio World Convention Centre',
        N'',
        N'BKC',
        N'Mumbai',
        N'Maharashtra',
        N'India',
        N'Limited Seats',
        N'Limited seats available. Apply for seats now via BLL.',
        1, 1, 2, GETDATE(), GETDATE()
    );

    DECLARE @E2 INT = SCOPE_IDENTITY();

    INSERT INTO dbo.EventHighlights (EventId, Text, SortOrder) VALUES
    (@E2, N'PSU Procurement Strategy & Vendor Empanelment Process', 1),
    (@E2, N'Open Tenders & Upcoming Pipeline for FY 2026–27', 2),
    (@E2, N'How to qualify, register, and get shortlisted as a vendor', 3),
    (@E2, N'Open to SMEs in Manufacturing, Services, Technology, Sustainability & Infrastructure', 4);

    INSERT INTO dbo.EventPartners (EventId, Name, LogoUrl, WebsiteUrl, SortOrder) VALUES
    (@E2, N'MSME Development & Facilitation Office, Mumbai, Ministry of MSME, Govt. of India', NULL, NULL, 1);
END
GO
