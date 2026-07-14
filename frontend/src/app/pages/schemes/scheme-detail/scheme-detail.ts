import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { SchemesService } from '../../../core/services/schemes.service';
import { SeoService } from '../../../core/services/seo.service';
import type { SchemeModel, SchemeSlug } from '../../../data/schemes.data';
import { AnalyticsService } from '../../../core/services/analytics.service';

@Component({
  selector: 'app-scheme-detail',
  imports: [CommonModule, RouterLink],
  templateUrl: './scheme-detail.html'
})
export class SchemeDetail {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly schemes = inject(SchemesService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly seoService = inject(SeoService);
  private readonly analytics = inject(AnalyticsService);

  readonly slug = signal<string>('');
  readonly scheme = signal<SchemeModel | null>(null);

  readonly safeHtml = computed(() => {
    const sc = this.scheme();
    if (!sc) return null;
    return this.sanitizer.bypassSecurityTrustHtml(sc.content);
  });

  constructor() {
    this.route.paramMap.subscribe((p) => {
      const slug = p.get('slug') ?? '';
      this.slug.set(slug);

      const sc = this.schemes.getSchemeBySlug(slug);
      if (!sc) {
        void this.router.navigate(['/schemes']);
        return;
      }

      this.scheme.set(sc);
      this.setSeo(slug as SchemeSlug, sc);
      this.analytics.trackSchemeView(sc.crumb, slug);
    });
  }

  private setSeo(slug: SchemeSlug, sc: SchemeModel) {
    const pageTitle = `${sc.crumb} | Government Scheme | MSME Bharat Manch`;
    const canonicalUrl = `https://msmebharatmanch.com/scheme/${slug}`;

    this.seoService.setPage({
      title: pageTitle,
      description: sc.subtitle,
      ogUrl: canonicalUrl,
    });

    this.seoService.setJsonLd({
      '@context': 'https://schema.org',
      '@type': 'GovernmentService',
      'name': sc.crumb,
      'description': sc.subtitle,
      'url': canonicalUrl,
      'provider': {
        '@type': 'GovernmentOrganization',
        'name': 'Ministry of MSME, Government of India',
      },
      'serviceOperator': {
        '@type': 'Organization',
        'name': 'MSME Bharat Manch',
        'url': 'https://msmebharatmanch.com',
      },
      'areaServed': { '@type': 'Country', 'name': 'India' },
    });
  }
}

