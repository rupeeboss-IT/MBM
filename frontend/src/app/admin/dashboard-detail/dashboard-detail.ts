import { CommonModule } from '@angular/common';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { combineLatest, firstValueFrom, map } from 'rxjs';
import { AdminListToolbar } from '../../core/components/admin-list-toolbar/admin-list-toolbar';
import { AdminPagination } from '../../core/components/admin-pagination/admin-pagination';
import { AuthService } from '../../core/services/auth.service';
import { ArticlesService } from '../../core/services/articles.service';
import { EventsService } from '../../core/services/events.service';
import { SchemesService } from '../../core/services/schemes.service';
import { ToastService } from '../../core/services/toast.service';
import { API_USER_MESSAGES } from '../../core/utils/api-user-messages';
import { exportToExcel, type ExportColumn } from '../../core/utils/admin-excel-export';
import { ADMIN_DEFAULT_PAGE_SIZE, sortIndicator, toggleColumnSort } from '../../core/utils/admin-list-params';
import { getHttpErrorMessage } from '../../core/utils/http-error-message';
import { LocalDatePipe } from '../../core/pipes/local-date.pipe';

export type DashboardDetailRes = {
  success: boolean;
  message?: string;
  category?: string;
  title?: string;
  total?: number;
  page?: number;
  pageSize?: number;
  users?: Array<{
    userId: string;
    role: string;
    fullName: string;
    email: string;
    phone: string;
    isActive: boolean;
    createdAt: string;
  }>;
  members?: Array<{
    userId: string;
    role: string;
    fullName: string;
    email: string;
    phone: string;
    isActive: boolean;
    createdAt: string;
    planCode?: string | null;
    planName?: string | null;
    advisorCode?: string | null;
  }>;
  plans?: Array<{
    planId: number;
    code: string;
    name: string;
    totalAmountPaise: number;
    durationDays: number;
    isActive: boolean;
  }>;
  paymentOrders?: Array<{
    paymentOrderId: string;
    userId: string;
    fullName: string;
    email: string;
    planCode: string;
    planName: string;
    totalAmountPaise: number;
    status: string;
    createdAt: string;
  }>;
  payments?: Array<{
    paymentId: string;
    paymentOrderId: string;
    fullName: string;
    email: string;
    planCode: string;
    amountPaise: number;
    status: string;
    paidAt: string;
  }>;
  subscriptions?: Array<{
    userPlanId: string;
    userId: string;
    fullName: string;
    email: string;
    phone: string;
    planCode: string;
    planName: string;
    activeFrom: string;
    activeTo?: string | null;
    status: string;
    daysRemaining?: number | null;
  }>;
  contentItems?: Array<{
    slug: string;
    title: string;
    subtitle: string;
    meta: string;
    category: string;
    publicPath: string;
  }>;
};

type ContentItem = NonNullable<DashboardDetailRes['contentItems']>[number];

