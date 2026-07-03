import { CommonModule } from '@angular/common';
import { Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom, timeout } from 'rxjs';
import { AdminListToolbar } from '../../core/components/admin-list-toolbar/admin-list-toolbar';
import { AdminPagination } from '../../core/components/admin-pagination/admin-pagination';
import { AdminSessionService } from '../../core/services/admin-session.service';
import { ToastService } from '../../core/services/toast.service';
import {
  UserManagementService,
  type CreateUserBody,
  type ManagedUser,
  type UserManagementRole,
} from '../../core/services/user-management.service';
import { exportToExcel } from '../../core/utils/admin-excel-export';
import { ADMIN_DEFAULT_PAGE_SIZE, sortIndicator, toggleColumnSort } from '../../core/utils/admin-list-params';
import { getHttpErrorMessage } from '../../core/utils/http-error-message';
import { LocalDatePipe } from '../../core/pipes/local-date.pipe';
import { PasswordInputComponent } from '../../core/components/password-input/password-input';
import { formatMemberPartnerId } from '../../core/utils/member-id-display.util';

type RoleConfig = {
  role: UserManagementRole;
  title: string;
  eyebrow: string;
  canCreate: boolean;
  canDelete: boolean;
  showCompany: boolean;
  createLabel: string;
};

@Component({
  selector: 'app-user-management-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, LocalDatePipe, AdminListToolbar, AdminPagination, PasswordInputComponent],
  templateUrl: './user-management-list.html',
  styleUrls: ['../admin-shared.css', './user-management-list.css'],
})
export class UserManagementList {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly api = inject(UserManagementService);
  private readonly session = inject(AdminSessionService);
  private readonly toast = inject(ToastService);

  readonly isSuperAdmin = computed(() => this.session.isSuperAdmin());
  readonly loading = signal(false);
  readonly exporting = signal(false);
  readonly users = signal<ManagedUser[]>([]);
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

  readonly createOpen = signal(false);
  readonly createSubmitting = signal(false);
  readonly formFullName = signal('');
  readonly formEmail = signal('');
  readonly formPhone = signal('');
  readonly formPassword = signal('');
  readonly formCompany = signal('');

  readonly deactivateOpen = signal(false);
  readonly deactivateTarget = signal<ManagedUser | null>(null);
  readonly deactivateRemarks = signal('');
  readonly actionSubmitting = signal(false);

  readonly sortIndicator = sortIndicator;

  readonly roleSegment = signal<UserManagementRole>('members');

  readonly config = computed<RoleConfig>(() => {
    const segment = this.roleSegment();
    if (segment === 'admins') {
      return {
        role: 'admins',
        title: 'Admin Management',
        eyebrow: 'User Management',
        canCreate: this.isSuperAdmin(),
        canDelete: this.isSuperAdmin(),
        showCompany: false,
        createLabel: 'Create Admin',
      };
    }
    if (segment === 'partners') {
      return {
        role: 'partners',
        title: 'Partner Management',
        eyebrow: 'User Management',
        canCreate: true,
        canDelete: false,
        showCompany: true,
        createLabel: 'Create Partner',
      };
    }
    return {
      role: 'members',
      title: 'Member Management',
      eyebrow: 'User Management',
      canCreate: true,
      canDelete: false,
      showCompany: true,
      createLabel: 'Create Member',
    };
  });

  constructor() {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((pm) => {
      const raw = (pm.get('role') ?? 'members').toLowerCase();
      const role: UserManagementRole =
        raw === 'admins' || raw === 'partners' || raw === 'members' ? raw : 'members';
      if (raw !== role) {
        void this.router.navigate(['/admin/user-management', role]);
        return;
      }
      this.roleSegment.set(role);
      this.page.set(1);
      void this.load();
    });
  }

