import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ImageLightbox } from '../../core/components/image-lightbox/image-lightbox';
import {
  EventCategoryService,
  type EventCategoryItem,
} from '../../core/services/event-category.service';
import { EventsService, type PublicEvent } from '../../core/services/events.service';
import { getHttpErrorMessage } from '../../core/utils/http-error-message';

@Component({
  selector: 'app-events',
  imports: [CommonModule, RouterLink, ImageLightbox],
  templateUrl: './events.html',
  styleUrl: './events.css',
})
export class Events implements OnInit {
  private readonly eventsApi = inject(EventsService);
  private readonly categoryApi = inject(EventCategoryService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly events = signal<PublicEvent[]>([]);
  readonly categories = signal<EventCategoryItem[]>([]);
  readonly activeCategory = signal<string>('all');

  readonly lightboxOpen = signal(false);
  readonly lightboxSrc = signal<string | null>(null);
  readonly lightboxAlt = signal('');

  async ngOnInit() {
    await Promise.all([this.loadFilters(), this.loadEvents()]);
  }

  private async loadFilters() {
    try {
      const catRes = await firstValueFrom(this.categoryApi.listPublic(true));
      const cats = catRes.categories ?? [];
      this.categories.set(
        cats.length
          ? cats
          : [{ eventCategoryId: 0, slug: 'bll', name: 'BLL', sortOrder: 1, isActive: true, showInFilter: true }],
      );
    } catch {
      this.categories.set([
        { eventCategoryId: 0, slug: 'bll', name: 'BLL', sortOrder: 1, isActive: true, showInFilter: true },
      ]);
    }
  }

  async loadEvents() {
    try {
      this.loading.set(true);
      this.error.set(null);
      const res = await firstValueFrom(
        this.eventsApi.getPublished({
          category: this.activeCategory() === 'all' ? undefined : this.activeCategory(),
          page: 1,
          pageSize: 100,
        }),
      );
      this.events.set(res.events);
    } catch (e: unknown) {
      this.error.set(getHttpErrorMessage(e, 'Failed to load events.'));
      this.events.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  setCategory(slug: string) {
    this.activeCategory.set(slug);
    void this.loadEvents();
  }

  metaLine(ev: PublicEvent): string {
    const parts = [ev.date, ev.time, ev.location].filter((p) => !!p?.trim());
    return parts.join(' · ');
  }

  openLightbox(src: string, alt: string, event: MouseEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.lightboxSrc.set(src);
    this.lightboxAlt.set(alt);
    this.lightboxOpen.set(true);
  }

  closeLightbox() {
    this.lightboxOpen.set(false);
  }
}
