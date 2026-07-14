import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ServicesService } from '../../../core/services/services.service';
import { SeoService } from '../../../core/services/seo.service';
import type { ServiceModel, ServiceSlug } from '../../../data/services.data';

@Component({
  selector: 'app-services-details',
  imports: [CommonModule, RouterLink],
  templateUrl: './services-details.html',
  styleUrl: './services-details.css',
})
export class ServicesDetails {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly services = inject(ServicesService);
  private readonly seoService = inject(SeoService);

  readonly slug = signal<string>('');
  readonly svc = signal<ServiceModel | null>(null);

  readonly related = computed(() => {
    const svc = this.svc();
    if (!svc?.related?.length) return [];
    return svc.related
      .map((slug) => {
        const data = this.services.getServiceBySlug(slug);
        return data ? ({ slug: slug as ServiceSlug, data } satisfies { slug: ServiceSlug; data: ServiceModel }) : null;
      })
      .filter((x): x is { slug: ServiceSlug; data: ServiceModel } => x !== null);
  });

  constructor() {
    this.route.paramMap.subscribe((p) => {
      const slug = p.get('slug') ?? '';
      this.slug.set(slug);

      const svc = this.services.getServiceBySlug(slug);
      if (!svc) {
        void this.router.navigate(['/our-services']);
        return;
      }

      this.svc.set(svc);
      this.setSeo(slug, svc);
    });
  }

  splitTarget(target: string): { icon: string; text: string } {
    const idx = target.indexOf(' ');
    if (idx === -1) return { icon: target, text: '' };
    return { icon: target.slice(0, idx), text: target.slice(idx + 1) };
  }

  private setSeo(slug: string, svc: ServiceModel) {
    const pageTitle = `${svc.title} | MSME Bharat Manch`;
    const canonicalUrl = `https://msmebharatmanch.com/service/${slug}`;

    this.seoService.setPage({
      title: pageTitle,
      description: svc.subtitle,
      ogUrl: canonicalUrl,
    });

    this.seoService.setJsonLd({
      '@context': 'https://schema.org',
      '@type': 'Service',
      'name': svc.title,
      'description': svc.subtitle,
      'url': canonicalUrl,
      'provider': {
        '@type': 'Organization',
        'name': 'MSME Bharat Manch',
        'url': 'https://msmebharatmanch.com',
      },
      'areaServed': { '@type': 'Country', 'name': 'India' },
      'serviceType': 'MSME Business Services',
    });
  }
}
