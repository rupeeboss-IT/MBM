import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AdminListToolbar } from '../../core/components/admin-list-toolbar/admin-list-toolbar';
import { AuthService } from '../../core/services/auth.service';
import { UserManagementService, type UserManagementStats } from '../../core/services/user-management.service';
import { AdminSessionService } from '../../core/services/admin-session.service';
import { ToastService } from '../../core/services/toast.service';
import { API_USER_MESSAGES } from '../../core/utils/api-user-messages';
import { getHttpErrorMessage } from '../../core/utils/http-error-message';
import { CONTENT_COUNTS } from '../../data/content-counts';

type DashboardCounts = {
  success: boolean;
  message?: string;
  users: number;
  members: number;
  activeMembers: number;
  inactiveMembers: number;
  plans: number;
  paymentOrders: number;
  payments: number;
  userPlans: number;
  activeSubscriptions: number;
  expiringSoon: number;
  expiredSubscriptions: number;
  reportsGenerated: number;
  blogs: number;
  events: number;
  schemes: number;
};

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, AdminListToolbar],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.css'],
})
export class AdminDashboard {
  private readonly auth = inject(AuthService);
  private readonly userMgmt = inject(UserManagementService);
  private readonly session = inject(AdminSessionService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly counts = signal<DashboardCounts | null>(null);
  readonly userStats = signal<UserManagementStats | null>(null);
  readonly isSuperAdmin = this.session.isSuperAdmin;
  readonly dateFrom = signal('');
  readonly dateTo = signal('');

  async ngOnInit() {
    await this.refresh();
  }

  async refresh() {
    try {
      this.loading.set(true);
      const counts = (await firstValueFrom(
        this.auth.adminDashboardCounts({
          dateFrom: this.dateFrom() || undefined,
          dateTo: this.dateTo() || undefined,
        }),
      )) as DashboardCounts;
      this.counts.set({
        ...counts,
        blogs: CONTENT_COUNTS.blogs,
        events: CONTENT_COUNTS.events,
        schemes: CONTENT_COUNTS.schemes,
      });
      try {
        const um = await firstValueFrom(this.userMgmt.stats());
        if (um?.success && um.stats) this.userStats.set(um.stats);
      } catch {
        this.userStats.set(null);
      }
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, API_USER_MESSAGES.dashboard));
    } finally {
      this.loading.set(false);
    }
  }

  applyDateFilter() {
    void this.refresh();
  }

  openDetail(category: string, query?: Record<string, string>) {
    const extras = query ? { queryParams: query } : undefined;
    void this.router.navigate(['/admin-dashboard/detail', category], extras);
  }

  logout() {
    this.session.logout();
    this.toast.info('Logged out.');
    void this.router.navigateByUrl('/admin-login');
  }
}
