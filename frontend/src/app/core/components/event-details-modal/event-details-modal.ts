import { Component, HostListener, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import type { BllEventMeta } from '../../../data/bll-events.data';

@Component({
  selector: 'app-event-details-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './event-details-modal.html',
  styleUrl: './event-details-modal.css',
})
export class EventDetailsModal {
  readonly event = input<BllEventMeta | null>(null);
  readonly isOpen = input(false);
  readonly closed = output<void>();
  readonly viewPoster = output<BllEventMeta>();

  @HostListener('document:keydown', ['$event'])
  onKeydown(ev: KeyboardEvent) {
    if (!this.isOpen()) return;
    if (ev.key === 'Escape') {
      ev.preventDefault();
      this.close();
    }
  }

  close() {
    this.closed.emit();
  }

  onBackdropClick(ev: MouseEvent) {
    if ((ev.target as HTMLElement).dataset['modalBackdrop'] === 'true') {
      this.close();
    }
  }

  openPoster() {
    const ev = this.event();
    if (ev) this.viewPoster.emit(ev);
  }
}