@Component({
  selector: 'app-admin-dashboard-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, LocalDatePipe, AdminPagination, AdminListToolbar],
  templateUrl: './dashboard-detail.html',
  styleUrls: ['./dashboard-detail.css'],
})
export class AdminDashboardDetail {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly articles = inject(ArticlesService);
  private readonly events = inject(EventsService);
  private readonly schemes = inject(SchemesService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(false);
  readonly exporting = signal(false);
  readonly data = signal<DashboardDetailRes | null>(null);
  readonly search = signal('');
  readonly page = signal(1);
  readonly pageSize = ADMIN_DEFAULT_PAGE_SIZE;
  readonly total = signal(0);
  readonly category = signal('');
  readonly dateFrom = signal('');
  readonly dateTo = signal('');
  readonly sortPreset = signal('latest');
  readonly sortBy = signal('created');
  readonly sortDir = signal<'asc' | 'desc'>('desc');
  readonly statusFilter = signal('');
  readonly days = signal<number | undefined>(undefined);

  readonly isPaymentOrders = computed(() => this.category() === 'payment-orders');

  readonly sortIndicator = sortIndicator;
  readonly title = computed(() => this.data()?.title ?? 'Details');

  readonly isStaticContent = computed(() =>
    ['events', 'schemes'].includes(this.category()),
  );

  readonly filteredContentItems = computed(() => {
    const items = this.filterList(this.data()?.contentItems ?? [], [
      'title',
      'subtitle',
      'meta',
      'category',
      'slug',
    ]);
    return this.sortContentItems(this.filterContentByDate(items));
  });

  readonly pagedContentItems = computed(() => {
    const items = this.filteredContentItems();
    const start = (this.page() - 1) * this.pageSize;
    return items.slice(start, start + this.pageSize);
  });

  readonly staticContentTotal = computed(() => {
    if (!this.isStaticContent()) return this.total();
    return this.filteredContentItems().length;
  });

  constructor() {
    combineLatest([this.route.paramMap, this.route.queryParamMap])
      .pipe(
        map(([params, query]) => ({
          category: (params.get('category') ?? '').toLowerCase(),
          page: Math.max(1, Number(query.get('page') ?? 1) || 1),
          search: (query.get('search') ?? '').trim(),
          days: query.get('days'),
          dateFrom: (query.get('dateFrom') ?? '').trim(),
          dateTo: (query.get('dateTo') ?? '').trim(),
          sortPreset: (query.get('sortPreset') ?? query.get('sortBy') ?? 'latest').trim() || 'latest',
          sortBy: (query.get('sortBy') ?? 'created').trim() || 'created',
          sortDir: ((query.get('sortDir') ?? 'desc').trim() || 'desc') as 'asc' | 'desc',
          status: (query.get('status') ?? '').trim(),
        })),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((state) => {
        this.category.set(state.category);
        this.page.set(state.page);
        this.search.set(state.search);
        this.dateFrom.set(state.dateFrom);
        this.dateTo.set(state.dateTo);
        this.sortPreset.set(
          ['latest', 'oldest', 'name_asc', 'name_desc'].includes(state.sortPreset)
            ? state.sortPreset
            : '',
        );
        this.sortBy.set(state.sortBy);
        this.sortDir.set(state.sortDir);
        this.statusFilter.set(state.status);
        this.days.set(state.days ? Number(state.days) : undefined);
        void this.load();
      });
  }

  listQueryOpts(exportAll = false) {
    const preset = this.sortPreset();
    const usePreset = ['latest', 'oldest', 'name_asc', 'name_desc'].includes(preset);
    return {
      days: this.days(),
      page: exportAll ? 1 : this.page(),
      pageSize: this.pageSize,
      search: this.search(),
      dateFrom: this.dateFrom() || undefined,
      dateTo: this.dateTo() || undefined,
      sortBy: usePreset ? preset : this.sortBy(),
      sortDir: usePreset ? undefined : this.sortDir(),
      status: this.statusFilter() || undefined,
      export: exportAll || undefined,
    };
  }

  onStatusChange(value: string) {
    this.statusFilter.set(value);
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { page: 1, status: value || null },
      queryParamsHandling: 'merge',
    });
  }

