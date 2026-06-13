import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom, timeout } from 'rxjs';
import { AdminListToolbar } from '../../../core/components/admin-list-toolbar/admin-list-toolbar';
import { AdminPagination } from '../../../core/components/admin-pagination/admin-pagination';
import {
  CustomerReportService,
  type ReportAuditLogItem,
  type ReportChangeRequestDetail,
  type ReportChangeRequestItem,
  type ReportHistoryItem,
} from '../../../core/services/customer-report.service';
import { AdminSessionService } from '../../../core/services/admin-session.service';
import { ToastService } from '../../../core/services/toast.service';
import { API_USER_MESSAGES } from '../../../core/utils/api-user-messages';
import { exportToExcel } from '../../../core/utils/admin-excel-export';
import { sortIndicator, toggleColumnSort } from '../../../core/utils/admin-list-params';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';
import { LocalDatePipe } from '../../../core/pipes/local-date.pipe';

type RequestModalMode = 'delete' | 'replace' | 'edit' | null;
type SuperAdminModalMode = 'edit' | 'replace' | 'delete' | 'audit' | null;

@Component({
  selector: 'app-admin-view-reports',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, LocalDatePipe, AdminListToolbar, AdminPagination],
  templateUrl: './view-reports.html',
  styleUrls: ['../../admin-shared.css', './view-reports.css'],
})
export class AdminViewReports {
  private readonly api = inject(CustomerReportService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly adminSession = inject(AdminSessionService);

  readonly isSuperAdmin = computed(() => this.adminSession.isSuperAdmin());

  readonly search = signal('');
  readonly page = signal(1);
  readonly pageSize = 10;
  readonly loading = signal(false);
  readonly exporting = signal(false);
  readonly items = signal<ReportHistoryItem[]>([]);
  readonly totalCount = signal(0);
  readonly dateFrom = signal('');
  readonly dateTo = signal('');
  readonly sortPreset = signal('latest');
  readonly sortBy = signal('created');
  readonly sortDir = signal<'asc' | 'desc'>('desc');

  readonly pendingCount = signal(0);
  readonly queueLoading = signal(false);
  readonly queueItems = signal<ReportChangeRequestItem[]>([]);
  readonly queueTotal = signal(0);
  readonly queuePage = signal(1);
  readonly queuePageSize = 10;

  readonly requestModalOpen = signal(false);
  readonly requestModalMode = signal<RequestModalMode>(null);
  readonly requestTarget = signal<ReportHistoryItem | null>(null);
  readonly requestReason = signal('');
  readonly requestNewFileName = signal('');
  readonly requestFile = signal<File | null>(null);
  readonly requestSubmitting = signal(false);

  readonly detailModalOpen = signal(false);
  readonly detailLoading = signal(false);
  readonly detailItem = signal<ReportChangeRequestDetail | null>(null);
  readonly reviewRemarks = signal('');
  readonly reviewSubmitting = signal(false);

  readonly superModalOpen = signal(false);
  readonly superModalMode = signal<SuperAdminModalMode>(null);
  readonly superTarget = signal<ReportHistoryItem | null>(null);
  readonly superReason = signal('');
  readonly superFileName = signal('');
  readonly superFile = signal<File | null>(null);
  readonly superSubmitting = signal(false);
  readonly auditItems = signal<ReportAuditLogItem[]>([]);

  readonly sortIndicator = sortIndicator;

  constructor() {
    void this.load();
    void this.loadPendingCount();
    void this.loadQueue();
  }

  listQueryOpts(exportAll = false) {
    const preset = this.sortPreset();
    const usePreset = ['latest', 'oldest', 'name_asc', 'name_desc'].includes(preset);
    return {
      dateFrom: this.dateFrom() || undefined,
      dateTo: this.dateTo() || undefined,
      sortBy: usePreset ? preset : this.sortBy(),
      sortDir: usePreset ? undefined : this.sortDir(),
      export: exportAll || undefined,
    };
  }

  async load() {
    try {
      this.loading.set(true);
      const res = await firstValueFrom(
        this.api
          .adminHistory(this.search(), this.page(), this.pageSize, this.listQueryOpts())
          .pipe(timeout(20000)),
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

  async loadPendingCount() {
    if (!this.isSuperAdmin()) return;
    try {
      const res = await firstValueFrom(this.api.adminPendingRequestCount().pipe(timeout(10000)));
      this.pendingCount.set(res?.count ?? 0);
    } catch {
      this.pendingCount.set(0);
    }
  }

  async loadQueue() {
    try {
      this.queueLoading.set(true);
      const status = this.isSuperAdmin() ? 'Pending' : undefined;
      const res = await firstValueFrom(
        this.api
          .adminListChangeRequests(this.queuePage(), this.queuePageSize, status ? { status } : undefined)
          .pipe(timeout(20000)),
      );
      this.queueItems.set(res?.items ?? []);
      this.queueTotal.set(res?.totalCount ?? 0);
    } catch (e: unknown) {
      if (this.isSuperAdmin()) {
        this.toast.error(getHttpErrorMessage(e, 'Unable to load approval queue.'));
      }
      this.queueItems.set([]);
    } finally {
      this.queueLoading.set(false);
    }
  }

  private async loadWithExport() {
    return firstValueFrom(
      this.api
        .adminHistory(this.search(), 1, this.pageSize, this.listQueryOpts(true))
        .pipe(timeout(60000)),
    );
  }

  applyFilters() {
    this.page.set(1);
    void this.load();
  }

  onSortPresetChange(value: string) {
    this.sortPreset.set(value);
    this.page.set(1);
    void this.load();
  }

  onColumnSort(column: string) {
    const next = toggleColumnSort(this.sortBy(), this.sortDir(), column);
    this.sortBy.set(next.sortBy);
    this.sortDir.set(next.sortDir);
    this.sortPreset.set('');
    this.page.set(1);
    void this.load();
  }

  onPageChange(next: number) {
    this.page.set(next);
    void this.load();
  }

  onQueuePageChange(next: number) {
    this.queuePage.set(next);
    void this.loadQueue();
  }

  async exportExcel() {
    try {
      this.exporting.set(true);
      const res = await this.loadWithExport();
      const rows = res?.items ?? [];
      if (!rows.length) {
        this.toast.warning('No rows to export for the current filters.');
        return;
      }
      await exportToExcel(
        'report-history',
        [
          { header: 'Customer', value: (r) => r.customerName },
          { header: 'Member ID', value: (r) => r.memberId },
          { header: 'Email', value: (r) => r.email },
          { header: 'Upload date', value: (r) => r.uploadDate, type: 'datetime' },
          { header: 'Downloads', value: (r) => r.downloadCount, type: 'number' },
          { header: 'Last download', value: (r) => r.lastDownloadDate ?? '', type: 'datetime' },
          { header: 'File', value: (r) => r.originalFileName },
          { header: 'Size (bytes)', value: (r) => r.fileSize, type: 'number' },
          { header: 'Plan', value: (r) => r.planName || '—' },
          { header: 'Request status', value: (r) => r.latestRequestStatus || '—' },
        ],
        rows,
        { sheetTitle: 'Report History' },
      );
      this.toast.success(`Exported ${rows.length} report(s).`);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Could not export reports.'));
    } finally {
      this.exporting.set(false);
    }
  }

  formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  requestStatusBadge(item: ReportHistoryItem): { label: string; class: string } | null {
    if (item.hasPendingRequest) return { label: 'Pending', class: 'badge-warning' };
    const status = (item.latestRequestStatus ?? '').toLowerCase();
    if (status === 'approved') return { label: 'Approved', class: 'badge-success' };
    if (status === 'rejected') return { label: 'Rejected', class: 'badge-danger' };
    return null;
  }

  openRequestModal(mode: RequestModalMode, item: ReportHistoryItem) {
    if (item.hasPendingRequest) {
      this.toast.warning('A request is already pending for this report.');
      return;
    }
    this.requestModalMode.set(mode);
    this.requestTarget.set(item);
    this.requestReason.set('');
    this.requestNewFileName.set(item.originalFileName);
    this.requestFile.set(null);
    this.requestModalOpen.set(true);
  }

  closeRequestModal() {
    this.requestModalOpen.set(false);
    this.requestModalMode.set(null);
    this.requestTarget.set(null);
  }

  onRequestFileChange(ev: Event) {
    const input = ev.target as HTMLInputElement;
    this.requestFile.set(input.files?.[0] ?? null);
  }

  async submitRequest() {
    const mode = this.requestModalMode();
    const target = this.requestTarget();
    const reason = this.requestReason().trim();
    if (!mode || !target) return;
    if (!reason) {
      this.toast.warning('Reason is required.');
      return;
    }

    const form = new FormData();
    form.append('reportId', target.id);
    form.append('requestType', mode === 'delete' ? 'Delete' : mode === 'replace' ? 'Replace' : 'Edit');
    form.append('reason', reason);

    if (mode === 'edit') {
      const name = this.requestNewFileName().trim();
      if (!name) {
        this.toast.warning('New file name is required.');
        return;
      }
      form.append('newOriginalFileName', name);
    }
    if (mode === 'replace') {
      const file = this.requestFile();
      if (!file) {
        this.toast.warning('Select a replacement ZIP file.');
        return;
      }
      form.append('file', file, file.name);
    }

    try {
      this.requestSubmitting.set(true);
      const res = await firstValueFrom(this.api.adminCreateChangeRequest(form).pipe(timeout(60000)));
      if (!res?.success) {
        this.toast.error(res?.message || API_USER_MESSAGES.save);
        return;
      }
      this.toast.success(res.message || 'Request submitted.');
      this.closeRequestModal();
      await Promise.all([this.load(), this.loadPendingCount(), this.loadQueue()]);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, API_USER_MESSAGES.save));
    } finally {
      this.requestSubmitting.set(false);
    }
  }

  async openDetailModal(requestId: string) {
    this.detailModalOpen.set(true);
    this.detailLoading.set(true);
    this.detailItem.set(null);
    this.reviewRemarks.set('');
    try {
      const res = await firstValueFrom(this.api.adminGetChangeRequest(requestId).pipe(timeout(20000)));
      if (!res?.success || !res.request) {
        this.toast.error(res?.message || 'Unable to load request details.');
        this.closeDetailModal();
        return;
      }
      this.detailItem.set(res.request);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to load request details.'));
      this.closeDetailModal();
    } finally {
      this.detailLoading.set(false);
    }
  }

  closeDetailModal() {
    this.detailModalOpen.set(false);
    this.detailItem.set(null);
    this.reviewRemarks.set('');
  }

  async approveRequest() {
    const detail = this.detailItem();
    if (!detail) return;
    try {
      this.reviewSubmitting.set(true);
      const res = await firstValueFrom(
        this.api.adminApproveChangeRequest(detail.id, this.reviewRemarks()).pipe(timeout(30000)),
      );
      if (!res?.success) {
        this.toast.error(res?.message || API_USER_MESSAGES.save);
        return;
      }
      this.toast.success(res.message || 'Request approved.');
      this.closeDetailModal();
      await Promise.all([this.load(), this.loadPendingCount(), this.loadQueue()]);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, API_USER_MESSAGES.save));
    } finally {
      this.reviewSubmitting.set(false);
    }
  }

