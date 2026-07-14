import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { JoinCtaService } from '../../../core/services/join-cta.service';
import { EventsService } from '../../../core/services/events.service';
import { SeoService } from '../../../core/services/seo.service';
import type { EventModel, EventSlug } from '../../../data/events.data';

@Component({
  selector: 'app-event-detail',
  imports: [CommonModule, RouterLink],
  templateUrl: './event-detail.html'
})
export class EventDetail {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly events = inject(EventsService);
  readonly joinCta = inject(JoinCtaService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly seoService = inject(SeoService);

  readonly slug = signal<string>('');
  readonly event = signal<EventModel | null>(null);

  readonly safeBody = computed(() => {
    const ev = this.event();
    if (!ev) return null;
    return this.sanitizer.bypassSecurityTrustHtml(ev.body);
  });

  constructor() {
    this.route.paramMap.subscribe((p) => {
      const slug = p.get('slug') ?? '';
      this.slug.set(slug);

      const ev = this.events.getEventBySlug(slug);
      if (!ev) {
        void this.router.navigate(['/events']);
        return;
      }

      this.event.set(ev);
      this.setSeo(slug as EventSlug, ev);
    });
  }

  private setSeo(slug: EventSlug, ev: EventModel) {
    const pageTitle = `${ev.title} | MSME Bharat Manch`;
    const canonicalUrl = `https://msmebharatmanch.com/event/${slug}`;
    const ogImage = ev.imageUrl
      ? `https://msmebharatmanch.com${ev.imageUrl}`
      : undefined;

    this.seoService.setPage({
      title: pageTitle,
      description: ev.subtitle,
      ogUrl: canonicalUrl,
      ogImage,
    });

    this.seoService.setJsonLd({
      '@context': 'https://schema.org',
      '@type': 'Event',
      'name': ev.title,
      'description': ev.subtitle,
      'startDate': ev.dateISO,
      'location': {
        '@type': 'Place',
        'name': ev.venue,
        'address': { '@type': 'PostalAddress', 'addressCountry': 'IN' },
      },
      'organizer': {
        '@type': 'Organization',
        'name': 'MSME Bharat Manch',
        'url': 'https://msmebharatmanch.com',
      },
      'url': canonicalUrl,
      'eventStatus': 'https://schema.org/EventScheduled',
      'eventAttendanceMode': 'https://schema.org/OfflineEventAttendanceMode',
      ...(ogImage ? { 'image': ogImage } : {}),
    });
  }
}

