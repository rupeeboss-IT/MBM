import { Component, PLATFORM_ID, OnInit, inject } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { SeoService } from './core/services/seo.service';
import { MembershipCheckoutModals } from './core/components/membership-checkout-modals/membership-checkout-modals';
import { SchemeDiscoveryModals } from './core/components/scheme-discovery-modals/scheme-discovery-modals';
import { MsmeSaathiChat } from './core/components/msme-saathi-chat/msme-saathi-chat';
import { ToastContainer } from './core/components/toast-container/toast-container';
import { captureContactLeadSourceFromUrl } from './core/utils/contact-lead-source.util';
import { captureRegistrationAdvisorFromUrl } from './core/utils/registration-advisor.util';
import { captureRegistrationLeadSourceFromUrl } from './core/utils/registration-lead-source.util';
import { captureRegistrationModeFromUrl } from './core/utils/registration-mode.util';
import { Header } from "./header/header";
import { Footer } from "./footer/footer";
import { CookieConsentBanner } from './core/components/cookie-consent-banner/cookie-consent-banner';
import { AnalyticsService } from './core/services/analytics.service';
import { CookieConsentService } from './core/services/cookie-consent.service';

/** Canonical origin — must be absolute and match the production domain. */
const SITE_ORIGIN = 'https://msmebharatmanch.com';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrl: './app.css',
  imports: [Header, Footer, RouterOutlet, ToastContainer, SchemeDiscoveryModals, MembershipCheckoutModals, MsmeSaathiChat, CookieConsentBanner]
})
export class App implements OnInit {
  private readonly analytics = inject(AnalyticsService);
  private readonly cookieConsent = inject(CookieConsentService);
  private readonly seoService = inject(SeoService);
  private readonly doc = inject(DOCUMENT);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly platformId = inject(PLATFORM_ID);

  constructor(private readonly router: Router) {}

  ngOnInit(): void {
    // Apply route SEO on initial load — runs on both server (SSR/prerender) and browser
    // so prerendered HTML gets route-specific title, description and canonical tag.
    this.applyRouteSeo(this.router.url);

    // Everything below is browser-only (analytics, lead capture, SPA navigation hooks)
    if (!isPlatformBrowser(this.platformId)) return;

    // Re-initialize GA if the user already consented in a previous session
    if (this.cookieConsent.hasConsented()) {
      this.analytics.initialize();
    }

    this.captureLeadSourcesFromUrl(window.location.search);

    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe((e) => {
        // Update SEO tags on every SPA navigation
        this.applyRouteSeo(e.urlAfterRedirects);

        const query = e.urlAfterRedirects.includes('?')
          ? e.urlAfterRedirects.slice(e.urlAfterRedirects.indexOf('?'))
          : '';
        this.captureLeadSourcesFromUrl(query);
        this.analytics.trackPageView(e.urlAfterRedirects);
      });
  }

  /**
   * Sets canonical + (for static routes) all SEO meta tags for the given URL.
   *
   * For static routes (those with data.title in app.routes.ts): delegates
   * to SeoService.setPage() which sets title, description, all OG and Twitter
   * tags, then removes any stale page-specific JSON-LD left by a prior dynamic
   * route.
   *
   * For dynamic routes (service/:slug, article/:slug, etc.): the component's
   * constructor calls SeoService.setPage() with page-specific data BEFORE
   * NavigationEnd fires (Angular creates components before emitting NavigationEnd),
   * so we do not touch those tags here — the component's values win.
   *
   * The canonical <link> is always maintained here regardless of route type.
   */
  private applyRouteSeo(url: string): void {
    const path = url.split('?')[0].split('#')[0] || '/';
    const data = this.deepestRouteData();

    if (data['title']) {
      // Static route — SeoService sets title, description, OG and Twitter in one call
      this.seoService.setPage({
        title: data['title'] as string,
        description: (data['description'] as string) ?? '',
        ogUrl: `${SITE_ORIGIN}${path}`,
      });
      // Remove stale page-specific JSON-LD (Event, Article, Service …) left by
      // a previous dynamic route so crawlers don't see mismatched structured data.
      this.seoService.removeJsonLd();
    }

    this.setCanonical(`${SITE_ORIGIN}${path}`);
  }

  /** Returns the snapshot data of the deepest active child route. */
  private deepestRouteData(): Record<string, unknown> {
    let route = this.activatedRoute;
    while (route.firstChild) {
      route = route.firstChild;
    }
    return (route.snapshot?.data ?? {}) as Record<string, unknown>;
  }

  /** Upserts a single <link rel="canonical"> element in <head>. */
  private setCanonical(href: string): void {
    let el = this.doc.querySelector<HTMLLinkElement>('link[rel="canonical"]');
    if (!el) {
      el = this.doc.createElement('link');
      el.setAttribute('rel', 'canonical');
      this.doc.head.appendChild(el);
    }
    el.setAttribute('href', href);
  }

  private captureLeadSourcesFromUrl(search: string): void {
    captureRegistrationLeadSourceFromUrl(search);
    captureRegistrationAdvisorFromUrl(search);
    captureContactLeadSourceFromUrl(search);
    captureRegistrationModeFromUrl(search);
  }
}
