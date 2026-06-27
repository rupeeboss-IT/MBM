import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom, timeout } from 'rxjs';
import {
  CustomerReportService,
  type CustomerReportListItem,
  type CustomerSearchHit,
} from '../../../core/services/customer-report.service';
import { AdminSessionService } from '../../../core/services/admin-session.service';
import { ToastService } from '../../../core/services/toast.service';
import { API_USER_MESSAGES } from '../../../core/utils/api-user-messages';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';
import { LocalDatePipe } from '../../../core/pipes/local-date.pipe';

@Component({
  selector: 'app-admin-generate-sdr',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, LocalDatePipe],
  templateUrl: './generate-sdr.html',
  styleUrls: ['../../admin-shared.css', './generate-sdr.css'],
})
export class AdminGenerateSdr {
  private readonly api = inject(CustomerReportService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly adminSession = inject(AdminSessionService);
  private readonly fb = inject(FormBuilder);

  readonly searchForm = this.fb.group({
    memberId: [''],
    mobile: [''],
    email: [''],
    customerName: [''],
  });

  readonly udyamForm = this.fb.group({
    udyamNumber: [''],
  });

  readonly searching = signal(false);
  readonly generating = signal(false);
  readonly loadingReports = signal(false);
  readonly downloadingId = signal<string | null>(null);
  readonly results = signal<CustomerSearchHit[]>([]);
  readonly selected = signal<CustomerSearchHit | null>(null);
  readonly existingReports = signal<CustomerReportListItem[]>([]);
  readonly lastGeneratedReportId = signal<string | null>(null);

  async search() {
    const v = this.searchForm.getRawValue();
    if (!v.memberId?.trim() && !v.mobile?.trim() && !v.email?.trim() && !v.customerName?.trim()) {
      this.toast.error('Enter at least one search field.');
      return;
    }
    try {
      this.searching.set(true);
      this.selected.set(null);
      this.existingReports.set([]);
      this.lastGeneratedReportId.set(null);
      const res = await firstValueFrom(
        this.api
          .adminSearchCustomers({
            memberId: v.memberId ?? '',
            mobile: v.mobile ?? '',
            email: v.email ?? '',
            customerName: v.customerName ?? '',
          })
          .pipe(timeout(20000)),
      );
      if (res?.success === false) {
        this.toast.error(res.message || 'Search failed.');
        this.results.set([]);
        return;
      }
      this.results.set(res?.customers ?? []);
      if ((res?.customers?.length ?? 0) === 0) this.toast.error('No customers found.');
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, API_USER_MESSAGES.reportSearch));
      this.results.set([]);
    } finally {
      this.searching.set(false);
    }
  }

  async selectCustomer(c: CustomerSearchHit) {
    this.selected.set(c);
    this.lastGeneratedReportId.set(null);
    this.udyamForm.reset({ udyamNumber: '' });
    await this.loadExistingReports(c.userId);
  }

  async loadExistingReports(customerId: string) {
    try {
      this.loadingReports.set(true);
      const res = await firstValueFrom(
        this.api.adminListCustomerSdrReports(customerId).pipe(timeout(20000)),
      );
      this.existingReports.set(res?.items ?? []);
    } catch {
      this.existingReports.set([]);
    } finally {
      this.loadingReports.set(false);
    }
  }

  async generate() {
    const customer = this.selected();
    const udyam = this.udyamForm.getRawValue().udyamNumber?.trim() ?? '';
    if (!customer) {
      this.toast.error('Select a customer first.');
      return;
    }
    if (!udyam) {
      this.toast.error('Enter the Udyam Registration Number.');
      return;
    }

    try {
      this.generating.set(true);
      const res = await firstValueFrom(
        this.api.adminGenerateSdr(customer.userId, udyam).pipe(timeout(180000)),
      );
      if (!res?.success || !res.reportId) {
        this.toast.error(res?.message || 'Report generation failed.');
        return;
      }
      this.toast.success(res.message || 'Report generated successfully.');
      this.lastGeneratedReportId.set(res.reportId);
      await this.loadExistingReports(customer.userId);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, API_USER_MESSAGES.sdrGenerate));
    } finally {
      this.generating.set(false);
    }
  }

  async downloadReport(item: CustomerReportListItem) {
    if (!item?.id) return;
    try {
      this.downloadingId.set(item.id);
      const blob = await firstValueFrom(
        this.api.adminDownloadReport(item.id).pipe(timeout(120000)),
      );
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = item.reportName || 'Government-Scheme-Discovery-Report.pdf';
      a.click();
      URL.revokeObjectURL(url);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, API_USER_MESSAGES.reportDownload));
    } finally {
      this.downloadingId.set(null);
    }
  }

  formatSize(bytes: number): string {
    if (!bytes) return '0 B';
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  logout() {
    this.adminSession.logout();
    void this.router.navigateByUrl('/admin-login');
  }
}
