import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom, timeout } from 'rxjs';
import { AdminListToolbar } from '../../../core/components/admin-list-toolbar/admin-list-toolbar';
import { ToastService } from '../../../core/services/toast.service';
import {
  LeadAttributionService,
  type LeadAttributionStats,
  type LeadPerformer,
  type LeadPerformerPayment,
  type LeadSourceBreakdown,
} from '../../../core/services/lead-attribution.service';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';
import { LocalDatePipe } from '../../../core/pipes/local-date.pipe';

type PerformerModalKind = 'employee' | 'rba';

@Component({
  selector: 'app-lead-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, AdminListToolbar, LocalDatePipe],
  templateUrl: './lead-dashboard.html',
  styleUrls: ['../../admin-shared.css', './lead-dashboard.css'],
})
export class LeadDashboard {
  private readonly api = inject(LeadAttributionService);
  private readonly toast = inject(ToastService);

  readonly loading = signal(false);
  readonly stats = signal<LeadAttributionStats | null>(null);
  readonly breakdown = signal<LeadSourceBreakdown[]>([]);
  readonly topEmployees = signal<LeadPerformer[]>([]);
  readonly topPartners = signal<LeadPerformer[]>([]);
  readonly dateFrom = signal('');
  readonly dateTo = signal('');

  readonly performerModalOpen = signal(false);
  readonly performerModalLoading = signal(false);
  readonly performerModalKind = signal<PerformerModalKind>('employee');
  readonly performerModalTitle = signal('');
  readonly performerModalCode = signal('');
  readonly performerModalRevenue = signal(0);
  readonly performerModalPayments = signal<LeadPerformerPayment[]>([]);

  constructor() {
    void this.load();
  }

  async load() {
    try {
      this.loading.set(true);
      const res = await firstValueFrom(
        this.api.dashboard({
          dateFrom: this.dateFrom() || undefined,
          dateTo: this.dateTo() || undefined,
        }).pipe(timeout(30000)),
      );
      if (res?.success === false) {
        this.toast.error(res.message || 'Unable to load lead attribution dashboard.');
        return;
      }
      this.stats.set(res.stats ?? null);
      this.breakdown.set(res.sourceBreakdown ?? []);
      this.topEmployees.set(res.topEmployees ?? []);
      this.topPartners.set(res.topPartners ?? []);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to load lead attribution dashboard.'));
    } finally {
      this.loading.set(false);
    }
  }

  applyDateFilter() {
    void this.load();
  }

  breakdownPercent(count: number): number {
    const total = this.stats()?.totalLeads ?? 0;
    if (!total) return 0;
    return Math.round((count / total) * 100);
  }

  formatAmount(paise: number): string {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 2,
    }).format(paise / 100);
  }

  async openPerformer(kind: PerformerModalKind, performer: LeadPerformer) {
    if (!performer.code) return;

    this.performerModalOpen.set(true);
    this.performerModalKind.set(kind);
    this.performerModalTitle.set(performer.name);
    this.performerModalCode.set(performer.code);
    this.performerModalRevenue.set(performer.revenuePaise);
    this.performerModalPayments.set([]);
    this.performerModalLoading.set(true);

    try {
      const res = await firstValueFrom(
        this.api
          .performerDetails(kind, performer.code, {
            dateFrom: this.dateFrom() || undefined,
            dateTo: this.dateTo() || undefined,
          })
          .pipe(timeout(30000)),
      );
      if (res?.success === false) {
        this.toast.error(res.message || 'Unable to load performer details.');
        return;
      }
      this.performerModalTitle.set(res.performerName || performer.name);
      this.performerModalRevenue.set(res.totalRevenuePaise ?? performer.revenuePaise);
      this.performerModalPayments.set(res.payments ?? []);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to load performer details.'));
    } finally {
      this.performerModalLoading.set(false);
    }
  }

  closePerformerModal() {
    this.performerModalOpen.set(false);
  }

  trackPayment(_index: number, item: LeadPerformerPayment): string {
    return `${item.userId}-${item.paidAt}-${item.planCode}`;
  }
}
