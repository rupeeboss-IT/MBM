import { CommonModule } from '@angular/common';
import { afterNextRender, Component, DestroyRef, effect, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter, firstValueFrom, timeout } from 'rxjs';
import { AuthService, type MeRes } from '../../core/services/auth.service';
import { AuthSessionService } from '../../core/services/auth-session.service';
import { PaymentService, type ActivePlan, type InvoiceListItem } from '../../core/services/payment.service';
import {
  CustomerReportService,
  type CustomerReportListItem,
} from '../../core/services/customer-report.service';
import { ToastService } from '../../core/services/toast.service';
import { API_USER_MESSAGES } from '../../core/utils/api-user-messages';
import { getHttpErrorMessage } from '../../core/utils/http-error-message';
import { scrollToRouteFragment } from '../../core/utils/scroll-to-fragment';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './profile.html',
  styleUrl: './profile.css',
})
export class Profile {
  private readonly session = inject(AuthSessionService);
  private readonly router = inject(Router);
  private readonly api = inject(AuthService);
  private readonly payments = inject(PaymentService);
  private readonly reportsApi = inject(CustomerReportService);
  private readonly toast = inject(ToastService);

  readonly userId = this.session.userId;
  /** Signals so updates run change detection in zoneless mode (this app has no zone.js). */
  readonly profile = signal<MeRes | null>(null);
  readonly loading = signal(false);
  readonly activePlan = signal<ActivePlan | null>(null);
  readonly planLoading = signal(false);
  readonly invoices = signal<InvoiceListItem[]>([]);
  readonly invoicesLoading = signal(false);
  readonly invoiceDownloading = signal<string | null>(null);
  readonly reports = signal<CustomerReportListItem[]>([]);
  readonly reportsLoading = signal(false);
  readonly reportDownloading = signal<string | null>(null);
  private lastLoadedUserId: string | null = null;
  /** Ignores stale HTTP completions if userId/effect re-runs before the request finishes. */
  private loadGen = 0;

  constructor() {
    const destroyRef = inject(DestroyRef);

    afterNextRender(() => scrollToRouteFragment(this.router));
    const navSub = this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe(() => scrollToRouteFragment(this.router));
    destroyRef.onDestroy(() => navSub.unsubscribe());

    effect(() => {
      const id = this.userId();
      if (!id) return;
      if (this.lastLoadedUserId === id) return;
      void this.loadProfile();
      void this.loadActivePlan();
      void this.loadInvoices();
      void this.loadReports();
    });
  }

  private async loadProfile() {
    const gen = ++this.loadGen;
    try {
      this.loading.set(true);
      const data = await firstValueFrom(this.api.me().pipe(timeout(15000)));
      if (gen !== this.loadGen) return;
      this.profile.set(data);
      this.lastLoadedUserId = this.userId();
    } catch (e: unknown) {
      if (gen !== this.loadGen) return;
      this.toast.error(getHttpErrorMessage(e, API_USER_MESSAGES.profile));
    } finally {
      if (gen === this.loadGen) this.loading.set(false);
    }
  }

  private async loadActivePlan() {
    try {
      this.planLoading.set(true);
      const res = await firstValueFrom(this.payments.myPlan().pipe(timeout(15000)));
      this.activePlan.set(res?.plan ?? null);
    } catch {
      // Silent: profile is still useful without plan info.
      this.activePlan.set(null);
    } finally {
      this.planLoading.set(false);
    }
  }

  private async loadInvoices() {
    try {
      this.invoicesLoading.set(true);
      const res = await firstValueFrom(this.payments.listInvoices().pipe(timeout(15000)));
      this.invoices.set(res?.items ?? []);
    } catch {
      this.invoices.set([]);
    } finally {
      this.invoicesLoading.set(false);
    }
  }

  inr(paise: number): string {
    return new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 }).format(
      (paise ?? 0) / 100
    );
  }

  private async loadReports() {
    try {
      this.reportsLoading.set(true);
      const res = await firstValueFrom(this.reportsApi.listMyReports().pipe(timeout(15000)));
      this.reports.set(res?.items ?? []);
    } catch {
      this.reports.set([]);
    } finally {
      this.reportsLoading.set(false);
    }
  }

  formatFileSize(bytes: number): string {
    if (!bytes || bytes < 1024) return `${bytes || 0} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  async downloadReport(item: CustomerReportListItem) {
    if (!item?.id) return;
    try {
      this.reportDownloading.set(item.id);
      const blob = await firstValueFrom(this.reportsApi.downloadReport(item.id).pipe(timeout(120000)));
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = item.reportName || 'Report.zip';
      a.click();
      URL.revokeObjectURL(url);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, API_USER_MESSAGES.reportDownload));
    } finally {
      this.reportDownloading.set(null);
    }
  }

  async downloadInvoice(item: InvoiceListItem) {
    if (!item?.paymentId) return;
    try {
      this.invoiceDownloading.set(item.paymentId);
      const blob = await firstValueFrom(this.payments.downloadInvoice(item.paymentId).pipe(timeout(30000)));
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${item.invoiceNumber}.pdf`;
      a.click();
      URL.revokeObjectURL(url);
    } catch (e: any) {
      const msg =
        (e?.error?.message as string | undefined) ||
        e?.message ||
        'Could not download invoice.';
      this.toast.error(msg);
    } finally {
      this.invoiceDownloading.set(null);
    }
  }

  logout() {
    this.session.logout();
    this.router.navigateByUrl('/home');
  }
}

