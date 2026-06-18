import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { firstValueFrom, timeout } from 'rxjs';
import { AdminListToolbar } from '../../../core/components/admin-list-toolbar/admin-list-toolbar';
import { AdminPagination } from '../../../core/components/admin-pagination/admin-pagination';
import { ToastService } from '../../../core/services/toast.service';
import {
  EnquiryManagementService,
  type EnquiryListItem,
} from '../../../core/services/enquiry-management.service';
import { exportToExcel } from '../../../core/utils/admin-excel-export';
import { sortIndicator, toggleColumnSort } from '../../../core/utils/admin-list-params';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';
import { LocalDatePipe } from '../../../core/pipes/local-date.pipe';

type ListView = 'all' | 'new' | 'resolved' | 'closed';

@Component({
  selector: 'app-enquiry-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, RouterLinkActive, AdminListToolbar, AdminPagination, LocalDatePipe],
  templateUrl: './enquiry-list.html',
  styleUrls: ['../../admin-shared.css', './enquiry-list.css'],
})
export class EnquiryList {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(EnquiryManagementService);
  private readonly toast = inject(ToastService);

  readonly loading = signal(false);
  readonly exporting = signal(false);
  readonly actionSubmitting = signal(false);
  readonly enquiries = signal<EnquiryListItem[]>([]);
  readonly sources = signal<string[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly pageSize = 10;
  readonly search = signal('');
  readonly statusFilter = signal('');
  readonly sourceFilter = signal('');
  readonly dateFrom = signal('');
  readonly dateTo = signal('');
  readonly sortPreset = signal('latest');
  readonly sortBy = signal('created');
  readonly sortDir = signal<'asc' | 'desc'>('desc');
  readonly listView = signal<ListView>('all');

  readonly selectedIds = signal<Set<number>>(new Set());
  readonly bulkAction = signal('');
  readonly rowActionOpenId = signal<number | null>(null);

  readonly selectedCount = computed(() => this.selectedIds().size);
  readonly allOnPageSelected = computed(() => {
    const rows = this.enquiries();
    if (rows.length === 0) return false;
    const sel = this.selectedIds();
    return rows.every((r) => sel.has(r.id));
  });

  readonly sortIndicator = sortIndicator;

  constructor() {
    this.route.data.subscribe((data) => {
      const view = data['view'] as ListView | undefined;
      if (view === 'new') {
        this.listView.set('new');
        this.statusFilter.set('New');
      } else if (view === 'resolved') {
        this.listView.set('resolved');
        this.statusFilter.set('Resolved');
      } else if (view === 'closed') {
        this.listView.set('closed');
        this.statusFilter.set('Closed');
      } else {
        this.listView.set('all');
        const status = this.route.snapshot.queryParamMap.get('status');
        this.statusFilter.set(status ?? '');
      }
      this.page.set(1);
      void this.load();
    });

    const source = this.route.snapshot.queryParamMap.get('source');
    if (source) this.sourceFilter.set(source);
    void this.loadFilters();
  }

  listQueryOpts(exportAll = false) {
    const preset = this.sortPreset();
    const usePreset = ['latest', 'oldest', 'name_asc', 'name_desc'].includes(preset);
    return {
      page: exportAll ? 1 : this.page(),
      pageSize: exportAll ? 10000 : this.pageSize,
      dateFrom: this.dateFrom() || undefined,
      dateTo: this.dateTo() || undefined,
      sortBy: usePreset ? preset : this.sortBy(),
      sortDir: usePreset ? undefined : this.sortDir(),
      export: exportAll || undefined,
      status: this.statusFilter() || undefined,
      source: this.sourceFilter() || undefined,
    };
  }

  async loadFilters() {
    try {
      const res = await firstValueFrom(this.api.filters().pipe(timeout(15000)));
      this.sources.set(res?.sources ?? []);
    } catch {
      this.sources.set([]);
    }
  }

  async load() {
    try {
      this.loading.set(true);
      const res = await firstValueFrom(
        this.api.list(this.search(), this.listQueryOpts()).pipe(timeout(20000)),
      );
      if (res?.success === false) {
        this.toast.error(res.message || 'Unable to load enquiries.');
        this.enquiries.set([]);
        this.total.set(0);
        return;
      }
      this.enquiries.set(res?.enquiries ?? []);
      this.total.set(res?.total ?? 0);
      this.selectedIds.set(new Set());
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to load enquiries.'));
      this.enquiries.set([]);
      this.total.set(0);
    } finally {
      this.loading.set(false);
    }
  }

  applyFilters() {
    this.page.set(1);
    void this.load();
  }

  onPageChange(p: number) {
    this.page.set(p);
    void this.load();
  }

  onSortColumn(field: string) {
    const next = toggleColumnSort(this.sortBy(), this.sortDir(), field);
    this.sortBy.set(next.sortBy);
    this.sortDir.set(next.sortDir);
    this.sortPreset.set('');
    void this.load();
  }

  detailLink(item: EnquiryListItem): string[] {
    return ['/admin/enquiry-management/enquiries', String(item.id)];
  }

  statusBadgeClass(status: string): string {
    switch (status) {
      case 'New':
        return 'badge badge-danger';
      case 'Read':
        return 'badge badge-info';
      case 'In Progress':
        return 'badge badge-warning';
      case 'Resolved':
        return 'badge badge-success';
      case 'Closed':
        return 'badge badge-neutral';
      default:
        return 'badge badge-neutral';
    }
  }

  toggleSelectAll(checked: boolean) {
    if (!checked) {
      this.selectedIds.set(new Set());
      return;
    }
    this.selectedIds.set(new Set(this.enquiries().map((e) => e.id)));
  }

  toggleSelect(id: number, checked: boolean) {
    const next = new Set(this.selectedIds());
    if (checked) next.add(id);
    else next.delete(id);
    this.selectedIds.set(next);
  }

  isSelected(id: number): boolean {
    return this.selectedIds().has(id);
  }

  toggleRowActions(id: number, event: Event) {
    event.stopPropagation();
    this.rowActionOpenId.set(this.rowActionOpenId() === id ? null : id);
  }

  closeRowActions() {
    this.rowActionOpenId.set(null);
  }

  async applyRowStatus(item: EnquiryListItem, status: string) {
    this.closeRowActions();
    try {
      this.actionSubmitting.set(true);
      const res = await firstValueFrom(
        this.api.updateStatus(item.id, status).pipe(timeout(15000)),
      );
      if (res?.success === false) {
        this.toast.error(res.message || 'Unable to update status.');
        return;
      }
      this.toast.success(res.message || 'Status updated.');
      await this.load();
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to update status.'));
    } finally {
      this.actionSubmitting.set(false);
    }
  }

  async applyBulkAction() {
    const status = this.bulkAction();
    const ids = [...this.selectedIds()];
    if (!status) {
      this.toast.error('Select a bulk action.');
      return;
    }
    if (ids.length === 0) {
      this.toast.error('Select at least one enquiry.');
      return;
    }
    try {
      this.actionSubmitting.set(true);
      const res = await firstValueFrom(
        this.api.bulkUpdateStatus(ids, status).pipe(timeout(30000)),
      );
      if (res?.success === false) {
        this.toast.error(res.message || 'Bulk update failed.');
        return;
      }
      this.toast.success(res.message || 'Bulk update completed.');
      this.bulkAction.set('');
      await this.load();
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Bulk update failed.'));
    } finally {
      this.actionSubmitting.set(false);
    }
  }

  async exportExcel() {
    try {
      this.exporting.set(true);
      const res = await firstValueFrom(
        this.api.list(this.search(), this.listQueryOpts(true)).pipe(timeout(60000)),
      );
      const rows = res?.enquiries ?? [];
      exportToExcel(
        'enquiries',
        [
          { header: 'Enquiry Id', value: (r) => r.id },
          { header: 'Name', value: (r) => r.fullName },
          { header: 'Company Name', value: (r) => r.companyName ?? '' },
          { header: 'Mobile', value: (r) => r.phone },
          { header: 'Email', value: (r) => r.email || '—' },
          { header: 'Source', value: (r) => r.source },
          { header: 'Subject', value: (r) => r.subject },
          { header: 'Created Date', value: (r) => r.createdAt, type: 'datetime' },
          { header: 'Status', value: (r) => r.status },
          { header: 'Assigned To', value: (r) => r.assignedToName ?? '' },
        ],
        rows,
      );
      this.toast.success('Export started.');
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Export failed.'));
    } finally {
      this.exporting.set(false);
    }
  }

  navigateView(view: ListView) {
    const paths: Record<ListView, string> = {
      all: '/admin/enquiry-management/enquiries',
      new: '/admin/enquiry-management/new',
      resolved: '/admin/enquiry-management/resolved',
      closed: '/admin/enquiry-management/closed',
    };
    void this.router.navigate([paths[view]]);
  }
}