  async rejectRequest() {
    const detail = this.detailItem();
    if (!detail) return;
    try {
      this.reviewSubmitting.set(true);
      const res = await firstValueFrom(
        this.api.adminRejectChangeRequest(detail.id, this.reviewRemarks()).pipe(timeout(30000)),
      );
      if (!res?.success) {
        this.toast.error(res?.message || API_USER_MESSAGES.save);
        return;
      }
      this.toast.success(res.message || 'Request rejected.');
      this.closeDetailModal();
      await Promise.all([this.load(), this.loadPendingCount(), this.loadQueue()]);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, API_USER_MESSAGES.save));
    } finally {
      this.reviewSubmitting.set(false);
    }
  }

  openSuperModal(mode: SuperAdminModalMode, item: ReportHistoryItem) {
    this.superModalMode.set(mode);
    this.superTarget.set(item);
    this.superReason.set('');
    this.superFileName.set(item.originalFileName);
    this.superFile.set(null);
    this.auditItems.set([]);
    this.superModalOpen.set(true);
    if (mode === 'audit') void this.loadAudit(item.id);
  }

  closeSuperModal() {
    this.superModalOpen.set(false);
    this.superModalMode.set(null);
    this.superTarget.set(null);
    this.auditItems.set([]);
  }

  onSuperFileChange(ev: Event) {
    const input = ev.target as HTMLInputElement;
    this.superFile.set(input.files?.[0] ?? null);
  }

  async loadAudit(reportId: string) {
    try {
      this.superSubmitting.set(true);
      const res = await firstValueFrom(this.api.adminReportAuditHistory(reportId).pipe(timeout(20000)));
      this.auditItems.set(res?.items ?? []);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to load audit history.'));
      this.auditItems.set([]);
    } finally {
      this.superSubmitting.set(false);
    }
  }

  async submitSuperAction() {
    const mode = this.superModalMode();
    const target = this.superTarget();
    if (!mode || !target || mode === 'audit') return;

    const reason = this.superReason().trim();
    if (!reason) {
      this.toast.warning('Reason is required.');
      return;
    }

    try {
      this.superSubmitting.set(true);
      let res;
      if (mode === 'delete') {
        res = await firstValueFrom(this.api.adminDirectDeleteReport(target.id, reason).pipe(timeout(30000)));
      } else if (mode === 'edit') {
        const name = this.superFileName().trim();
        if (!name) {
          this.toast.warning('File name is required.');
          return;
        }
        res = await firstValueFrom(
          this.api.adminDirectEditReport(target.id, name, reason).pipe(timeout(30000)),
        );
      } else {
        const file = this.superFile();
        if (!file) {
          this.toast.warning('Select a replacement ZIP file.');
          return;
        }
        res = await firstValueFrom(
          this.api.adminDirectReplaceReport(target.id, file, reason).pipe(timeout(60000)),
        );
      }
      if (!res?.success) {
        this.toast.error(res?.message || API_USER_MESSAGES.save);
        return;
      }
      this.toast.success(res.message || 'Done.');
      this.closeSuperModal();
      await Promise.all([this.load(), this.loadPendingCount(), this.loadQueue()]);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, API_USER_MESSAGES.save));
    } finally {
      this.superSubmitting.set(false);
    }
  }

  logout() {
    this.adminSession.logout();
    void this.router.navigateByUrl('/admin-login');
  }
}