  async load() {
    const category = this.category();
    if (!category) {
      await this.router.navigateByUrl('/admin-dashboard');
      return;
    }

    const staticDetail = this.loadStaticContent(category);
    if (staticDetail) {
      this.data.set(staticDetail);
      this.total.set(this.filteredContentItems().length);
      return;
    }

    try {
      this.loading.set(true);
      const res = (await firstValueFrom(
        this.auth.adminDashboardDetail(category, this.listQueryOpts()),
      )) as DashboardDetailRes;

      if (!res?.success) {
        this.toast.error(res?.message || 'Could not load details.');
        await this.router.navigateByUrl('/admin-dashboard');
        return;
      }

      this.data.set(res);
      this.total.set(res.total ?? 0);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, API_USER_MESSAGES.dashboard));
      await this.router.navigateByUrl('/admin-dashboard');
    } finally {
      this.loading.set(false);
    }
  }

  applyFilters() {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        page: 1,
        search: this.search().trim() || null,
        dateFrom: this.dateFrom() || null,
        dateTo: this.dateTo() || null,
        sortPreset: this.sortPreset() || null,
        sortBy: this.sortPreset() ? null : this.sortBy(),
        sortDir: this.sortPreset() ? null : this.sortDir(),
        status: this.statusFilter() || null,
      },
      queryParamsHandling: 'merge',
    });
  }

  onSortPresetChange(value: string) {
    this.sortPreset.set(value);
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { page: 1, sortPreset: value || null, sortBy: null, sortDir: null },
      queryParamsHandling: 'merge',
    });
  }

  onColumnSort(column: string) {
    const next = toggleColumnSort(this.sortBy(), this.sortDir(), column);
    this.sortPreset.set('');
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        page: 1,
        sortPreset: null,
        sortBy: next.sortBy,
        sortDir: next.sortDir,
      },
      queryParamsHandling: 'merge',
    });
  }

  runSearch() {
    this.applyFilters();
  }

  onPageChange(next: number) {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { page: next },
      queryParamsHandling: 'merge',
    });
  }

  async exportExcel() {
    const category = this.category();
    if (!category) return;

    if (this.isStaticContent()) {
      const rows = this.filteredContentItems();
      if (!rows.length) {
        this.toast.warning('No rows to export for the current filters.');
        return;
      }
      await exportToExcel(
        category,
        [
          { header: 'Title', value: (r) => r.title },
          { header: 'Summary', value: (r) => r.subtitle },
          { header: 'Category', value: (r) => r.category },
          { header: 'Date / Meta', value: (r) => r.meta },
          { header: 'Slug', value: (r) => r.slug },
        ],
        rows,
        { sheetTitle: this.title() },
      );
      this.toast.success(`Exported ${rows.length} item(s).`);
      return;
    }

    try {
      this.exporting.set(true);
      const res = (await firstValueFrom(
        this.auth.adminDashboardDetail(category, this.listQueryOpts(true)),
      )) as DashboardDetailRes;
      const exported = this.extractExportRows(res);
      if (!exported.rows.length) {
        this.toast.warning('No rows to export for the current filters.');
        return;
      }
      await exportToExcel(category, exported.columns, exported.rows, {
        sheetTitle: this.title(),
      });
      this.toast.success(`Exported ${exported.rows.length} row(s).`);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Could not export data.'));
    } finally {
      this.exporting.set(false);
    }
  }

  inr(paise: number): string {
    return new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 }).format(
      (paise ?? 0) / 100,
    );
  }

  expiryClass(days: number | null | undefined): string {
    if (days == null) return '';
    if (days <= 7) return 'badge badge-danger';
    if (days <= 30) return 'badge badge-warn';
    return 'badge badge-ok';
  }

  orderStatusBadge(status: string): string {
    const s = (status ?? '').toLowerCase();
    if (s === 'paid') return 'badge-success';
    if (s === 'failed') return 'badge-error';
    if (s === 'created') return 'badge-warning';
    return 'badge-neutral';
  }

  activeBadge(isActive: boolean): string {
    return isActive ? 'badge-success' : 'badge-error';
  }

  memberHasPlan(u: { planName?: string | null; planCode?: string | null }): boolean {
    return !!(u.planName?.trim() || u.planCode?.trim());
  }

  memberPlanLabel(u: { planName?: string | null; planCode?: string | null }): string {
    const name = (u.planName ?? '').trim();
    if (name) return name;
    return (u.planCode ?? '').trim();
  }

  private extractExportRows(res: DashboardDetailRes): {
    columns: ExportColumn<unknown>[];
    rows: unknown[];
  } {
    type UserRow = NonNullable<DashboardDetailRes['users']>[number];
    type MemberRow = NonNullable<DashboardDetailRes['members']>[number];
    type PlanRow = NonNullable<DashboardDetailRes['plans']>[number];
    type OrderRow = NonNullable<DashboardDetailRes['paymentOrders']>[number];
    type PaymentRow = NonNullable<DashboardDetailRes['payments']>[number];
    type SubRow = NonNullable<DashboardDetailRes['subscriptions']>[number];

    if (res.users?.length) {
      return {
        columns: [
          { header: 'Name', value: (r) => (r as UserRow).fullName },
          { header: 'Email', value: (r) => (r as UserRow).email },
          { header: 'Phone', value: (r) => (r as UserRow).phone },
          { header: 'Role', value: (r) => (r as UserRow).role },
          { header: 'Status', value: (r) => ((r as UserRow).isActive ? 'Active' : 'Inactive') },
          { header: 'Joined', value: (r) => (r as UserRow).createdAt, type: 'datetime' },
        ],
        rows: res.users,
      };
    }
    if (res.members?.length) {
      return {
        columns: [
          { header: 'Name', value: (r) => (r as MemberRow).fullName },
          { header: 'Email', value: (r) => (r as MemberRow).email },
          { header: 'Phone', value: (r) => (r as MemberRow).phone },
          { header: 'Role', value: (r) => (r as MemberRow).role },
          { header: 'Plan', value: (r) => this.memberPlanLabel(r as MemberRow) || 'No plans' },
          { header: 'Advisor code', value: (r) => (r as MemberRow).advisorCode || '—' },
          { header: 'Status', value: (r) => ((r as MemberRow).isActive ? 'Active' : 'Inactive') },
          { header: 'Joined', value: (r) => (r as MemberRow).createdAt, type: 'datetime' },
        ],
        rows: res.members,
      };
    }
    if (res.plans?.length) {
      return {
        columns: [
          { header: 'Code', value: (r) => (r as PlanRow).code },
          { header: 'Name', value: (r) => (r as PlanRow).name },
          { header: 'Price (INR)', value: (r) => (r as PlanRow).totalAmountPaise, type: 'currency_paise' },
          { header: 'Duration (days)', value: (r) => (r as PlanRow).durationDays, type: 'number' },
          { header: 'Status', value: (r) => ((r as PlanRow).isActive ? 'Active' : 'Inactive') },
        ],
        rows: res.plans,
      };
    }
    if (res.paymentOrders?.length) {
      return {
        columns: [
          { header: 'Member', value: (r) => (r as OrderRow).fullName },
          { header: 'Email', value: (r) => (r as OrderRow).email },
          { header: 'Plan', value: (r) => (r as OrderRow).planName },
          { header: 'Amount (INR)', value: (r) => (r as OrderRow).totalAmountPaise, type: 'currency_paise' },
          { header: 'Status', value: (r) => (r as OrderRow).status },
          { header: 'Date', value: (r) => (r as OrderRow).createdAt, type: 'datetime' },
        ],
        rows: res.paymentOrders,
      };
    }
    if (res.payments?.length) {
      return {
        columns: [
          { header: 'Member', value: (r) => (r as PaymentRow).fullName },
          { header: 'Email', value: (r) => (r as PaymentRow).email },
          { header: 'Plan', value: (r) => (r as PaymentRow).planCode },
          { header: 'Amount (INR)', value: (r) => (r as PaymentRow).amountPaise, type: 'currency_paise' },
          { header: 'Status', value: (r) => (r as PaymentRow).status },
          { header: 'Paid at', value: (r) => (r as PaymentRow).paidAt, type: 'datetime' },
        ],
        rows: res.payments,
      };
    }
    if (res.subscriptions?.length) {
      return {
        columns: [
          { header: 'Member', value: (r) => (r as SubRow).fullName },
          { header: 'Email', value: (r) => (r as SubRow).email },
          { header: 'Phone', value: (r) => (r as SubRow).phone },
          { header: 'Plan', value: (r) => (r as SubRow).planName },
          { header: 'Started', value: (r) => (r as SubRow).activeFrom, type: 'datetime' },
          { header: 'Ends', value: (r) => (r as SubRow).activeTo ?? '', type: 'datetime' },
          { header: 'Days left', value: (r) => (r as SubRow).daysRemaining ?? '', type: 'number' },
          { header: 'Status', value: (r) => (r as SubRow).status },
        ],
        rows: res.subscriptions,
      };
    }
    return { columns: [], rows: [] };
  }

  private loadStaticContent(cat: string): DashboardDetailRes | null {
    if (cat === 'blogs') {
      // Blogs are now managed dynamically — redirect to the blog management page.
      void this.router.navigateByUrl('/admin/blog-management');
      return null;
    }
    if (cat === 'events') {
      // Events are now managed dynamically — redirect to the event management page.
      void this.router.navigateByUrl('/admin/event-management');
      return null;
    }
    if (cat === 'schemes') {
      return {
        success: true,
        category: cat,
        title: 'Government Schemes',
        contentItems: this.schemes.getAllSchemes().map(({ slug, data }) => ({
          slug,
          title: data.title,
          subtitle: data.subtitle,
          meta: data.crumb,
          category: 'Scheme',
          publicPath: `/scheme/${slug}`,
        })),
      };
    }
    return null;
  }

  private filterList<T extends Record<string, unknown>>(list: T[], fields: string[]): T[] {
    const q = this.search().trim().toLowerCase();
    if (!q) return list;
    return list.filter((row) =>
      fields.some((f) => String(row[f] ?? '').toLowerCase().includes(q)),
    );
  }

  private filterContentByDate(items: ContentItem[]): ContentItem[] {
    const from = this.dateFrom();
    const to = this.dateTo();
    if (!from && !to) return items;
    const fromMs = from ? Date.parse(from) : Number.NEGATIVE_INFINITY;
    const toMs = to ? Date.parse(to) + 86_400_000 - 1 : Number.POSITIVE_INFINITY;
    return items.filter((item) => {
      const metaMs = Date.parse(item.meta);
      if (!Number.isNaN(metaMs)) return metaMs >= fromMs && metaMs <= toMs;
      if (from && item.meta.includes(from.slice(0, 4))) return true;
      return !from && !to;
    });
  }

  private sortContentItems(items: ContentItem[]): ContentItem[] {
    const preset = this.sortPreset();
    const by = this.sortBy();
    const dir = this.sortDir();
    const asc = preset === 'name_asc' || preset === 'oldest' || dir === 'asc';
    const field =
      preset === 'name_asc' || preset === 'name_desc' || by === 'name' || by === 'title'
        ? 'title'
        : preset === 'oldest' || preset === 'latest' || by === 'created' || by === 'meta'
          ? 'meta'
          : by;
    const sorted = [...items].sort((a, b) => {
      const av = String(a[field as keyof ContentItem] ?? '');
      const bv = String(b[field as keyof ContentItem] ?? '');
      return av.localeCompare(bv, undefined, { numeric: true });
    });
    return asc ? sorted : sorted.reverse();
  }
}
