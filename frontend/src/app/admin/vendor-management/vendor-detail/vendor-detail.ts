import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom, timeout } from 'rxjs';
import { AdminPagination } from '../../../core/components/admin-pagination/admin-pagination';
import { AdminSessionService } from '../../../core/services/admin-session.service';
import { ToastService } from '../../../core/services/toast.service';
import {
  VendorManagementService,
  type VendorAuditItem,
  type VendorDetail as VendorDetailModel,
  type VendorFormBody,
  type VendorPlan,
} from '../../../core/services/vendor-management.service';
import { ADMIN_DEFAULT_PAGE_SIZE } from '../../../core/utils/admin-list-params';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';
import { LocalDatePipe } from '../../../core/pipes/local-date.pipe';

@Component({
  selector: 'app-vendor-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, LocalDatePipe, AdminPagination],
  templateUrl: './vendor-detail.html',
  styleUrls: ['../../admin-shared.css', './vendor-detail.css'],
})
export class VendorDetail {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(VendorManagementService);
  private readonly session = inject(AdminSessionService);
  private readonly toast = inject(ToastService);

  readonly isSuperAdmin = computed(() => this.session.isSuperAdmin());
  readonly loading = signal(true);
  readonly vendor = signal<VendorDetailModel | null>(null);
  readonly plans = signal<VendorPlan[]>([]);
  readonly auditItems = signal<VendorAuditItem[]>([]);
  readonly auditTotal = signal(0);
  readonly auditPage = signal(1);
  readonly auditPageSize = ADMIN_DEFAULT_PAGE_SIZE;

  readonly editOpen = signal(false);
  readonly editSubmitting = signal(false);
  readonly formServiceName = signal('');
  readonly formCompanyName = signal('');
  readonly formContactPerson = signal('');
  readonly formMobile = signal('');
  readonly formAltMobile = signal('');
  readonly formEmail = signal('');
  readonly formAltEmail = signal('');
  readonly formWebsite = signal('');
  readonly formAddress = signal('');
  readonly formRemarks = signal('');
  readonly formIsActive = signal(true);
  readonly formPlanIds = signal<number[]>([]);

  readonly deactivateOpen = signal(false);
  readonly deactivateRemarks = signal('');
  readonly deleteOpen = signal(false);
  readonly deleteRemarks = signal('');
  readonly actionSubmitting = signal(false);

  readonly listLink = ['/admin/vendor-management/vendors'];

  constructor() {
    void this.loadPlans();
    void this.load();
  }

  async loadPlans() {
    try {
      const res = await firstValueFrom(this.api.listPlans().pipe(timeout(15000)));
      this.plans.set(res?.plans ?? []);
    } catch {
      this.plans.set([]);
    }
  }

  async load() {
    const vendorId = this.route.snapshot.paramMap.get('vendorId');
    if (!vendorId) {
      void this.router.navigate(this.listLink);
      return;
    }

    try {
      this.loading.set(true);
      const res = await firstValueFrom(this.api.getVendor(vendorId).pipe(timeout(20000)));
      if (res?.success === false || !res.vendor) {
        this.toast.error(res?.message || 'Vendor not found.');
        void this.router.navigate(this.listLink);
        return;
      }
      this.vendor.set(res.vendor);
      await this.loadAudit();
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to load vendor.'));
      void this.router.navigate(this.listLink);
    } finally {
      this.loading.set(false);
    }
  }

  async loadAudit() {
    const vendorId = this.vendor()?.vendorId;
    if (!vendorId) return;

    try {
      const res = await firstValueFrom(
        this.api.audit(vendorId, this.auditPage(), this.auditPageSize).pipe(timeout(20000)),
      );
      this.auditItems.set(res?.items ?? []);
      this.auditTotal.set(res?.total ?? 0);
    } catch {
      this.auditItems.set([]);
      this.auditTotal.set(0);
    }
  }

  onAuditPageChange(p: number) {
    this.auditPage.set(p);
    void this.loadAudit();
  }

  openEdit() {
    const v = this.vendor();
    if (!v) return;
    this.formServiceName.set(v.serviceName);
    this.formCompanyName.set(v.companyName);
    this.formContactPerson.set(v.contactPersonName);
    this.formMobile.set(v.mobile);
    this.formAltMobile.set(v.alternateMobile ?? '');
    this.formEmail.set(v.email);
    this.formAltEmail.set(v.alternateEmail ?? '');
    this.formWebsite.set(v.website ?? '');
    this.formAddress.set(v.address ?? '');
    this.formRemarks.set(v.remarks ?? '');
    this.formIsActive.set(v.isActive);
    this.formPlanIds.set(v.assignedPlans.map((p) => p.planId));
    this.editOpen.set(true);
  }

