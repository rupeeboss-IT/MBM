import { CommonModule } from '@angular/common';
import { afterNextRender, Component, DestroyRef, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
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
import { LocalDatePipe } from '../../core/pipes/local-date.pipe';
import { SchemeDiscoveryReportAccess } from '../../core/components/scheme-discovery-report-access/scheme-discovery-report-access';
import { MembershipCard } from '../../core/components/membership-card/membership-card';
import { formatMemberPartnerId, getIdLabel } from '../../core/utils/member-id-display.util';
import {
  ConnectService,
  type ConnectIncomingItem,
  type ConnectProfileDetail,
} from '../../core/services/connect.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, RouterLink, LocalDatePipe, SchemeDiscoveryReportAccess, MembershipCard, FormsModule],
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
  private readonly connectApi = inject(ConnectService);

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
  readonly connectIncoming = signal<ConnectIncomingItem[]>([]);
  readonly connectIncomingLoading = signal(false);
  readonly connectProfile = signal<ConnectProfileDetail | null>(null);
  readonly connectProfileLoading = signal(false);
  readonly connectSaving = signal(false);
  readonly connectSector = signal('');
  readonly connectCity = signal('');
  readonly connectState = signal('');
  readonly connectBusinessType = signal('');
  readonly connectDescription = signal('');
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
      void this.loadConnectIncoming();
      void this.loadConnectProfile();
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

  reportTypeLabel(reportType?: string): string {
    if (!reportType || reportType === 'Credit') return 'Credit Report';
    if (reportType === 'SchemeDiscovery' || reportType === 'SDR') return 'Government Scheme Discovery';
    return reportType;
  }

  formatFileSize(bytes: number): string {
    if (!bytes || bytes < 1024) return `${bytes || 0} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  idLabel(role?: string | null): string {
    return getIdLabel(role ?? null);
  }

  idValue(memberId?: string | null, role?: string | null): string {
    return formatMemberPartnerId(memberId ?? null, role ?? null) ?? '—';
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

  readonly connectDisplay = ConnectService.display;

  connectInitials(name?: string | null): string {
    const s = (name ?? '').trim();
    if (!s) return '?';
    const parts = s.split(/\s+/).filter(Boolean);
    if (parts.length >= 2) return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    return s.slice(0, 2).toUpperCase();
  }

  private async loadConnectIncoming() {
    try {
      this.connectIncomingLoading.set(true);
      const res = await firstValueFrom(this.connectApi.incoming().pipe(timeout(15000)));
      this.connectIncoming.set(res?.items ?? []);
    } catch {
      this.connectIncoming.set([]);
    } finally {
      this.connectIncomingLoading.set(false);
    }
  }

  private async loadConnectProfile() {
    try {
      this.connectProfileLoading.set(true);
      const res = await firstValueFrom(this.connectApi.getMyProfile().pipe(timeout(15000)));
      const p = res?.profile ?? null;
      this.connectProfile.set(p);
      if (p) {
        this.connectSector.set(p.sector && p.sector !== '-' ? p.sector : '');
        this.connectCity.set(p.city && p.city !== '-' ? p.city : '');
        this.connectState.set(p.state && p.state !== '-' ? p.state : '');
        this.connectBusinessType.set(p.businessType && p.businessType !== '-' ? p.businessType : '');
        this.connectDescription.set(p.description && p.description !== '-' ? p.description : '');
      }
    } catch {
      this.connectProfile.set(null);
    } finally {
      this.connectProfileLoading.set(false);
    }
  }

  async acceptConnect(req: ConnectIncomingItem) {
    try {
      const res = await firstValueFrom(this.connectApi.acceptRequest(req.requestId).pipe(timeout(15000)));
      if (res?.success === false) {
        this.toast.error(res.message || 'Unable to accept request.');
        return;
      }
      this.toast.success(res.message || 'Connection accepted.');
      await this.loadConnectIncoming();
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to accept request.'));
    }
  }

  async rejectConnect(req: ConnectIncomingItem) {
    try {
      const res = await firstValueFrom(this.connectApi.rejectRequest(req.requestId).pipe(timeout(15000)));
      if (res?.success === false) {
        this.toast.error(res.message || 'Unable to reject request.');
        return;
      }
      this.toast.success(res.message || 'Request rejected.');
      await this.loadConnectIncoming();
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to reject request.'));
    }
  }

  async saveConnectProfile() {
    try {
      this.connectSaving.set(true);
      const res = await firstValueFrom(
        this.connectApi
          .updateMyProfile({
            sector: this.connectSector() || null,
            city: this.connectCity() || null,
            state: this.connectState() || null,
            businessType: this.connectBusinessType() || null,
            description: this.connectDescription() || null,
          })
          .pipe(timeout(15000)),
      );
      if (res?.success === false) {
        this.toast.error(res.message || 'Unable to save Connect profile.');
        return;
      }
      this.toast.success('Connect profile updated.');
      await this.loadConnectProfile();
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to save Connect profile.'));
    } finally {
      this.connectSaving.set(false);
    }
  }
}

