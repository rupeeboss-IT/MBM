import { CommonModule } from '@angular/common';
import { Component, OnDestroy, computed, inject, signal } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subscription, firstValueFrom } from 'rxjs';
import { SchemesService, type PublicScheme } from '../../../core/services/schemes.service';
import { SeoService } from '../../../core/services/seo.service';
import { AnalyticsService } from '../../../core/services/analytics.service';

@Component({
  selector: 'app-scheme-detail',
  imports: [CommonModule, RouterLink],
  templateUrl: './scheme-detail.html',
})
export class SchemeDetail implements OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly schemes = inject(SchemesService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly seoService = inject(SeoService);
  private readonly analytics = inject(AnalyticsService);

  readonly slug = signal<string>('');
  readonly scheme = signal<PublicScheme | null>(null);
  readonly loading = signal(true);

  readonly safeHtml = computed(() => {
    const sc = this.scheme();
    if (!sc?.contentHtml) return null;
    return this.sanitizer.bypassSecurityTrustHtml(sc.contentHtml);
  });

  private sub: Subscription;

  constructor() {
    this.sub = this.route.paramMap.subscribe((p) => {
      const slug = p.get('slug') ?? '';
      this.slug.set(slug);
      void this.load(slug);
    });
  }

  ngOnDestroy() {
    this.sub.unsubscribe();
  }

  private async load(slug: string) {
    if (!slug) {
      void this.router.navigate(['/schemes']);
      return;
    }

    this.loading.set(true);
    try {
      const sc = await firstValueFrom(this.schemes.getPublishedBySlug(slug));
      if (!sc) {
        void this.router.navigate(['/schemes']);
        return;
      }
      this.scheme.set(sc);
      this.setSeo(slug, sc);
      this.analytics.trackSchemeView(sc.crumb, slug);
    } catch {
      void this.router.navigate(['/schemes']);
    } finally {
      this.loading.set(false);
    }
  }

  private setSeo(slug: string, sc: PublicScheme) {
    const pageTitle =
      sc.seoTitle?.trim() || `${sc.crumb} | Government Scheme | MSME Bharat Manch`;
    const description = sc.metaDescription?.trim() || sc.subtitle;
    const canonicalUrl = `https://msmebharatmanch.com/scheme/${slug}`;

    this.seoService.setPage({
      title: pageTitle,
      description,
      ogUrl: canonicalUrl,
    });

    this.seoService.setJsonLd({
      '@context': 'https://schema.org',
      '@type': 'GovernmentService',
      name: sc.crumb,
      description: sc.subtitle,
      url: canonicalUrl,
      provider: {
        '@type': 'GovernmentOrganization',
        name: 'Ministry of MSME, Government of India',
      },
      serviceOperator: {
        '@type': 'Organization',
        name: 'MSME Bharat Manch',
        url: 'https://msmebharatmanch.com',
      },
      areaServed: { '@type': 'Country', name: 'India' },
    });
  }
}
