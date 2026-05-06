import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { AdminSessionService } from '../../core/services/admin-session.service';
import { ToastService } from '../../core/services/toast.service';

type DashboardCounts = {
  success: boolean;
  message?: string;
  users: number;
  plans: number;
  paymentOrders: number;
  payments: number;
  userPlans: number;
  blogs: number;
  events: number;
  schemes: number;
  schemeNews: number;
  successStories: number;
  offers: number;
  pricing: number;
};

type AdminUsersRes = {
  success: boolean;
  message?: string;
  users?: Array<{
    userId: string;
    role: string;
    fullName: string;
    email: string;
    phone: string;
    isActive: boolean;
    createdAt: string;
  }>;
};

type MembersRes = {
  success: boolean;
  message?: string;
  users?: Array<{
    userId: string;
    role: string;
    fullName: string;
    email: string;
    phone: string;
    isActive: boolean;
    createdAt: string;
  }>;
};

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.css'],
})
export class AdminDashboard {
  private readonly auth = inject(AuthService);
  private readonly session = inject(AdminSessionService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly counts = signal<DashboardCounts | null>(null);
  readonly adminUsers = signal<AdminUsersRes['users']>([]);
  readonly members = signal<MembersRes['users']>([]);
  readonly isSuperAdmin = this.session.isSuperAdmin;

  readonly createForm = this.fb.nonNullable.group({
    fullName: this.fb.nonNullable.control('', { validators: [Validators.required, Validators.maxLength(160)] }),
    email: this.fb.nonNullable.control('', { validators: [Validators.required, Validators.email, Validators.maxLength(508)] }),
    phone: this.fb.nonNullable.control('', { validators: [Validators.required, Validators.maxLength(32)] }),
    password: this.fb.nonNullable.control('', { validators: [Validators.required, Validators.minLength(8), Validators.maxLength(128)] }),
  });

  async ngOnInit() {
    await this.refresh();
  }

  async refresh() {
    try {
      this.loading.set(true);
      const counts = (await firstValueFrom(this.auth.adminDashboardCounts())) as any;
      this.counts.set(counts as DashboardCounts);

      if (this.session.isSuperAdmin()) {
        const users = (await firstValueFrom(this.auth.adminListUsers('admin'))) as any;
        this.adminUsers.set(((users as AdminUsersRes).users ?? []) as any);
      } else {
        this.adminUsers.set([]);
      }

      const members = (await firstValueFrom(this.auth.adminListMembers())) as any;
      this.members.set(((members as MembersRes).users ?? []) as any);
    } catch (e: any) {
      const msg =
        (e?.error?.message as string | undefined) ||
        (typeof e?.error === 'string' ? e.error : undefined) ||
        e?.message ||
        'Failed to load dashboard.';
      this.toast.error(msg);
    } finally {
      this.loading.set(false);
    }
  }

  logout() {
    this.session.logout();
    this.toast.info('Logged out.');
    this.router.navigateByUrl('/admin-login');
  }

  async createAdmin() {
    this.createForm.markAllAsTouched();
    if (this.createForm.invalid) {
      this.toast.warning('Please fill all fields correctly.');
      return;
    }
    try {
      this.loading.set(true);
      const res = await firstValueFrom(
        this.auth.adminCreateUser({
          fullName: this.createForm.controls.fullName.value.trim(),
          email: this.createForm.controls.email.value.trim(),
          phone: this.createForm.controls.phone.value.trim(),
          password: this.createForm.controls.password.value,
        })
      );
      if (!res?.success) {
        this.toast.error(res?.message || 'Could not create admin user.');
        return;
      }
      this.toast.success('Admin user created.');
      this.createForm.reset();
      await this.refresh();
    } catch (e: any) {
      const msg =
        (e?.error?.message as string | undefined) ||
        (typeof e?.error === 'string' ? e.error : undefined) ||
        e?.message ||
        'Could not create admin user.';
      this.toast.error(msg);
    } finally {
      this.loading.set(false);
    }
  }

  async deleteUser(userId: string) {
    if (!userId) return;
    try {
      this.loading.set(true);
      const res = await firstValueFrom(this.auth.adminDeleteUser(userId));
      if (!res?.success) {
        this.toast.error(res?.message || 'Could not delete user.');
        return;
      }
      this.toast.success('User deleted.');
      await this.refresh();
    } catch (e: any) {
      const msg =
        (e?.error?.message as string | undefined) ||
        (typeof e?.error === 'string' ? e.error : undefined) ||
        e?.message ||
        'Could not delete user.';
      this.toast.error(msg);
    } finally {
      this.loading.set(false);
    }
  }

  async toggleActive(u: NonNullable<AdminUsersRes['users']>[number]) {
    if (!u?.userId) return;
    try {
      this.loading.set(true);
      const res = await firstValueFrom(this.auth.adminSetUserActive(u.userId, !u.isActive));
      if (!res?.success) {
        this.toast.error(res?.message || 'Could not update status.');
        return;
      }
      this.toast.success(!u.isActive ? 'Admin activated.' : 'Admin deactivated.');
      await this.refresh();
    } catch (e: any) {
      const msg =
        (e?.error?.message as string | undefined) ||
        (typeof e?.error === 'string' ? e.error : undefined) ||
        e?.message ||
        'Could not update status.';
      this.toast.error(msg);
    } finally {
      this.loading.set(false);
    }
  }

  async toggleMemberActive(u: NonNullable<MembersRes['users']>[number]) {
    if (!u?.userId) return;
    try {
      this.loading.set(true);
      let reason: string | undefined;
      if (u.isActive) {
        reason = globalThis.prompt?.('Reason for deactivation?') ?? '';
        if (!reason?.trim()) {
          this.toast.warning('Deactivation cancelled (reason required).');
          return;
        }
      }

      const res = await firstValueFrom(this.auth.adminSetMemberActive(u.userId, !u.isActive, reason));
      if (!res?.success) {
        this.toast.error(res?.message || 'Could not update member status.');
        return;
      }
      this.toast.success(!u.isActive ? 'Member activated.' : 'Member deactivated.');
      await this.refresh();
    } catch (e: any) {
      const msg =
        (e?.error?.message as string | undefined) ||
        (typeof e?.error === 'string' ? e.error : undefined) ||
        e?.message ||
        'Could not update member status.';
      this.toast.error(msg);
    } finally {
      this.loading.set(false);
    }
  }
}

