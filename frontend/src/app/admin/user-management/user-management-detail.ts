import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom, timeout } from 'rxjs';
import { AdminPagination } from '../../core/components/admin-pagination/admin-pagination';
import { AdminSessionService } from '../../core/services/admin-session.service';
import { ToastService } from '../../core/services/toast.service';
import {
  UserManagementService,
  type ManagedUserDetail,
  type UserAuditItem,
  type UserManagementRole,
  type UserStatusHistoryItem,
} from '../../core/services/user-management.service';
import { ADMIN_DEFAULT_PAGE_SIZE } from '../../core/utils/admin-list-params';
import { getHttpErrorMessage } from '../../core/utils/http-error-message';
import { LocalDatePipe } from '../../core/pipes/local-date.pipe';
import { PasswordInputComponent } from '../../core/components/password-input/password-input';
import { formatMemberPartnerId, getIdLabel } from '../../core/utils/member-id-display.util';

@Component({
  selector: 'app-user-management-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, LocalDatePipe, AdminPagination, PasswordInputComponent],
  templateUrl: './user-management-detail.html',
  styleUrls: ['../admin-shared.css', './user-management-detail.css'],
})
export class UserManagementDetail {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(UserManagementService);
  private readonly session = inject(AdminSessionService);
  private readonly toast = inject(ToastService);

  readonly isSuperAdmin = computed(() => this.session.isSuperAdmin());
  readonly loading = signal(true);
  readonly user = signal<ManagedUserDetail | null>(null);
  readonly auditItems = signal<UserAuditItem[]>([]);
  readonly statusItems = signal<UserStatusHistoryItem[]>([]);
  readonly auditTotal = signal(0);
  readonly auditPage = signal(1);
  readonly auditPageSize = ADMIN_DEFAULT_PAGE_SIZE;
  readonly auditTab = signal<'audit' | 'status'>('audit');

  readonly editOpen = signal(false);
  readonly editSubmitting = signal(false);
  readonly formFullName = signal('');
  readonly formEmail = signal('');
  readonly formPhone = signal('');
  readonly formCompany = signal('');
  readonly formPassword = signal('');

  readonly deactivateOpen = signal(false);
  readonly deactivateRemarks = signal('');
  readonly deleteOpen = signal(false);
  readonly deleteRemarks = signal('');
  readonly actionSubmitting = signal(false);

  readonly roleSegment = computed(() => (this.route.snapshot.paramMap.get('role') ?? 'members') as UserManagementRole);
  readonly listLink = computed(() => ['/admin/user-management', this.roleSegment()]);
  readonly canDelete = computed(() => this.isSuperAdmin() && this.roleSegment() === 'admins' ? true : this.isSuperAdmin());

  constructor() {
    void this.load();
  }

  async load() {
    const userId = this.route.snapshot.paramMap.get('userId');
    if (!userId) {
      void this.router.navigate(this.listLink());
      return;
    }

    try {
      this.loading.set(true);
      const res = await firstValueFrom(this.api.getUser(userId).pipe(timeout(20000)));
      if (res?.success === false || !res.user) {
        this.toast.error(res?.message || 'User not found.');
        void this.router.navigate(this.listLink());
        return;
      }
      this.user.set(res.user);
      await this.loadAudit();
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to load user.'));
      void this.router.navigate(this.listLink());
    } finally {
      this.loading.set(false);
    }
  }

  async loadAudit() {
    const userId = this.user()?.userId;
    if (!userId) return;

    try {
      if (this.auditTab() === 'audit') {
        const res = await firstValueFrom(
          this.api.audit(userId, this.auditPage(), this.auditPageSize).pipe(timeout(20000)),
        );
        this.auditItems.set(res?.items ?? []);
        this.auditTotal.set(res?.total ?? 0);
      } else {
        const res = await firstValueFrom(
          this.api.statusHistory(userId, this.auditPage(), this.auditPageSize).pipe(timeout(20000)),
        );
        this.statusItems.set(res?.items ?? []);
        this.auditTotal.set(res?.total ?? 0);
      }
    } catch {
      this.auditItems.set([]);
      this.statusItems.set([]);
      this.auditTotal.set(0);
    }
  }

  switchAuditTab(tab: 'audit' | 'status') {
    this.auditTab.set(tab);
    this.auditPage.set(1);
    void this.loadAudit();
  }

  onAuditPageChange(p: number) {
    this.auditPage.set(p);
    void this.loadAudit();
  }

  openEdit() {
    const u = this.user();
    if (!u) return;
    this.formFullName.set(u.fullName);
    this.formEmail.set(u.email);
    this.formPhone.set(u.phone);
    this.formCompany.set(u.companyName ?? '');
    this.formPassword.set('');
    this.editOpen.set(true);
  }

  closeEdit() {
    this.editOpen.set(false);
  }

  async submitEdit() {
    const u = this.user();
    if (!u) return;

    try {
      this.editSubmitting.set(true);
      const res = await firstValueFrom(
        this.api
          .updateUser(u.userId, {
            fullName: this.formFullName().trim(),
            email: this.formEmail().trim(),
            phone: this.formPhone().trim(),
            companyName: this.formCompany().trim() || null,
            password: this.formPassword().trim() || null,
          })
          .pipe(timeout(20000)),
      );
      if (res?.success === false) {
        this.toast.error(res.message || 'Unable to update user.');
        return;
      }
      this.toast.success(res.message || 'User updated.');
      this.closeEdit();
      void this.load();
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to update user.'));
    } finally {
      this.editSubmitting.set(false);
    }
  }

  async activate() {
    const u = this.user();
    if (!u) return;
    await this.setActive(true);
  }

  openDeactivate() {
    this.deactivateRemarks.set('');
    this.deactivateOpen.set(true);
  }

  closeDeactivate() {
    this.deactivateOpen.set(false);
  }

  async confirmDeactivate() {
    if (!this.deactivateRemarks().trim()) {
      this.toast.error('Remarks are required to deactivate a user.');
      return;
    }
    await this.setActive(false, this.deactivateRemarks().trim());
    this.closeDeactivate();
  }

  private async setActive(isActive: boolean, remarks?: string) {
    const u = this.user();
    if (!u) return;

    try {
      this.actionSubmitting.set(true);
      const res = await firstValueFrom(
        this.api.setActive(u.userId, isActive, remarks).pipe(timeout(20000)),
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

  openDelete() {
    this.deleteRemarks.set('');
    this.deleteOpen.set(true);
  }

  closeDelete() {
    this.deleteOpen.set(false);
  }

  async confirmDelete() {
    const u = this.user();
    if (!u) return;

    try {
      this.actionSubmitting.set(true);
      const res = await firstValueFrom(
        this.api.deleteUser(u.userId, this.deleteRemarks().trim() || undefined).pipe(timeout(20000)),
      );
      if (res?.success === false) {
        this.toast.error(res.message || 'Unable to delete user.');
        return;
      }
      this.toast.success(res.message || 'User deleted.');
      void this.router.navigate(this.listLink());
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to delete user.'));
    } finally {
      this.actionSubmitting.set(false);
      this.closeDelete();
    }
  }

  roleLabel(role: string): string {
    const r = (role ?? '').toLowerCase();
    if (r === 'superadmin') return 'Super Admin';
    if (r === 'admin') return 'Admin';
    if (r === 'partner') return 'Partner';
    if (r === 'member') return 'Member';
    return role;
  }

  idLabel(role?: string | null): string {
    return getIdLabel(role ?? null);
  }

  idValue(memberId?: string | null, role?: string | null): string {
    return formatMemberPartnerId(memberId ?? null, role ?? null) ?? '—';
  }
}
