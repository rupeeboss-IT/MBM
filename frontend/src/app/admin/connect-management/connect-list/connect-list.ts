import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom, timeout } from 'rxjs';
import { AdminPagination } from '../../../core/components/admin-pagination/admin-pagination';
import { ToastService } from '../../../core/services/toast.service';
import {
  ConnectManagementService,
  type ConnectAdminListItem,
  type ConnectListingFormBody,
  type ConnectUserSearchItem,
} from '../../../core/services/connect-management.service';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';

@Component({
  selector: 'app-connect-list',
  standalone: true,
  imports: [CommonModule, FormsModule, AdminPagination],
  templateUrl: './connect-list.html',
  styleUrls: ['../../admin-shared.css', './connect-list.css'],
})
export class ConnectList {
  private readonly api = inject(ConnectManagementService);
  private readonly toast = inject(ToastService);

  readonly loading = signal(false);
  readonly listings = signal<ConnectAdminListItem[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly pageSize = 10;
  readonly search = signal('');
  readonly roleFilter = signal('');
  readonly statusFilter = signal('');

  readonly formOpen = signal(false);
  readonly formMode = signal<'create' | 'edit'>('create');
  readonly formSubmitting = signal(false);
  readonly editListingId = signal<string | null>(null);

  readonly userSearch = signal('');
  readonly userResults = signal<ConnectUserSearchItem[]>([]);
  readonly selectedUser = signal<ConnectUserSearchItem | null>(null);

  readonly formIsListed = signal(true);
  readonly formIsVerified = signal(false);
  readonly formIsActive = signal(true);
  readonly formBusinessType = signal('');
  readonly formSector = signal('');
  readonly formState = signal('');
  readonly formCity = signal('');
  readonly formTurnover = signal('');
  readonly formUdyam = signal('');
  readonly formEmployees = signal('');
  readonly formDescription = signal('');
  readonly formWebsite = signal('');
  readonly formEstablished = signal('');
  readonly formRemarks = signal('');
  readonly customerLocked = signal<string[]>([]);
  readonly readOnlyMember = signal<{
    fullName: string;
    companyName?: string | null;
    phone: string;
    email: string;
  } | null>(null);

  readonly isFieldLocked = computed(() => {
    const locked = new Set(this.customerLocked().map((f) => f.toLowerCase()));
    return (field: string) => locked.has(field.toLowerCase());
  });

  constructor() {
    void this.load();
  }

  async load() {
    try {
      this.loading.set(true);
      const res = await firstValueFrom(
        this.api
          .list({
            page: this.page(),
            pageSize: this.pageSize,
            search: this.search() || undefined,
            role: this.roleFilter() || undefined,
            status: this.statusFilter() || undefined,
          })
          .pipe(timeout(20000)),
      );
      this.listings.set(res.listings ?? []);
      this.total.set(res.total ?? 0);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to load Connect listings.'));
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

  openCreate() {
    this.formMode.set('create');
    this.editListingId.set(null);
    this.selectedUser.set(null);
    this.userSearch.set('');
    this.userResults.set([]);
    this.customerLocked.set([]);
    this.readOnlyMember.set(null);
    this.resetFormFields();
    this.formOpen.set(true);
  }

  async openEdit(item: ConnectAdminListItem) {
    this.formMode.set('edit');
    this.editListingId.set(item.listingId);
    this.selectedUser.set(null);
    this.customerLocked.set([]);
    this.readOnlyMember.set(null);
    this.resetFormFields();
    this.formSubmitting.set(true);
    this.formOpen.set(true);
    try {
      const res = await firstValueFrom(this.api.get(item.listingId).pipe(timeout(20000)));
      if (!res?.listing) {
        this.toast.error(res?.message || 'Listing not found.');
        this.closeForm();
        return;
      }
      const l = res.listing;
      this.formMode.set('edit');
      this.editListingId.set(l.listingId);
      this.selectedUser.set({
        userId: l.userId,
        role: l.role,
        fullName: l.fullName,
        companyName: l.companyName,
        phone: l.phone,
        email: l.email,
        hasListing: true,
      });
      this.readOnlyMember.set({
        fullName: l.fullName,
        companyName: l.companyName,
        phone: l.phone,
        email: l.email,
      });
      this.customerLocked.set(l.customerLockedFields ?? []);
      this.formIsListed.set(l.isListed);
      this.formIsVerified.set(l.isVerified);
      this.formIsActive.set(l.isActive);
      this.formBusinessType.set(l.businessType ?? '');
      this.formSector.set(l.sector ?? '');
      this.formState.set(l.state ?? '');
      this.formCity.set(l.city ?? '');
      this.formTurnover.set(l.turnover ?? '');
      this.formUdyam.set(l.udyam ?? '');
      this.formEmployees.set(l.employees ?? '');
      this.formDescription.set(l.description ?? '');
      this.formWebsite.set(l.website ?? '');
      this.formEstablished.set(l.established ?? '');
      this.formRemarks.set(l.remarks ?? '');
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to load listing.'));
      this.closeForm();
    } finally {
      this.formSubmitting.set(false);
    }
  }

  closeForm() {
    this.formOpen.set(false);
  }

  resetFormFields() {
    this.formIsListed.set(true);
    this.formIsVerified.set(false);
    this.formIsActive.set(true);
    this.formBusinessType.set('');
    this.formSector.set('');
    this.formState.set('');
    this.formCity.set('');
    this.formTurnover.set('');
    this.formUdyam.set('');
    this.formEmployees.set('');
    this.formDescription.set('');
    this.formWebsite.set('');
    this.formEstablished.set('');
    this.formRemarks.set('');
  }

  async searchUsers() {
    const q = this.userSearch().trim();
    if (q.length < 2) {
      this.userResults.set([]);
      return;
    }
    try {
      const res = await firstValueFrom(
        this.api.searchUsers(q, this.roleFilter() || undefined).pipe(timeout(15000)),
      );
      this.userResults.set((res.users ?? []).filter((u) => !u.hasListing));
    } catch {
      this.userResults.set([]);
    }
  }

  pickUser(u: ConnectUserSearchItem) {
    this.selectedUser.set(u);
    this.readOnlyMember.set({
      fullName: u.fullName,
      companyName: u.companyName,
      phone: u.phone,
      email: u.email,
    });
    this.userResults.set([]);
    this.userSearch.set(u.fullName);
  }

  formBody(): ConnectListingFormBody {
    return {
      isListed: this.formIsListed(),
      isVerified: this.formIsVerified(),
      isActive: this.formIsActive(),
      businessType: this.formBusinessType() || null,
      sector: this.formSector() || null,
      state: this.formState() || null,
      city: this.formCity() || null,
      turnover: this.formTurnover() || null,
      udyam: this.formUdyam() || null,
      employees: this.formEmployees() || null,
      description: this.formDescription() || null,
      website: this.formWebsite() || null,
      established: this.formEstablished() || null,
      remarks: this.formRemarks() || null,
    };
  }

  async submitForm() {
    try {
      this.formSubmitting.set(true);
      if (this.formMode() === 'create') {
        const user = this.selectedUser();
        if (!user) {
          this.toast.error('Select a member or partner.');
          return;
        }
        const res = await firstValueFrom(
          this.api.create({ ...this.formBody(), userId: user.userId }).pipe(timeout(20000)),
        );
        if (res?.success === false) {
          this.toast.error(res.message || 'Unable to create listing.');
          return;
        }
        this.toast.success(res.message || 'Listing created.');
      } else {
        const id = this.editListingId();
        if (!id) return;
        const res = await firstValueFrom(this.api.update(id, this.formBody()).pipe(timeout(20000)));
        if (res?.success === false) {
          this.toast.error(res.message || 'Unable to update listing.');
          return;
        }
        this.toast.success(res.message || 'Listing updated.');
      }
      this.closeForm();
      await this.load();
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Save failed.'));
    } finally {
      this.formSubmitting.set(false);
    }
  }

  display(v: string | null | undefined): string {
    const s = (v ?? '').trim();
    return s.length ? s : '-';
  }
}