  idValue(memberId?: string | null, role?: string | null): string {
    return formatMemberPartnerId(memberId ?? null, role ?? null) ?? '—';
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

  async load() {
    if (this.config().role === 'admins' && !this.isSuperAdmin()) {
      this.toast.error('Only Super Admin can access Admin Management.');
      void this.router.navigateByUrl('/admin-dashboard');
      return;
    }

    try {
      this.loading.set(true);
      const res = await firstValueFrom(
        this.api.list(this.config().role, this.search(), this.listQueryOpts()).pipe(timeout(20000)),
      );
      if (res?.success === false) {
        this.toast.error(res.message || 'Unable to load users.');
        this.users.set([]);
        this.total.set(0);
        return;
      }
      this.users.set(res?.users ?? []);
      this.total.set(res?.total ?? 0);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to load users.'));
      this.users.set([]);
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

  openCreate() {
    this.formFullName.set('');
    this.formEmail.set('');
    this.formPhone.set('');
    this.formPassword.set('');
    this.formCompany.set('');
    this.createOpen.set(true);
  }

  closeCreate() {
    this.createOpen.set(false);
  }

  async submitCreate() {
    const body: CreateUserBody = {
      fullName: this.formFullName().trim(),
      email: this.formEmail().trim(),
      phone: this.formPhone().trim(),
      password: this.formPassword(),
      companyName: this.formCompany().trim() || null,
    };
    if (!body.fullName || !body.email || !body.phone || !body.password) {
      this.toast.error('Please fill all required fields.');
      return;
    }

    try {
      this.createSubmitting.set(true);
      const role = this.config().role;
      const call =
        role === 'admins'
          ? this.api.createAdmin(body)
          : role === 'partners'
            ? this.api.createPartner(body)
            : this.api.createMember(body);
      const res = await firstValueFrom(call.pipe(timeout(20000)));
      if (res?.success === false) {
        this.toast.error(res.message || 'Unable to create user.');
        return;
      }
      this.toast.success(res.message || 'User created.');
      this.closeCreate();
      void this.load();
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to create user.'));
    } finally {
      this.createSubmitting.set(false);
    }
  }

  openDeactivate(user: ManagedUser) {
    this.deactivateTarget.set(user);
    this.deactivateRemarks.set('');
    this.deactivateOpen.set(true);
  }

  closeDeactivate() {
    this.deactivateOpen.set(false);
    this.deactivateTarget.set(null);
  }

  async confirmActivate(user: ManagedUser) {
    await this.runActive(user, true);
  }

  async confirmDeactivate() {
    const user = this.deactivateTarget();
    if (!user) return;
    if (!this.deactivateRemarks().trim()) {
      this.toast.error('Remarks are required to deactivate a user.');
      return;
    }
    await this.runActive(user, false, this.deactivateRemarks().trim());
    this.closeDeactivate();
  }

  private async runActive(user: ManagedUser, isActive: boolean, remarks?: string) {
    try {
      this.actionSubmitting.set(true);
      const res = await firstValueFrom(
        this.api.setActive(user.userId, isActive, remarks).pipe(timeout(20000)),
      );
      if (res?.success === false) {
        this.toast.error(res.message || 'Unable to update status.');
        return;
      }
      this.toast.success(res.message || (isActive ? 'User activated.' : 'User deactivated.'));
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
        this.api.list(this.config().role, this.search(), this.listQueryOpts(true)).pipe(timeout(30000)),
      );
      const rows = (res?.users ?? []) as ManagedUser[];
      exportToExcel(
        `${this.config().role}-users`,
        [
          { header: 'Name', value: (r) => r.fullName },
          { header: 'Email', value: (r) => r.email },
          { header: 'Phone', value: (r) => r.phone },
          { header: 'Role', value: (r) => r.role },
          { header: 'Active', value: (r) => (r.isActive ? 'Active' : 'Inactive') },
          { header: 'Created', value: (r) => r.createdAt, type: 'datetime' },
        ],
        rows,
      );
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Export failed.'));
    } finally {
      this.exporting.set(false);
    }
  }

  detailLink(user: ManagedUser): string[] {
    return ['/admin/user-management', this.config().role, user.userId];
  }
}
