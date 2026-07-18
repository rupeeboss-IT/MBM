import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  EventCategoryService,
  type EventCategoryItem,
} from '../../../core/services/event-category.service';
import { ToastService } from '../../../core/services/toast.service';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';

@Component({
  selector: 'app-event-category-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './event-category-list.html',
  styleUrls: ['../../admin-shared.css', './event-category-list.css'],
})
export class EventCategoryList implements OnInit {
  private readonly api = inject(EventCategoryService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly actionId = signal<number | null>(null);
  readonly categories = signal<EventCategoryItem[]>([]);

  async ngOnInit() {
    await this.load();
  }

  async load() {
    try {
      this.loading.set(true);
      const res = await firstValueFrom(this.api.adminList());
      if (!res.success) {
        this.toast.error(res.message ?? 'Failed to load categories.');
        return;
      }
      this.categories.set(res.categories ?? []);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to load categories.'));
    } finally {
      this.loading.set(false);
    }
  }

  edit(id: number) {
    void this.router.navigate(['/admin/event-categories/edit', id]);
  }

  truncate(text: string | null | undefined, max = 60): string {
    const t = (text ?? '').trim();
    if (!t) return '—';
    return t.length > max ? `${t.slice(0, max)}…` : t;
  }

  async remove(cat: EventCategoryItem) {
    if (!confirm(`Delete category "${cat.name}"? This cannot be undone.`)) return;

    this.actionId.set(cat.eventCategoryId);
    try {
      const res = await firstValueFrom(this.api.delete(cat.eventCategoryId));
      if (res.success) {
        this.categories.update((list) =>
          list.filter((c) => c.eventCategoryId !== cat.eventCategoryId),
        );
        this.toast.success('Category deleted.');
      } else {
        this.toast.error(res.message ?? 'Delete failed.');
      }
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Delete failed.'));
    } finally {
      this.actionId.set(null);
    }
  }
}
