import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { SchemeDiscoveryFlowService } from '../../core/services/scheme-discovery-flow.service';
import { DomSanitizer, Meta, Title } from '@angular/platform-browser';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { OfferingCtaActions } from '../../core/components/offering-cta-actions/offering-cta-actions';
import { OfferingsService } from '../../core/services/offerings.service';
import type { OfferingModel, OfferingSlug } from '../../data/offerings.data';

@Component({
  selector: 'app-offering-details',
  standalone: true,
  imports: [CommonModule, RouterLink, OfferingCtaActions],
  templateUrl: './offering-details.html',
  styleUrl: './offering-details.css'
})
export class OfferingDetails implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly offerings = inject(OfferingsService);
  private readonly schemeDiscovery = inject(SchemeDiscoveryFlowService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);

  readonly slug = signal<string>('');
  readonly offering = signal<OfferingModel | null>(null);

  readonly safeContent = computed(() => {
    const off = this.offering();
    if (!off) return null;
    return this.sanitizer.bypassSecurityTrustHtml(off.content);
  });

  readonly includedPlans = computed(() => {
    const off = this.offering();
    if (!off) return [];

    const planColors: Record<string, string> = {
      'MSME Basic': '#e8f5e9',
      'MSME Standard': '#e3f2fd',
      'MSME Premium': '#fff3e0',
      'MSME Pro': '#ede7f6',
      'MSME Premium (2 events)': '#fff3e0',
      'MSME Pro (4 events)': '#ede7f6'
    };
    const planIcons: Record<string, string> = {
      'MSME Basic': '🌱',
      'MSME Standard': '🌐',
      'MSME Premium': '🏆',
      'MSME Pro': '💎',
      'MSME Premium (2 events)': '🏆',
      'MSME Pro (4 events)': '💎'
    };

    return off.plans.map((p) => ({
      name: p,
      bg: planColors[p] ?? '#f5f5f5',
      icon: planIcons[p] ?? '✓'
    }));
  });

  ngOnInit(): void {
    void this.schemeDiscovery.tryResumeOnPageLoad();
  }

  constructor() {
    this.route.paramMap.subscribe((p) => {
      const slug = p.get('slug') ?? '';
      this.slug.set(slug);

      const off = this.offerings.getOfferingBySlug(slug);
      if (!off) {
        void this.router.navigate(['/membership']);
        return;
      }

      this.offering.set(off);
      this.setSeo(slug as OfferingSlug, off);
    });
  }

  private setSeo(slug: OfferingSlug, off: OfferingModel) {
    const pageTitle = `${off.title} | MSME Bharat Manch`;
    this.title.setTitle(pageTitle);
    this.meta.updateTag({ name: 'description', content: off.tagline });
    this.meta.updateTag({ property: 'og:type', content: 'website' });
    this.meta.updateTag({ property: 'og:title', content: pageTitle });
    this.meta.updateTag({ property: 'og:description', content: off.tagline });
    this.meta.updateTag({ property: 'og:url', content: `/offering/${slug}` });
  }
}
