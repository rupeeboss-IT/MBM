import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { DomSanitizer, Meta, Title } from '@angular/platform-browser';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { EventsService } from '../../../core/services/events.service';
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
  private readonly sanitizer = inject(DomSanitizer);
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);

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
    this.title.setTitle(pageTitle);
    this.meta.updateTag({ name: 'description', content: ev.subtitle });
    this.meta.updateTag({ property: 'og:type', content: 'website' });
    this.meta.updateTag({ property: 'og:title', content: pageTitle });
    this.meta.updateTag({ property: 'og:description', content: ev.subtitle });
    this.meta.updateTag({ property: 'og:url', content: `/event/${slug}` });
  }
}

