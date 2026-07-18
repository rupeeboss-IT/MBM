import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  EventManagementService,
  type EventListItem,
} from '../../../core/services/event-management.service';
import { ToastService } from '../../../core/services/toast.service';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';
import { AdminPagination } from '../../../core/components/admin-pagination/admin-pagination';

const PAGE_SIZE = 20;

@Component({
  selector: 'app-event-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, AdminPagination],
  templateUrl: './event-list.html',
  styleUrls: ['../../admin-shared.css', './event-list.css'],
})
export class EventList implements OnInit {
  private readonly api = inject(EventManagementService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly actionId = signal<number | null>(null);
  readonly events = signal<EventListItem[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly search = signal('');
  readonly statusFilter = signal('');
  readonly pageSize = PAGE_SIZE;

  readonly pagedEvents = computed(() => {
    const start = (this.page() - 1) * PAGE_SIZE;
    return this.events().slice(start, start + PAGE_SIZE);
  });

  async ngOnInit() {
    await this.load();
  }

  async load() {
    try {
      this.loading.set(true);
      const res = await firstValueFrom(
        this.api.adminList({
          search: this.search().trim() || undefined,
          status: this.statusFilter() || undefined,
          page: 1,
          pageSize: 200,
        }),
      );
      if (!res.success) {
        this.toast.error(res.message ?? 'Failed to load events.');
        return;
      }
      this.events.set(res.events ?? []);
      this.total.set(res.events?.length ?? 0);
      this.page.set(1);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to load events.'));
    } finally {
      this.loading.set(false);
    }
  }

  async togglePublish(event: EventListItem) {
    const newState = !event.isPublished;
    this.actionId.set(event.eventId);
    try {
      const res = await firstValueFrom(this.api.setPublished(event.eventId, newState));
      if (res.success) {
        this.events.update((list) =>
          list.map((e) => (e.eventId === event.eventId ? { ...e, isPublished: newState } : e)),
        );
        this.toast.success(newState ? 'Event published.' : 'Event unpublished (draft).');
      } else {
        this.toast.error(res.message ?? 'Action failed.');
      }
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Action failed.'));
    } finally {
      this.actionId.set(null);
    }
  }

  async deleteEvent(event: EventListItem) {
    if (!confirm(`Delete "${event.name}"? This cannot be undone.`)) return;
    this.actionId.set(event.eventId);
    try {
      const res = await firstValueFrom(this.api.delete(event.eventId));
      if (res.success) {
        this.events.update((list) => list.filter((e) => e.eventId !== event.eventId));
        this.total.update((t) => t - 1);
        this.toast.success('Event deleted.');
      } else {
        this.toast.error(res.message ?? 'Delete failed.');
      }
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Delete failed.'));
    } finally {
      this.actionId.set(null);
    }
  }

  goToEdit(eventId: number) {
    void this.router.navigate(['/admin/event-management/edit', eventId]);
  }

  onPageChange(p: number) {
    this.page.set(p);
  }

  runSearch() {
    void this.load();
  }

  onStatusChange(v: string) {
    this.statusFilter.set(v);
    void this.load();
  }

  publishBadge(isPublished: boolean): string {
    return isPublished ? 'badge-success' : 'badge-warning';
  }

  publishLabel(isPublished: boolean): string {
    return isPublished ? 'Published' : 'Draft';
  }
}
