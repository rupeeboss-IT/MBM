import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom, timeout } from 'rxjs';
import {
  CustomerReportService,
  type CustomerSearchHit,
} from '../../../core/services/customer-report.service';
import { AdminSessionService } from '../../../core/services/admin-session.service';
import { ToastService } from '../../../core/services/toast.service';
import { API_USER_MESSAGES } from '../../../core/utils/api-user-messages';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';

@Component({
  selector: 'app-admin-upload-reports',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './upload-reports.html',
  styleUrls: ['../../admin-shared.css'],
})
export class AdminUploadReports {
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

  readonly searching = signal(false);
  readonly uploading = signal(false);
  readonly results = signal<CustomerSearchHit[]>([]);
  readonly selected = signal<CustomerSearchHit | null>(null);
  readonly selectedFile = signal<File | null>(null);

  async search() {
    const v = this.searchForm.getRawValue();
    if (!v.memberId?.trim() && !v.mobile?.trim() && !v.email?.trim() && !v.customerName?.trim()) {
      this.toast.error('Enter at least one search field.');
      return;
    }
    try {
      this.searching.set(true);
      this.selected.set(null);
      const res = await firstValueFrom(
        this.api
          .adminSearchCustomers({
            memberId: v.memberId ?? '',
            mobile: v.mobile ?? '',
            email: v.email ?? '',
            customerName: v.customerName ?? '',
          })
          .pipe(timeout(20000))
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

  selectCustomer(c: CustomerSearchHit) {
    if (!c.hasActiveSubscription) {
      this.toast.error('Customer does not have an active subscription.');
      return;
    }
    this.selected.set(c);
  }

  onFileChange(ev: Event) {
    const input = ev.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    if (file && !file.name.toLowerCase().endsWith('.zip')) {
      this.toast.error('Only ZIP files are allowed.');
      input.value = '';
      this.selectedFile.set(null);
      return;
    }
    this.selectedFile.set(file);
  }

  async upload() {
    const customer = this.selected();
    const file = this.selectedFile();
    if (!customer) {
      this.toast.error('Select a customer with an active subscription.');
      return;
    }
    if (!file) {
      this.toast.error('Choose a ZIP file.');
      return;
    }
    try {
      this.uploading.set(true);
      const res = await firstValueFrom(
        this.api.adminUpload(customer.userId, file).pipe(timeout(120000))
      );
      if (!res?.success) {
        this.toast.error(res?.message || 'Upload failed.');
        return;
      }
      this.toast.success(res.message || 'Report uploaded successfully.');
      this.selectedFile.set(null);
      this.selected.set(null);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, API_USER_MESSAGES.reportUpload));
    } finally {
      this.uploading.set(false);
    }
  }

  logout() {
    this.adminSession.logout();
    void this.router.navigateByUrl('/admin-login');
  }
}
