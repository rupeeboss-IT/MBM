import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  SchemeManagementService,
  type SchemeListItem,
} from '../../../core/services/scheme-management.service';
import { ToastService } from '../../../core/services/toast.service';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';
import { AdminPagination } from '../../../core/components/admin-pagination/admin-pagination';

const PAGE_SIZE = 20;

@Component({
  selector: 'app-scheme-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, AdminPagination],
  templateUrl: './scheme-list.html',
  styleUrls: ['../../admin-shared.css', './scheme-list.css'],
})
export class SchemeList implements OnInit {
  private readonly api = inject(SchemeManagementService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly actionId = signal<number | null>(null);
  readonly schemes = signal<SchemeListItem[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly search = signal('');
  readonly statusFilter = signal('');
  readonly pageSize = PAGE_SIZE;

  readonly pagedSchemes = computed(() => {
    const start = (this.page() - 1) * PAGE_SIZE;
    return this.schemes().slice(start, start + PAGE_SIZE);
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
        this.toast.error(res.message ?? 'Failed to load schemes.');
        return;
      }
      this.schemes.set(res.schemes ?? []);
      this.total.set(res.schemes?.length ?? 0);
      this.page.set(1);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to load schemes.'));
    } finally {
      this.loading.set(false);
    }
  }

  async togglePublish(scheme: SchemeListItem) {
    const newState = !scheme.isPublished;
    this.actionId.set(scheme.schemeId);
    try {
      const res = await firstValueFrom(this.api.setPublished(scheme.schemeId, newState));
      if (res.success) {
        this.schemes.update((list) =>
          list.map((s) =>
            s.schemeId === scheme.schemeId ? { ...s, isPublished: newState } : s,
          ),
        );
        this.toast.success(newState ? 'Scheme published.' : 'Scheme unpublished (draft).');
      } else {
        this.toast.error(res.message ?? 'Action failed.');
      }
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Action failed.'));
    } finally {
      this.actionId.set(null);
    }
  }

  async deleteScheme(scheme: SchemeListItem) {
    if (!confirm(`Delete "${scheme.name}"? This cannot be undone.`)) return;
    this.actionId.set(scheme.schemeId);
    try {
      const res = await firstValueFrom(this.api.delete(scheme.schemeId));
      if (res.success) {
        this.schemes.update((list) => list.filter((s) => s.schemeId !== scheme.schemeId));
        this.total.update((t) => t - 1);
        this.toast.success('Scheme deleted.');
      } else {
        this.toast.error(res.message ?? 'Delete failed.');
      }
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Delete failed.'));
    } finally {
      this.actionId.set(null);
    }
  }

  goToEdit(schemeId: number) {
    void this.router.navigate(['/admin/scheme-management/edit', schemeId]);
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
