import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom, timeout } from 'rxjs';
import { AdminListToolbar } from '../../../core/components/admin-list-toolbar/admin-list-toolbar';
import { AdminPagination } from '../../../core/components/admin-pagination/admin-pagination';
import { ToastService } from '../../../core/services/toast.service';
import {
  LeadAttributionService,
  type LeadCustomerListItem,
  type LeadFilterOptions,
} from '../../../core/services/lead-attribution.service';
import { exportToExcel } from '../../../core/utils/admin-excel-export';
import { sortIndicator, toggleColumnSort } from '../../../core/utils/admin-list-params';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';
import { LocalDatePipe } from '../../../core/pipes/local-date.pipe';

@Component({
  selector: 'app-lead-customer-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, LocalDatePipe, AdminListToolbar, AdminPagination],
  templateUrl: './lead-customer-list.html',
  styleUrls: ['../../admin-shared.css', './lead-customer-list.css'],
})
export class LeadCustomerList {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(LeadAttributionService);
  private readonly toast = inject(ToastService);

  readonly loading = signal(false);
  readonly exporting = signal(false);
  readonly customers = signal<LeadCustomerListItem[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly pageSize = 10;
  readonly search = signal('');
  readonly dateFrom = signal('');
  readonly dateTo = signal('');
  readonly sortPreset = signal('latest');
  readonly sortBy = signal('created');
  readonly sortDir = signal<'asc' | 'desc'>('desc');

  readonly sourceTypeFilter = signal('');
  readonly sourceNameFilter = signal('');
  readonly employeeFilter = signal('');
  readonly partnerFilter = signal('');
  readonly planFilter = signal('');

  readonly filterOptions = signal<LeadFilterOptions>({
    sourceTypes: [],
    sourceNames: [],
    employeeNames: [],
    partnerNames: [],
    planCodes: [],
  });

  readonly sortIndicator = sortIndicator;

  constructor() {
    const qp = this.route.snapshot.queryParamMap;
    const st = qp.get('sourceType');
    if (st) this.sourceTypeFilter.set(st);
    void this.loadFilters();
    void this.load();
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
      sourceType: this.sourceTypeFilter() || undefined,
      sourceName: this.sourceNameFilter() || undefined,
      employeeName: this.employeeFilter() || undefined,
      partnerName: this.partnerFilter() || undefined,
      planCode: this.planFilter() || undefined,
    };
  }

  async loadFilters() {
    try {
      const res = await firstValueFrom(this.api.filters().pipe(timeout(15000)));
      if (res?.success === false) return;
      this.filterOptions.set({
        sourceTypes: res.sourceTypes ?? [],
        sourceNames: res.sourceNames ?? [],
        employeeNames: res.employeeNames ?? [],
        partnerNames: res.partnerNames ?? [],
        planCodes: res.planCodes ?? [],
      });
    } catch {
      /* optional */
    }
  }

  async load() {
    try {
      this.loading.set(true);
      const res = await firstValueFrom(
        this.api.list(this.search(), this.listQueryOpts()).pipe(timeout(30000)),
      );
      if (res?.success === false) {
        this.toast.error(res.message || 'Unable to load customers.');
        this.customers.set([]);
        this.total.set(0);
        return;
      }
      this.customers.set(res?.customers ?? []);
      this.total.set(res?.total ?? 0);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to load customers.'));
      this.customers.set([]);
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

  async exportExcel() {
    try {
      this.exporting.set(true);
      const res = await firstValueFrom(
        this.api.list(this.search(), this.listQueryOpts(true)).pipe(timeout(60000)),
      );
      const rows = (res?.customers ?? []) as LeadCustomerListItem[];
      exportToExcel(
        'lead-customers',
        [
          { header: 'Member ID', value: (r) => r.memberId },
          { header: 'Customer Name', value: (r) => r.fullName },
          { header: 'Mobile', value: (r) => r.phone },
          { header: 'Email', value: (r) => r.email },
          { header: 'Source Type', value: (r) => r.sourceType },
          { header: 'Source Name', value: (r) => r.sourceName ?? '' },
          { header: 'Source Code', value: (r) => r.sourceCode ?? '' },
          { header: 'Assigned Employee', value: (r) => r.assignedEmployee ?? '' },
          { header: 'Registration Date', value: (r) => r.registrationDate, type: 'datetime' },
          { header: 'Membership Status', value: (r) => r.membershipStatus ?? '' },
          { header: 'Plan', value: (r) => r.planName ?? '' },
          { header: 'Membership Sales', value: (r) => r.membershipSalesCount },
          { header: 'Report Purchases', value: (r) => r.reportPurchaseCount },
          { header: 'Reports Generated', value: (r) => r.reportGeneratedCount },
        ],
        rows,
      );
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Export failed.'));
    } finally {
      this.exporting.set(false);
    }
  }

  detailLink(c: LeadCustomerListItem): string[] {
    return ['/admin/lead-attribution/customers', c.userId];
  }
}
