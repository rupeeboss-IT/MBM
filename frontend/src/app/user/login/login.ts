import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { AuthSessionService } from '../../core/services/auth-session.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly session = inject(AuthSessionService);
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
        this.auth.login({
          identifier: this.form.controls.identifier.value.trim(),
          password: this.form.controls.password.value,
        })
      );
      if (!res?.success || !res.userId) {
        this.toast.error(res?.message || 'Login failed.');
        return;
      }
      this.session.setUserId(res.userId);
      this.toast.success('Login successful.');
      this.router.navigateByUrl('/profile');
    } catch (e: any) {
      const msg =
        (e?.error?.message as string | undefined) ||
        (typeof e?.error === 'string' ? e.error : undefined) ||
        e?.message ||
        'Login failed.';
      this.toast.error(msg);
    } finally {
      this.submitting.set(false);
    }
  }
}
