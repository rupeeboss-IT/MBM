import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Router } from '@angular/router';
import { JoinTodayButton } from '../../core/components/join-today-button/join-today-button';
import { ImageLightbox } from '../../core/components/image-lightbox/image-lightbox';

@Component({
  selector: 'app-home',
  imports: [RouterLink, JoinTodayButton, ImageLightbox],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home {
  private readonly router = inject(Router);

  readonly lightboxOpen = signal(false);
  readonly lightboxSrc = signal<string | null>(null);
  readonly lightboxAlt = signal('');

  goToPlan(planCode: 'premium' | 'pro') {
    try {
      window.localStorage.setItem('mbm_pending_plan', planCode);
    } catch {
      // ignore storage errors
    }
    this.router.navigateByUrl('/membership');
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