  closeEdit() {
    this.editOpen.set(false);
  }

  togglePlan(planId: number) {
    const current = this.formPlanIds();
    if (current.includes(planId)) {
      this.formPlanIds.set(current.filter((id) => id !== planId));
    } else {
      this.formPlanIds.set([...current, planId]);
    }
  }

  isPlanSelected(planId: number): boolean {
    return this.formPlanIds().includes(planId);
  }

  async submitEdit() {
    const v = this.vendor();
    if (!v) return;

    const body: VendorFormBody = {
      serviceName: this.formServiceName().trim(),
      companyName: this.formCompanyName().trim(),
      contactPersonName: this.formContactPerson().trim(),
      mobile: this.formMobile().trim(),
      alternateMobile: this.formAltMobile().trim() || null,
      email: this.formEmail().trim(),
      alternateEmail: this.formAltEmail().trim() || null,
      website: this.formWebsite().trim() || null,
      address: this.formAddress().trim() || null,
      remarks: this.formRemarks().trim() || null,
      isActive: this.formIsActive(),
      planIds: this.formPlanIds(),
    };

    if (!body.serviceName || !body.companyName || !body.contactPersonName || !body.mobile || !body.email) {
      this.toast.error('Please fill all required fields.');
      return;
    }

    try {
      this.editSubmitting.set(true);
      const res = await firstValueFrom(this.api.update(v.vendorId, body).pipe(timeout(20000)));
      if (res?.success === false) {
        this.toast.error(res.message || 'Unable to update vendor.');
        return;
      }
      this.toast.success(res.message || 'Vendor updated.');
      this.closeEdit();
      void this.load();
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to update vendor.'));
    } finally {
      this.editSubmitting.set(false);
    }
  }

  openDeactivate() {
    this.deactivateRemarks.set('');
    this.deactivateOpen.set(true);
  }

  closeDeactivate() {
    this.deactivateOpen.set(false);
  }

  async activate() {
    await this.runActive(true);
  }

  async confirmDeactivate() {
    if (!this.deactivateRemarks().trim()) {
      this.toast.error('Remarks are required to deactivate a vendor.');
      return;
    }
    await this.runActive(false, this.deactivateRemarks().trim());
    this.closeDeactivate();
  }

  private async runActive(isActive: boolean, remarks?: string) {
    const v = this.vendor();
    if (!v) return;

    try {
      this.actionSubmitting.set(true);
      const res = await firstValueFrom(
        this.api.setActive(v.vendorId, isActive, remarks).pipe(timeout(20000)),
      );
      if (res?.success === false) {
        this.toast.error(res.message || 'Unable to update status.');
        return;
      }
      this.toast.success(res.message || (isActive ? 'Vendor activated.' : 'Vendor deactivated.'));
      void this.load();
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to update status.'));
    } finally {
      this.actionSubmitting.set(false);
    }
  }

  openDelete() {
    this.deleteRemarks.set('');
    this.deleteOpen.set(true);
  }

  closeDelete() {
    this.deleteOpen.set(false);
  }

  async confirmDelete() {
    const v = this.vendor();
    if (!v) return;

    try {
      this.actionSubmitting.set(true);
      const res = await firstValueFrom(
        this.api.delete(v.vendorId, this.deleteRemarks().trim() || undefined).pipe(timeout(20000)),
      );
      if (res?.success === false) {
        this.toast.error(res.message || 'Unable to delete vendor.');
        return;
      }
      this.toast.success(res.message || 'Vendor deleted.');
      void this.router.navigate(this.listLink);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to delete vendor.'));
    } finally {
      this.actionSubmitting.set(false);
    }
  }

  websiteUrl(website: string | null | undefined): string | null {
    const raw = (website ?? '').trim();
    if (!raw) return null;
    return /^https?:\/\//i.test(raw) ? raw : `https://${raw}`;
  }

  locationLabel(v: VendorDetailModel): string {
    const parts = [v.city, v.state, v.country, v.pincode].filter((x) => !!x?.trim());
    return parts.length ? parts.join(', ') : '—';
  }
}
