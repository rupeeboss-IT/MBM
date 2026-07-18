import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { JoinCtaService } from '../../../core/services/join-cta.service';
import { EventsService, type PublicEvent } from '../../../core/services/events.service';
import { SeoService } from '../../../core/services/seo.service';

@Component({
  selector: 'app-event-detail',
  imports: [CommonModule, RouterLink],
  templateUrl: './event-detail.html',
  styleUrl: './event-detail.css',
})
export class EventDetail {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly events = inject(EventsService);
  readonly joinCta = inject(JoinCtaService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly seoService = inject(SeoService);

  readonly slug = signal<string>('');
  readonly event = signal<PublicEvent | null>(null);
  readonly loading = signal(true);

  readonly safeAbout = computed(() => {
    const html = stripLeadingSectionHeading(this.event()?.aboutHtml, 'About the Event');
    if (!html) return null;
    return this.sanitizer.bypassSecurityTrustHtml(html);
  });

  readonly safeHighlightsHtml = computed(() => {
    const html = stripLeadingSectionHeading(this.event()?.highlightsHtml, 'Event Highlights');
    if (!html) return null;
    return this.sanitizer.bypassSecurityTrustHtml(html);
  });

  readonly safeAssociationHtml = computed(() => {
    const html = stripLeadingSectionHeading(this.event()?.associationHtml, 'In Association With');
    if (!html) return null;
    return this.sanitizer.bypassSecurityTrustHtml(html);
  });

  readonly hasHighlights = computed(() => {
    const ev = this.event();
    if (!ev) return false;
    return (ev.highlights?.length ?? 0) > 0 || !!ev.highlightsHtml?.trim();
  });

  readonly hasPartners = computed(() => {
    const ev = this.event();
    if (!ev) return false;
    return (ev.partners?.length ?? 0) > 0 || !!ev.associationHtml?.trim();
  });

  readonly hasAbout = computed(() => !!this.event()?.aboutHtml?.trim());

  constructor() {
    this.route.paramMap.subscribe((p) => {
      const slug = p.get('slug') ?? '';
      this.slug.set(slug);
      void this.load(slug);
    });
  }

  private async load(slug: string) {
    if (!slug) {
      void this.router.navigate(['/events']);
      return;
    }

    this.loading.set(true);
    try {
      const ev = await firstValueFrom(this.events.getPublishedBySlug(slug));
      if (!ev) {
        void this.router.navigate(['/events']);
        return;
      }
      this.event.set(ev);
      this.setSeo(slug, ev);
    } catch {
      void this.router.navigate(['/events']);
    } finally {
      this.loading.set(false);
    }
  }

  private setSeo(slug: string, ev: PublicEvent) {
    const pageTitle = `${ev.seoTitle || ev.title} | MSME Bharat Manch`;
    const canonicalUrl = `https://msmebharatmanch.com/event/${slug}`;
    const description = ev.metaDescription || ev.subtitle || ev.shortDescription;
    const ogImage = ev.imageUrl
      ? ev.imageUrl.startsWith('http')
        ? ev.imageUrl
        : `https://msmebharatmanch.com${ev.imageUrl}`
      : undefined;

    this.seoService.setPage({
      title: pageTitle,
      description,
      ogUrl: canonicalUrl,
      ogImage,
    });

    const attendance =
      ev.attendanceMode?.toLowerCase() === 'online'
        ? 'https://schema.org/OnlineEventAttendanceMode'
        : ev.attendanceMode?.toLowerCase() === 'hybrid'
          ? 'https://schema.org/MixedEventAttendanceMode'
          : 'https://schema.org/OfflineEventAttendanceMode';

    this.seoService.setJsonLd({
      '@context': 'https://schema.org',
      '@type': 'Event',
      name: ev.title,
      description,
      ...(ev.dateISO ? { startDate: ev.dateISO } : {}),
      location: {
        '@type': 'Place',
        name: ev.venue || ev.location || 'TBA',
        address: { '@type': 'PostalAddress', addressCountry: 'IN' },
      },
      organizer: {
        '@type': 'Organization',
        name: 'MSME Bharat Manch',
        url: 'https://msmebharatmanch.com',
      },
      url: canonicalUrl,
      eventStatus: 'https://schema.org/EventScheduled',
      eventAttendanceMode: attendance,
      ...(ogImage ? { image: ogImage } : {}),
    });
  }
}

/** Removes a leading heading that duplicates the section title (e.g. editor HTML already has "About the Event"). */
function stripLeadingSectionHeading(html: string | null | undefined, title: string): string {
  const raw = (html ?? '').trim();
  if (!raw) return '';

  const escaped = title.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const re = new RegExp(
    `^\\s*<(h[1-6]|p)\\b[^>]*>\\s*${escaped}\\s*</\\1>\\s*`,
    'i',
  );
  return raw.replace(re, '').trim();
}
