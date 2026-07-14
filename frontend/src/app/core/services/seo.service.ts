import { Injectable, inject } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { Title, Meta } from '@angular/platform-browser';

export interface SeoPageConfig {
  title: string;
  description: string;
  /** Defaults to 'website'. Use 'article' for blog/news entries. */
  ogType?: 'website' | 'article';
  /** Absolute canonical URL for this page (e.g. https://msmebharatmanch.com/service/slug). */
  ogUrl: string;
  /** Absolute URL of the social sharing image. Falls back to the site logo. */
  ogImage?: string;
}

const DEFAULT_OG_IMAGE = 'https://msmebharatmanch.com/assets/og-default.jpg';
/** id of the per-page JSON-LD script element so it can be upserted / removed. */
const JSONLD_ID = 'jsonld-page';

/**
 * Centralised SEO helper used by dynamic-route components (ServicesDetails,
 * EventDetail, SchemeDetail, OfferingDetails, ArticleDetail) AND by App for
 * static routes declared in app.routes.ts `data`.
 *
 * Site-wide invariants (og:site_name, og:locale, twitter:card) are set once
 * in index.html and are never touched here.
 */
@Injectable({ providedIn: 'root' })
export class SeoService {
  private readonly titleService = inject(Title);
  private readonly meta = inject(Meta);
  private readonly doc = inject(DOCUMENT);

  /** Updates all page-specific meta tags: title, description, Open Graph and Twitter Card. */
  setPage(config: SeoPageConfig): void {
    const image = config.ogImage ?? DEFAULT_OG_IMAGE;

    this.titleService.setTitle(config.title);
    this.meta.updateTag({ name: 'description', content: config.description });

    // Open Graph — page-specific (og:site_name & og:locale live in index.html)
    this.meta.updateTag({ property: 'og:type', content: config.ogType ?? 'website' });
    this.meta.updateTag({ property: 'og:title', content: config.title });
    this.meta.updateTag({ property: 'og:description', content: config.description });
    this.meta.updateTag({ property: 'og:url', content: config.ogUrl });
    this.meta.updateTag({ property: 'og:image', content: image });

    // Twitter Card — page-specific values (twitter:card & twitter:site in index.html)
    this.meta.updateTag({ name: 'twitter:title', content: config.title });
    this.meta.updateTag({ name: 'twitter:description', content: config.description });
    this.meta.updateTag({ name: 'twitter:image', content: image });
  }

  /**
   * Upserts a <script type="application/ld+json"> element in <head> for
   * page-specific structured data (Article, Event, Service, etc.).
   * Calling this again on the same page replaces the previous schema.
   */
  setJsonLd(schema: Record<string, unknown>): void {
    let el = this.doc.getElementById(JSONLD_ID) as HTMLScriptElement | null;
    if (!el) {
      el = this.doc.createElement('script');
      el.type = 'application/ld+json';
      el.id = JSONLD_ID;
      this.doc.head.appendChild(el);
    }
    el.textContent = JSON.stringify(schema);
  }

  /**
   * Removes the page-specific JSON-LD element.
   * Called by App when navigating to a static route that has no schema.
   */
  removeJsonLd(): void {
    this.doc.getElementById(JSONLD_ID)?.remove();
  }
}
