import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom, timeout } from 'rxjs';
import { ToastService } from '../../../core/services/toast.service';
import {
  EnquiryManagementService,
  type EnquiryDetail as EnquiryDetailModel,
} from '../../../core/services/enquiry-management.service';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';
import { LocalDatePipe } from '../../../core/pipes/local-date.pipe';

@Component({
  selector: 'app-enquiry-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, LocalDatePipe],
  templateUrl: './enquiry-detail.html',
  styleUrls: ['../../admin-shared.css', './enquiry-detail.css'],
})
export class EnquiryDetail {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(EnquiryManagementService);
  private readonly toast = inject(ToastService);

  readonly loading = signal(true);
  readonly actionSubmitting = signal(false);
  readonly enquiry = signal<EnquiryDetailModel | null>(null);
  readonly statusRemarks = signal('');

  readonly listLink = ['/admin/enquiry-management/enquiries'];

  constructor() {
    void this.load();
  }

  async load() {
    const idRaw = this.route.snapshot.paramMap.get('enquiryId');
    const enquiryId = Number(idRaw);
    if (!enquiryId || Number.isNaN(enquiryId)) {
      void this.router.navigate(this.listLink);
      return;
    }

    try {
      this.loading.set(true);
      const res = await firstValueFrom(this.api.get(enquiryId).pipe(timeout(20000)));
      if (res?.success === false || !res.enquiry) {
        this.toast.error(res?.message || 'Enquiry not found.');
        void this.router.navigate(this.listLink);
        return;
      }
      this.enquiry.set(res.enquiry);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to load enquiry.'));
      void this.router.navigate(this.listLink);
    } finally {
      this.loading.set(false);
    }
  }

  statusBadgeClass(status: string): string {
    switch (status) {
      case 'New':
        return 'badge badge-danger';
      case 'Read':
        return 'badge badge-info';
      case 'In Progress':
        return 'badge badge-warning';
      case 'Resolved':
        return 'badge badge-success';
      case 'Closed':
        return 'badge badge-neutral';
      default:
        return 'badge badge-neutral';
    }
  }

  async updateStatus(status: string) {
    const item = this.enquiry();
    if (!item) return;
    try {
      this.actionSubmitting.set(true);
      const res = await firstValueFrom(
        this.api.updateStatus(item.id, status, this.statusRemarks()).pipe(timeout(15000)),
      );
      if (res?.success === false) {
        this.toast.error(res.message || 'Unable to update status.');
        return;
      }
      this.toast.success(res.message || 'Status updated.');
      this.statusRemarks.set('');
      await this.load();
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to update status.'));
    } finally {
      this.actionSubmitting.set(false);
    }
  }
}
