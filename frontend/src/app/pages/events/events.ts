import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ImageLightbox } from '../../core/components/image-lightbox/image-lightbox';

@Component({
  selector: 'app-events',
  imports: [CommonModule, RouterLink, ImageLightbox],
  templateUrl: './events.html',
  styleUrl: './events.css',
})
export class Events {
  readonly active = signal<'all' | 'bll'>('all');

  readonly lightboxOpen = signal(false);
  readonly lightboxSrc = signal<string | null>(null);
  readonly lightboxAlt = signal('');

  setFilter(cat: 'all' | 'bll') {
    this.active.set(cat);
  }

  show(cat: 'bll') {
    const a = this.active();
    return a === 'all' || a === cat;
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
