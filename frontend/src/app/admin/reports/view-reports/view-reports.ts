import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom, timeout } from 'rxjs';
import {
  CustomerReportService,
  type ReportHistoryItem,
} from '../../../core/services/customer-report.service';
import { AdminSessionService } from '../../../core/services/admin-session.service';
import { ToastService } from '../../../core/services/toast.service';
import { API_USER_MESSAGES } from '../../../core/utils/api-user-messages';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';

@Component({
  selector: 'app-admin-view-reports',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './view-reports.html',
  styleUrls: ['../../admin-shared.css'],
})
export class AdminViewReports {
  private readonly api = inject(CustomerReportService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly adminSession = inject(AdminSessionService);

  readonly search = signal('');
  readonly page = signal(1);
  readonly pageSize = 20;
  readonly loading = signal(false);
  readonly items = signal<ReportHistoryItem[]>([]);
  readonly totalCount = signal(0);

  constructor() {
    void this.load();
  }

  async load() {
    try {
      this.loading.set(true);
      const res = await firstValueFrom(
        this.api.adminHistory(this.search(), this.page(), this.pageSize).pipe(timeout(20000))
      );
      if (res?.success === false) {
        this.toast.error(res.message || API_USER_MESSAGES.reportHistory);
        this.items.set([]);
        this.totalCount.set(0);
        return;
      }
      this.items.set(res?.items ?? []);
      this.totalCount.set(res?.totalCount ?? 0);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, API_USER_MESSAGES.reportHistory));
      this.items.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  onSearchInput(ev: Event) {
    this.search.set((ev.target as HTMLInputElement).value || '');
  }

  runSearch() {
    this.page.set(1);
    void this.load();
  }

  prevPage() {
    if (this.page() <= 1) return;
    this.page.update((p) => p - 1);
    void this.load();
  }

  nextPage() {
    const maxPage = Math.ceil(this.totalCount() / this.pageSize) || 1;
    if (this.page() >= maxPage) return;
    this.page.update((p) => p + 1);
    void this.load();
  }

  formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  logout() {
    this.adminSession.logout();
    void this.router.navigateByUrl('/admin-login');
  }
}
