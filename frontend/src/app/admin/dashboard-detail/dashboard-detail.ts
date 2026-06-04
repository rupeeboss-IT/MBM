import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { ArticlesService } from '../../core/services/articles.service';
import { EventsService } from '../../core/services/events.service';
import { SchemesService } from '../../core/services/schemes.service';
import { ToastService } from '../../core/services/toast.service';
import { API_USER_MESSAGES } from '../../core/utils/api-user-messages';
import { getHttpErrorMessage } from '../../core/utils/http-error-message';

export type DashboardDetailRes = {
  success: boolean;
  message?: string;
  category?: string;
  title?: string;
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

@Component({
  selector: 'app-admin-dashboard-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard-detail.html',
  styleUrls: ['./dashboard-detail.css'],
})
export class AdminDashboardDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly articles = inject(ArticlesService);
  private readonly events = inject(EventsService);
  private readonly schemes = inject(SchemesService);
  private readonly toast = inject(ToastService);

  readonly loading = signal(false);
  readonly data = signal<DashboardDetailRes | null>(null);
  readonly search = signal('');

  readonly category = computed(() => (this.route.snapshot.paramMap.get('category') ?? '').toLowerCase());

  readonly title = computed(() => this.data()?.title ?? 'Details');

  readonly filteredUsers = computed(() => this.filterList(this.data()?.users ?? [], ['fullName', 'email', 'phone', 'role']));
  readonly filteredMembers = computed(() => this.filterList(this.data()?.members ?? [], ['fullName', 'email', 'phone', 'role']));
  readonly filteredPlans = computed(() => this.filterList(this.data()?.plans ?? [], ['code', 'name']));
  readonly filteredPaymentOrders = computed(() =>
    this.filterList(this.data()?.paymentOrders ?? [], ['fullName', 'email', 'planCode', 'planName', 'status'])
  );
  readonly filteredPayments = computed(() =>
    this.filterList(this.data()?.payments ?? [], ['fullName', 'email', 'planCode', 'status'])
  );
  readonly filteredSubscriptions = computed(() =>
    this.filterList(this.data()?.subscriptions ?? [], ['fullName', 'email', 'phone', 'planCode', 'planName', 'status'])
  );
  readonly filteredContentItems = computed(() =>
    this.filterList(this.data()?.contentItems ?? [], ['title', 'subtitle', 'meta', 'category', 'slug'])
  );

  ngOnInit(): void {
    void this.load();
  }

  async load() {
    const cat = this.category();
    if (!cat) {
      this.router.navigateByUrl('/admin-dashboard');
      return;
    }

    const staticDetail = this.loadStaticContent(cat);
    if (staticDetail) {
      this.data.set(staticDetail);
      return;
    }

    try {
      this.loading.set(true);
      const days = this.route.snapshot.queryParamMap.get('days');
      const res = (await firstValueFrom(
        this.auth.adminDashboardDetail(cat, days ? { days: Number(days) } : undefined)
      )) as DashboardDetailRes;
      if (!res?.success) {
        this.toast.error(res?.message || 'Could not load details.');
        this.router.navigateByUrl('/admin-dashboard');
        return;
      }
      this.data.set(res);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, API_USER_MESSAGES.dashboard));
      this.router.navigateByUrl('/admin-dashboard');
    } finally {
      this.loading.set(false);
    }
  }

  inr(paise: number): string {
    return new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 }).format(
      (paise ?? 0) / 100
    );
  }

  expiryClass(days: number | null | undefined): string {
    if (days == null) return '';
    if (days <= 7) return 'badge badge-danger';
    if (days <= 30) return 'badge badge-warn';
    return 'badge badge-ok';
  }

  private loadStaticContent(cat: string): DashboardDetailRes | null {
    if (cat === 'blogs') {
      return {
        success: true,
        category: cat,
        title: 'Blogs & News Articles',
        contentItems: this.articles.getAllArticles().map(({ slug, data }) => ({
          slug,
          title: data.title,
          subtitle: data.summary,
          meta: data.dateLabel,
          category: data.category,
          publicPath: `/article/${slug}`,
        })),
      };
    }
    if (cat === 'events') {
      return {
        success: true,
        category: cat,
        title: 'Events',
        contentItems: this.events.getAllEvents().map(({ slug, data }) => ({
          slug,
          title: data.title,
          subtitle: data.subtitle,
          meta: data.date,
          category: data.type,
          publicPath: `/event/${slug}`,
        })),
      };
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
      fields.some((f) => String(row[f] ?? '').toLowerCase().includes(q))
    );
  }
}
