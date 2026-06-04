import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { AdminSessionService } from '../../core/services/admin-session.service';
import { ToastService } from '../../core/services/toast.service';
import { getHttpErrorMessage } from '../../core/utils/http-error-message';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly session = inject(AdminSessionService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  readonly submitting = signal(false);

  readonly form = this.fb.nonNullable.group({
    identifier: this.fb.nonNullable.control('', { validators: [Validators.required, Validators.maxLength(508)] }),
    password: this.fb.nonNullable.control('', { validators: [Validators.required, Validators.maxLength(128)] }),
  });

  async submit() {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.toast.warning('Please enter email/phone and password.');
      return;
    }

    try {
      this.submitting.set(true);
      const res = await firstValueFrom(
        this.auth.adminLogin({
          identifier: this.form.controls.identifier.value.trim(),
          password: this.form.controls.password.value,
        })
      );
      if (!res?.success || !res.userId || !res.token) {
        this.toast.error(res?.message || 'Admin login failed.');
        return;
      }
      this.session.setSession(res.userId, res.token, res.role ?? 'admin');
      this.toast.success('Admin login successful.');
      this.router.navigateByUrl('/admin-dashboard');
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to sign in. Please verify your credentials.'));
    } finally {
      this.submitting.set(false);
    }
  }
}
