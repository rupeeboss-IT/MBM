import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

// eslint-disable-next-line @typescript-eslint/no-explicit-any
declare let gtag: (...args: any[]) => void;

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private initialized = false;

  /**
   * Loads the GA4 gtag.js script and configures the measurement ID.
   * Safe to call multiple times — subsequent calls are no-ops.
   * Should be called after the user consents to cookies, or immediately
   * on app start if consent was previously given.
   */
  initialize(): void {
    if (this.initialized || !environment.gaMeasurementId || typeof window === 'undefined') return;
    this.initialized = true;

    // Inject the async gtag.js script
    const script = document.createElement('script');
    script.async = true;
    script.src = `https://www.googletagmanager.com/gtag/js?id=${environment.gaMeasurementId}`;
    document.head.appendChild(script);

    // Bootstrap dataLayer and gtag function.
    // MUST use a regular function (not arrow) and push `arguments` (not a rest-param array).
    // gtag.js checks instanceof IArguments internally — plain arrays are silently ignored.
    const win = window as unknown as Record<string, unknown>;
    win['dataLayer'] = win['dataLayer'] ?? [];
    // eslint-disable-next-line prefer-rest-params
    win['gtag'] = function () { (win['dataLayer'] as unknown[]).push(arguments); };

    gtag('js', new Date());
    gtag('config', environment.gaMeasurementId, {
      // We fire page_view manually on every NavigationEnd
      send_page_view: false,
    });
  }

  // ─── Page tracking ────────────────────────────────────────────────────────

  /** Fire a page_view hit — called automatically on every route change from app.ts */
  trackPageView(urlPath: string, pageTitle?: string): void {
    if (!this.canTrack()) return;
    gtag('event', 'page_view', {
      page_path: urlPath,
      page_title: pageTitle ?? (typeof document !== 'undefined' ? document.title : ''),
      page_location: typeof window !== 'undefined' ? window.location.href : '',
    });
  }

  // ─── Generic event ────────────────────────────────────────────────────────

  /** Low-level event — use the typed helpers below whenever possible */
  trackEvent(eventName: string, params?: Record<string, unknown>): void {
    if (!this.canTrack()) return;
    gtag('event', eventName, params ?? {});
  }

  // ─── Lead generation ─────────────────────────────────────────────────────

  /** Contact form successfully submitted */
  trackContactFormSubmit(subject?: string): void {
    this.trackEvent('generate_lead', { form_type: 'contact', subject });
  }

  /** Loan enquiry form successfully submitted */
  trackLoanEnquiry(loanType?: string): void {
    this.trackEvent('generate_lead', { form_type: 'loan_enquiry', loan_type: loanType });
  }

  /** Credit rebuild enquiry form successfully submitted */
  trackCreditRebuildEnquiry(audience?: string): void {
    this.trackEvent('generate_lead', { form_type: 'credit_rebuild', audience });
  }

  // ─── Auth ─────────────────────────────────────────────────────────────────

  /** User started filling the registration form */
  trackRegistrationStarted(role?: string): void {
    this.trackEvent('registration_started', { method: 'email', role });
  }

  /** User successfully completed registration */
  trackRegistrationCompleted(role?: string): void {
    this.trackEvent('sign_up', { method: 'email', role });
  }

  /** User successfully logged in */
  trackLogin(method = 'email'): void {
    this.trackEvent('login', { method });
  }

  // ─── Membership ───────────────────────────────────────────────────────────

  /** Membership plan card viewed */
  trackMembershipView(planCode: string): void {
    this.trackEvent('view_item', {
      item_name: planCode,
      item_category: 'membership',
    });
  }

  /** User initiated checkout for a membership plan */
  trackMembershipCheckoutStarted(planCode: string): void {
    this.trackEvent('begin_checkout', {
      currency: 'INR',
      items: [{ item_name: planCode, item_category: 'membership' }],
    });
  }

  /** Membership payment completed successfully */
  trackMembershipPurchase(planCode: string, value: number, transactionId?: string): void {
    this.trackEvent('purchase', {
      transaction_id: transactionId ?? '',
      currency: 'INR',
      value,
      items: [{ item_name: planCode, item_category: 'membership' }],
    });
  }

  // ─── Content ──────────────────────────────────────────────────────────────

  /** Government scheme detail page viewed */
  trackSchemeView(schemeName: string, schemeSlug: string): void {
    this.trackEvent('view_item', {
      item_name: schemeName,
      item_id: schemeSlug,
      item_category: 'scheme',
    });
  }

  /** Search performed */
  trackSearch(searchTerm: string): void {
    this.trackEvent('search', { search_term: searchTerm });
  }

  // ─── Engagement ───────────────────────────────────────────────────────────

  /** CTA button clicked (e.g. "Join Today", "Apply Now") */
  trackCTAClick(ctaLabel: string, pagePath?: string): void {
    this.trackEvent('cta_click', {
      cta_label: ctaLabel,
      page_path: pagePath ?? (typeof window !== 'undefined' ? window.location.pathname : ''),
    });
  }

  /** MSME Saathi chatbot opened */
  trackChatbotOpen(): void {
    this.trackEvent('chatbot_open', { chatbot_name: 'msme_saathi' });
  }

  /** Scheme discovery flow started */
  trackSchemeDiscoveryStart(): void {
    this.trackEvent('scheme_discovery_start');
  }

  /** File / report downloaded */
  trackFileDownload(fileName: string, fileCategory?: string): void {
    this.trackEvent('file_download', { file_name: fileName, file_category: fileCategory });
  }

  // ─── Internal ─────────────────────────────────────────────────────────────

  private canTrack(): boolean {
    return this.initialized && !!environment.gaMeasurementId && typeof gtag === 'function';
  }
}
