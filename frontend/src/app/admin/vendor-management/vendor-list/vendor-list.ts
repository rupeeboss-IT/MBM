import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom, timeout } from 'rxjs';
import { AdminListToolbar } from '../../../core/components/admin-list-toolbar/admin-list-toolbar';
import { AdminPagination } from '../../../core/components/admin-pagination/admin-pagination';
import { AdminSessionService } from '../../../core/services/admin-session.service';
import { ToastService } from '../../../core/services/toast.service';
import {
  VendorManagementService,
  type VendorFormBody,
  type VendorListItem,
  type VendorPlan,
} from '../../../core/services/vendor-management.service';
import { exportToExcel } from '../../../core/utils/admin-excel-export';
import { ADMIN_DEFAULT_PAGE_SIZE, sortIndicator, toggleColumnSort } from '../../../core/utils/admin-list-params';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';

@Component({
  selector: 'app-vendor-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, AdminListToolbar, AdminPagination],
  templateUrl: './vendor-list.html',
  styleUrls: ['../../admin-shared.css', './vendor-list.css'],
})
export class VendorList {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(VendorManagementService);
  private readonly session = inject(AdminSessionService);
  private readonly toast = inject(ToastService);

  readonly isSuperAdmin = computed(() => this.session.isSuperAdmin());
  readonly loading = signal(false);
  readonly exporting = signal(false);
  readonly vendors = signal<VendorListItem[]>([]);
  readonly plans = signal<VendorPlan[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly pageSize = ADMIN_DEFAULT_PAGE_SIZE;
  readonly search = signal('');
  readonly statusFilter = signal('');
  readonly dateFrom = signal('');
  readonly dateTo = signal('');
  readonly sortPreset = signal('latest');
  readonly sortBy = signal('created');
  readonly sortDir = signal<'asc' | 'desc'>('desc');

  readonly formOpen = signal(false);
  readonly formMode = signal<'create' | 'edit'>('create');
  readonly formSubmitting = signal(false);
  readonly editTargetId = signal<string | null>(null);

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

  readonly deleteOpen = signal(false);
  readonly deleteTarget = signal<VendorListItem | null>(null);
  readonly deleteRemarks = signal('');
  readonly deactivateOpen = signal(false);
  readonly deactivateTarget = signal<VendorListItem | null>(null);
  readonly deactivateRemarks = signal('');
  readonly actionSubmitting = signal(false);

  readonly plansOpen = signal(false);
  readonly plansTarget = signal<VendorListItem | null>(null);
  readonly plansSelection = signal<number[]>([]);

  readonly sortIndicator = sortIndicator;

  constructor() {
    const status = this.route.snapshot.queryParamMap.get('status');
    if (status === 'active' || status === 'inactive') {
      this.statusFilter.set(status);
    }
    void this.loadPlans();
    void this.load();
  }

  listQueryOpts(exportAll = false) {
    const preset = this.sortPreset();
    const usePreset = ['latest', 'oldest', 'name_asc', 'name_desc'].includes(preset);
    return {
      page: exportAll ? 1 : this.page(),
      pageSize: exportAll ? 10000 : this.pageSize,
      dateFrom: this.dateFrom() || undefined,
      dateTo: this.dateTo() || undefined,
      sortBy: usePreset ? preset : this.sortBy(),
      sortDir: usePreset ? undefined : this.sortDir(),
      export: exportAll || undefined,
      status: this.statusFilter() || undefined,
    };
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
    try {
      this.loading.set(true);
      const res = await firstValueFrom(
        this.api.list(this.search(), this.listQueryOpts()).pipe(timeout(20000)),
      );
      if (res?.success === false) {
        this.toast.error(res.message || 'Unable to load vendors.');
        this.vendors.set([]);
        this.total.set(0);
        return;
      }
      this.vendors.set(res?.vendors ?? []);
      this.total.set(res?.total ?? 0);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to load vendors.'));
      this.vendors.set([]);
      this.total.set(0);
    } finally {
      this.loading.set(false);
    }
  }

  applyFilters() {
    this.page.set(1);
    void this.load();
  }

  onPageChange(p: number) {
    this.page.set(p);
    void this.load();
  }

  onSortColumn(field: string) {
    const next = toggleColumnSort(this.sortBy(), this.sortDir(), field);
    this.sortBy.set(next.sortBy);
    this.sortDir.set(next.sortDir);
    this.sortPreset.set('');
    void this.load();
  }

  resetForm() {
    this.formServiceName.set('');
    this.formCompanyName.set('');
    this.formContactPerson.set('');
    this.formMobile.set('');
    this.formAltMobile.set('');
    this.formEmail.set('');
    this.formAltEmail.set('');
    this.formWebsite.set('');
    this.formAddress.set('');
    this.formRemarks.set('');
    this.formIsActive.set(true);
    this.formPlanIds.set([]);
    this.editTargetId.set(null);
  }

  openCreate() {
    this.resetForm();
    this.formMode.set('create');
    this.formOpen.set(true);
  }

  async openEdit(vendor: VendorListItem) {
    try {
      this.loading.set(true);
      const res = await firstValueFrom(this.api.getVendor(vendor.vendorId).pipe(timeout(20000)));
      if (res?.success === false || !res.vendor) {
        this.toast.error(res?.message || 'Unable to load vendor.');
        return;
      }
      const v = res.vendor;
      this.formMode.set('edit');
      this.editTargetId.set(v.vendorId);
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
      this.formOpen.set(true);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to load vendor.'));
    } finally {
      this.loading.set(false);
    }
  }

  closeForm() {
    this.formOpen.set(false);
    this.resetForm();
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

  togglePlansSelection(planId: number) {
    const current = this.plansSelection();
    if (current.includes(planId)) {
      this.plansSelection.set(current.filter((id) => id !== planId));
    } else {
      this.plansSelection.set([...current, planId]);
    }
  }

  isPlansSelection(planId: number): boolean {
    return this.plansSelection().includes(planId);
  }

  buildFormBody(): VendorFormBody {
    return {
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
  }

  async submitForm() {
    const body = this.buildFormBody();
    if (!body.serviceName || !body.companyName || !body.contactPersonName || !body.mobile || !body.email) {
      this.toast.error('Please fill all required fields.');
      return;
    }

    try {
      this.formSubmitting.set(true);
      const isEdit = this.formMode() === 'edit';
      const vendorId = this.editTargetId();
      const res = await firstValueFrom(
        (isEdit && vendorId ? this.api.update(vendorId, body) : this.api.create(body)).pipe(timeout(20000)),
      );
      if (res?.success === false) {
        this.toast.error(res.message || 'Unable to save vendor.');
        return;
      }
      this.toast.success(res.message || (isEdit ? 'Vendor updated.' : 'Vendor created.'));
      this.closeForm();
      void this.load();
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to save vendor.'));
    } finally {
      this.formSubmitting.set(false);
    }
  }

  openAssignPlans(vendor: VendorListItem) {
    void this.openPlansModal(vendor);
  }

  async openPlansModal(vendor: VendorListItem) {
    try {
      const res = await firstValueFrom(this.api.getVendor(vendor.vendorId).pipe(timeout(20000)));
      if (res?.success === false || !res.vendor) {
        this.toast.error(res?.message || 'Unable to load vendor plans.');
        return;
      }
      this.plansTarget.set(vendor);
      this.plansSelection.set(res.vendor.assignedPlans.map((p) => p.planId));
      this.plansOpen.set(true);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to load vendor plans.'));
    }
  }

  closePlansModal() {
    this.plansOpen.set(false);
    this.plansTarget.set(null);
    this.plansSelection.set([]);
  }

  async submitPlans() {
    const target = this.plansTarget();
    if (!target) return;

    try {
      this.actionSubmitting.set(true);
      const res = await firstValueFrom(
        this.api.assignPlans(target.vendorId, this.plansSelection()).pipe(timeout(20000)),
      );
      if (res?.success === false) {
        this.toast.error(res.message || 'Unable to assign plans.');
        return;
      }
      this.toast.success(res.message || 'Plans updated.');
      this.closePlansModal();
      void this.load();
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to assign plans.'));
    } finally {
      this.actionSubmitting.set(false);
    }
  }

  openDelete(vendor: VendorListItem) {
    this.deleteTarget.set(vendor);
    this.deleteRemarks.set('');
    this.deleteOpen.set(true);
  }

  closeDelete() {
    this.deleteOpen.set(false);
    this.deleteTarget.set(null);
  }

  async confirmDelete() {
    const target = this.deleteTarget();
    if (!target) return;

    try {
      this.actionSubmitting.set(true);
      const res = await firstValueFrom(
        this.api.delete(target.vendorId, this.deleteRemarks().trim() || undefined).pipe(timeout(20000)),
      );
      if (res?.success === false) {
        this.toast.error(res.message || 'Unable to delete vendor.');
        return;
      }
      this.toast.success(res.message || 'Vendor deleted.');
      this.closeDelete();
      void this.load();
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to delete vendor.'));
    } finally {
      this.actionSubmitting.set(false);
    }
  }

  openDeactivate(vendor: VendorListItem) {
    this.deactivateTarget.set(vendor);
    this.deactivateRemarks.set('');
    this.deactivateOpen.set(true);
  }

  closeDeactivate() {
    this.deactivateOpen.set(false);
    this.deactivateTarget.set(null);
  }

  async confirmActivate(vendor: VendorListItem) {
    await this.runActive(vendor, true);
  }

  async confirmDeactivate() {
    const vendor = this.deactivateTarget();
    if (!vendor) return;
    if (!this.deactivateRemarks().trim()) {
      this.toast.error('Remarks are required to deactivate a vendor.');
      return;
    }
    await this.runActive(vendor, false, this.deactivateRemarks().trim());
    this.closeDeactivate();
  }

  private async runActive(vendor: VendorListItem, isActive: boolean, remarks?: string) {
    try {
      this.actionSubmitting.set(true);
      const res = await firstValueFrom(
        this.api.setActive(vendor.vendorId, isActive, remarks).pipe(timeout(20000)),
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

  async exportExcel() {
    try {
      this.exporting.set(true);
      const res = await firstValueFrom(
        this.api.list(this.search(), this.listQueryOpts(true)).pipe(timeout(30000)),
      );
      const rows = (res?.vendors ?? []) as VendorListItem[];
      exportToExcel(
        'vendors',
        [
          { header: 'Service Name', value: (r) => r.serviceName },
          { header: 'Company Name', value: (r) => r.companyName },
          { header: 'Contact Person', value: (r) => r.contactPersonName },
          { header: 'Mobile', value: (r) => r.mobile },
          { header: 'Alt Mobile', value: (r) => r.alternateMobile ?? '' },
          { header: 'Email', value: (r) => r.email },
          { header: 'Alt Email', value: (r) => r.alternateEmail ?? '' },
          { header: 'Assigned Plans', value: (r) => r.assignedPlanNames.join(', ') },
          { header: 'Status', value: (r) => (r.isActive ? 'Active' : 'Inactive') },
        ],
        rows,
      );
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Export failed.'));
    } finally {
      this.exporting.set(false);
    }
  }

  detailLink(vendor: VendorListItem): string[] {
    return ['/admin/vendor-management/vendors', vendor.vendorId];
  }

  planNamesLabel(names: string[]): string {
    return names.length ? names.join(', ') : '—';
  }
}
