import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom, timeout } from 'rxjs';
import { AdminListToolbar } from '../../../core/components/admin-list-toolbar/admin-list-toolbar';
import { AdminPagination } from '../../../core/components/admin-pagination/admin-pagination';
import { ToastService } from '../../../core/services/toast.service';
import {
  CreditRepairManagementService,
  type CreditRepairListItem,
} from '../../../core/services/credit-repair-management.service';
import { exportToExcel } from '../../../core/utils/admin-excel-export';
import { ADMIN_DEFAULT_PAGE_SIZE, sortIndicator, toggleColumnSort } from '../../../core/utils/admin-list-params';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';
import { LocalDatePipe } from '../../../core/pipes/local-date.pipe';

@Component({
  selector: 'app-credit-repair-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    AdminListToolbar,
    AdminPagination,
    LocalDatePipe,
  ],
  templateUrl: './credit-repair-list.html',
  styleUrls: ['../../admin-shared.css', './credit-repair-list.css'],
})
export class CreditRepairList {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(CreditRepairManagementService);
  private readonly toast = inject(ToastService);

  readonly loading = signal(false);
  readonly exporting = signal(false);
  readonly leads = signal<CreditRepairListItem[]>([]);
  readonly sources = signal<string[]>([]);
  readonly campaigns = signal<string[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly pageSize = ADMIN_DEFAULT_PAGE_SIZE;
  readonly search = signal('');
  readonly sourceFilter = signal('');
  readonly campaignFilter = signal('');
  readonly linkStatusFilter = signal('');
  readonly dateFrom = signal('');
  readonly dateTo = signal('');
  readonly sortPreset = signal('latest');
  readonly sortBy = signal('created');
  readonly sortDir = signal<'asc' | 'desc'>('desc');

  readonly sortIndicator = sortIndicator;

  constructor() {
    this.route.queryParamMap.subscribe((params) => {
      const linkStatus = params.get('linkStatus');
      this.linkStatusFilter.set(linkStatus === 'linked' || linkStatus === 'unlinked' ? linkStatus : '');
      this.page.set(1);
      void this.load();
    });

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
      source: this.sourceFilter() || undefined,
      campaign: this.campaignFilter() || undefined,
      linkStatus: this.linkStatusFilter() || undefined,
    };
  }

  async loadFilters() {
    try {
      const res = await firstValueFrom(this.api.filters().pipe(timeout(15000)));
      this.sources.set(res?.sources ?? []);
      this.campaigns.set(res?.campaigns ?? []);
    } catch {
      this.sources.set([]);
      this.campaigns.set([]);
    }
  }

  async load() {
    try {
      this.loading.set(true);
      const res = await firstValueFrom(
        this.api.list(this.search(), this.listQueryOpts()).pipe(timeout(20000)),
      );
      if (res?.success === false) {
        this.toast.error(res.message || 'Unable to load credit repair leads.');
        this.leads.set([]);
        this.total.set(0);
        return;
      }
      this.leads.set(res?.leads ?? []);
      this.total.set(res?.total ?? 0);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to load credit repair leads.'));
      this.leads.set([]);
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

  display(v: string | null | undefined): string {
    const s = (v ?? '').trim();
    return s.length ? s : '—';
  }

  async exportExcel() {
    try {
      this.exporting.set(true);
      const res = await firstValueFrom(
        this.api.list(this.search(), this.listQueryOpts(true)).pipe(timeout(60000)),
      );
      const rows = res?.leads ?? [];
      exportToExcel(
        'credit-repair-leads',
        [
          { header: 'Id', value: (r) => r.id },
          { header: 'Name', value: (r) => r.fullName },
          { header: 'Mobile', value: (r) => r.phone },
          { header: 'Email', value: (r) => r.email ?? '' },
          { header: 'Source', value: (r) => r.source },
          { header: 'Campaign', value: (r) => r.campaignName },
          { header: 'Consent', value: (r) => (r.consentAccepted ? 'Yes' : 'No') },
          { header: 'Created', value: (r) => r.createdAt, type: 'datetime' },
          { header: 'CRM Lead ID', value: (r) => r.leadId ?? '' },
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
}
