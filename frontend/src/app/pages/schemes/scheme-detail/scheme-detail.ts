import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { DomSanitizer, Meta, Title } from '@angular/platform-browser';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { SchemesService } from '../../../core/services/schemes.service';
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
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
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
    const pageTitle = `${sc.crumb} | MSME Bharat Manch`;
    this.title.setTitle(pageTitle);
    this.meta.updateTag({ name: 'description', content: sc.subtitle });
    this.meta.updateTag({ property: 'og:type', content: 'website' });
    this.meta.updateTag({ property: 'og:title', content: pageTitle });
    this.meta.updateTag({ property: 'og:description', content: sc.subtitle });
    this.meta.updateTag({ property: 'og:url', content: `/scheme/${slug}` });
  }
}

