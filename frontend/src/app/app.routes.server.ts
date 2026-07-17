import { RenderMode, ServerRoute } from '@angular/ssr';

/**
 * Server-side rendering strategy per route.
 *
 * - RenderMode.Prerender  → HTML generated at build time (static file served by .NET)
 * - RenderMode.Client     → No prerender; .NET falls back to base index.html and
 *                           Angular bootstraps in the browser (auth-protected pages,
 *                           login, register, admin).
 *
 * Order matters: the first matching path wins.
 * Parameterised Prerender routes supply getPrerenderParams() so the builder
 * knows every slug to generate.
 */
export const serverRoutes: ServerRoute[] = [

  // ── Auth-only / user-session routes (no prerender value) ──────────────────
  { path: 'login',                renderMode: RenderMode.Client },
  { path: 'register',             renderMode: RenderMode.Client },
  { path: 'forgot-password',      renderMode: RenderMode.Client },
  { path: 'profile',              renderMode: RenderMode.Client },
  { path: 'my-plan',              renderMode: RenderMode.Client },

  // ── Admin routes ───────────────────────────────────────────────────────────
  { path: 'admin-login',                               renderMode: RenderMode.Client },
  { path: 'admin-forgot-password',                     renderMode: RenderMode.Client },
  { path: 'admin-dashboard',                           renderMode: RenderMode.Client },
  { path: 'admin-dashboard/detail/:category',          renderMode: RenderMode.Client },
  { path: 'admin/user-management/:role',               renderMode: RenderMode.Client },
  { path: 'admin/user-management/:role/:userId',       renderMode: RenderMode.Client },
  { path: 'admin-reports/upload',                      renderMode: RenderMode.Client },
  { path: 'admin-reports/sdr',                         renderMode: RenderMode.Client },
  { path: 'admin-reports/history',                     renderMode: RenderMode.Client },
  { path: 'admin/vendor-management/vendors',           renderMode: RenderMode.Client },
  { path: 'admin/vendor-management/vendors/:vendorId', renderMode: RenderMode.Client },
  { path: 'admin/vendor-management/plan-mapping',      renderMode: RenderMode.Client },
  { path: 'admin/lead-attribution',                    renderMode: RenderMode.Client },
  { path: 'admin/lead-attribution/customers',          renderMode: RenderMode.Client },
  { path: 'admin/lead-attribution/customers/:userId',  renderMode: RenderMode.Client },
  { path: 'admin/enquiry-management/enquiries',              renderMode: RenderMode.Client },
  { path: 'admin/enquiry-management/enquiries/:enquiryId',   renderMode: RenderMode.Client },
  { path: 'admin/enquiry-management/new',                    renderMode: RenderMode.Client },
  { path: 'admin/enquiry-management/resolved',               renderMode: RenderMode.Client },
  { path: 'admin/enquiry-management/closed',                 renderMode: RenderMode.Client },
  { path: 'admin/credit-repair/leads',                       renderMode: RenderMode.Client },
  { path: 'admin/connect-management/listings',               renderMode: RenderMode.Client },
  { path: 'admin/bulk-member-import',                        renderMode: RenderMode.Client },
  { path: 'admin/blog-management',                           renderMode: RenderMode.Client },
  { path: 'admin/blog-management/new',                       renderMode: RenderMode.Client },
  { path: 'admin/blog-management/edit/:blogId',              renderMode: RenderMode.Client },
  { path: 'admin/blog-categories',                           renderMode: RenderMode.Client },
  { path: 'admin/blog-categories/new',                       renderMode: RenderMode.Client },
  { path: 'admin/blog-categories/edit/:categoryId',          renderMode: RenderMode.Client },
  { path: 'admin/blog-badges',                              renderMode: RenderMode.Client },
  { path: 'admin/blog-badges/new',                          renderMode: RenderMode.Client },
  { path: 'admin/blog-badges/edit/:badgeId',                renderMode: RenderMode.Client },

  // ── Legacy redirect with param — keep as Client (redirects to service/:slug) ─
  { path: 'services/:slug', renderMode: RenderMode.Client },

  // ── Dynamic public routes — Prerender all known slugs ─────────────────────

  {
    path: 'service/:slug',
    renderMode: RenderMode.Prerender,
    async getPrerenderParams() {
      return [
        { slug: 'msme-loans-finance' },
        { slug: 'company-formation' },
        { slug: 'government-subsidies' },
        { slug: 'technology-solutions' },
        { slug: 'sales-digital-marketing' },
        { slug: 'ipo-investment-banking' },
        { slug: 'coaching-consulting' },
        { slug: 'legal-compliance' },
        { slug: 'research-development' },
        { slug: 'export-import' },
        { slug: 'hr-recruitment' },
        { slug: 'accounting-cfo' },
      ];
    },
  },

  {
    path: 'scheme/:slug',
    renderMode: RenderMode.Prerender,
    async getPrerenderParams() {
      return [
        { slug: 'pmegp' },
        { slug: 'cgtmse' },
        { slug: 'mudra' },
        { slug: 'digital-msme' },
        { slug: 'trade-fair' },
        { slug: 'design-clinic' },
      ];
    },
  },

  {
    path: 'event/:slug',
    renderMode: RenderMode.Prerender,
    async getPrerenderParams() {
      return [
        { slug: 'bll-business-topline-growth-meet-2026' },
        { slug: 'bll-psu-sme-access-summit-2026' },
      ];
    },
  },

  // Articles are now dynamic (served from the database).
  // Client-side rendering lets the page load any slug at runtime.
  { path: 'article/:slug', renderMode: RenderMode.Client },

  {
    path: 'offering/:slug',
    renderMode: RenderMode.Prerender,
    async getPrerenderParams() {
      return [
        { slug: 'trust-score' },
        { slug: 'business-diagnostic' },
        { slug: 'scheme-discovery' },
        { slug: 'loan-audit' },
        { slug: 'bank-statement-analyzer' },
        { slug: 'whatsapp-platform' },
        { slug: 'gem-registration' },
        { slug: 'basic-website' },
        { slug: 'practo-insurance' },
        { slug: 'credit-report' },
        { slug: 'relationship-manager' },
        { slug: 'insurance-audit' },
        { slug: 'msme-events' },
      ];
    },
  },

  // ── All remaining public routes → Prerender ────────────────────────────────
  // Covers: /, home, about, our-services, loans, credit-rebuild, contact,
  //         membership, connect, news, careers, media, privacy-policy,
  //         terms, disclaimer, schemes, events, search, and redirect stubs.
  { path: '**', renderMode: RenderMode.Prerender },
];
