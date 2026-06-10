import { Component, inject, input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BllEventsService } from '../../services/bll-events.service';
import type { BllEventMeta } from '../../../data/bll-events.data';
import { ImageLightbox } from '../image-lightbox/image-lightbox';
import { EventDetailsModal } from '../event-details-modal/event-details-modal';

@Component({
  selector: 'app-bll-events-grid',
  standalone: true,
  imports: [CommonModule, ImageLightbox, EventDetailsModal],
  templateUrl: './bll-events-grid.html',
  styleUrl: './bll-events-grid.css',
})
export class BllEventsGrid {
  private readonly bllEvents = inject(BllEventsService);

  /** 'page' = full events page layout; 'home' = compact home section cards */
  readonly variant = input<'page' | 'home'>('page');

  readonly events = this.bllEvents.events;
  readonly loading = this.bllEvents.loading;
  readonly error = this.bllEvents.error;

  readonly lightboxOpen = signal(false);
  readonly lightboxSrc = signal<string | null>(null);
  readonly lightboxAlt = signal('');

  readonly detailsOpen = signal(false);
  readonly selectedEvent = signal<BllEventMeta | null>(null);

  openLightbox(ev: BllEventMeta, event?: Event) {
    event?.stopPropagation();
    this.lightboxSrc.set(ev.imageUrl);
    this.lightboxAlt.set(ev.title);
    this.lightboxOpen.set(true);
  }

  closeLightbox() {
    this.lightboxOpen.set(false);
  }

  openDetails(ev: BllEventMeta, event?: Event) {
    event?.stopPropagation();
    this.selectedEvent.set(ev);
    this.detailsOpen.set(true);
  }

  closeDetails() {
    this.detailsOpen.set(false);
  }

  viewPosterFromDetails(ev: BllEventMeta) {
    this.closeDetails();
    this.openLightbox(ev);
  }
}
