import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { PasswordInputComponent } from '../../core/components/password-input/password-input';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule, PasswordInputComponent],
  templateUrl: './forgot-password.html',
  styleUrl: './forgot-password.css',
})
export class ForgotPassword {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  readonly submitting = signal(false);
  readonly step = signal<1 | 2>(1);

  readonly emailForm = this.fb.nonNullable.group({
    email: this.fb.nonNullable.control('', { validators: [Validators.required, Validators.email, Validators.maxLength(508)] }),
  });

  readonly resetForm = this.fb.nonNullable.group({
    email: this.fb.nonNullable.control('', { validators: [Validators.required, Validators.email, Validators.maxLength(508)] }),
    code: this.fb.nonNullable.control('', { validators: [Validators.required, Validators.minLength(4), Validators.maxLength(10)] }),
    newPassword: this.fb.nonNullable.control('', { validators: [Validators.required, Validators.minLength(8), Validators.maxLength(128)] }),
  });

  async sendOtp() {
    this.emailForm.markAllAsTouched();
    if (this.emailForm.invalid) {
      this.toast.warning('Please enter a valid email.');
      return;
    }

    const email = this.emailForm.controls.email.value.trim().toLowerCase();

    try {
      this.submitting.set(true);
      const res = await firstValueFrom(this.auth.forgotPassword(email));
      if (!res?.success) {
        this.toast.error(res?.message || 'Could not send OTP. Please try again.');
        return;
      }
      this.toast.success(res?.message || 'OTP sent. Please check your email.');
      this.resetForm.controls.email.setValue(email);
      this.step.set(2);
    } catch (e: any) {
      const msg =
        (e?.error?.message as string | undefined) ||
        (typeof e?.error === 'string' ? e.error : undefined) ||
        e?.message ||
        'Could not send OTP. Please try again.';
      this.toast.error(msg);
    } finally {
      this.submitting.set(false);
    }
  }

  async resetPassword() {
    this.resetForm.markAllAsTouched();
    if (this.resetForm.invalid) {
      this.toast.warning('Please enter OTP and a new password (min 8 chars).');
      return;
    }

    const email = this.resetForm.controls.email.value.trim().toLowerCase();
    const code = this.resetForm.controls.code.value.trim();
    const newPassword = this.resetForm.controls.newPassword.value;

    try {
      this.submitting.set(true);
      const res = await firstValueFrom(this.auth.resetPassword(email, code, newPassword));
      if (!res?.success) {
        this.toast.error(res?.message || 'Password reset failed.');
        return;
      }
      this.toast.success(res?.message || 'Password updated. Please login.');
      this.router.navigateByUrl('/login');
    } catch (e: any) {
      const msg =
        (e?.error?.message as string | undefined) ||
        (typeof e?.error === 'string' ? e.error : undefined) ||
        e?.message ||
        'Password reset failed.';
      this.toast.error(msg);
    } finally {
      this.submitting.set(false);
    }
  }

  back() {
    this.step.set(1);
  }
}
