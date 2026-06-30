import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { AuthSessionService } from '../../core/services/auth-session.service';
import { SchemeDiscoveryFlowService } from '../../core/services/scheme-discovery-flow.service';
import { ToastService } from '../../core/services/toast.service';
import { API_USER_MESSAGES } from '../../core/utils/api-user-messages';
import { getHttpErrorMessage } from '../../core/utils/http-error-message';
import { PasswordInputComponent } from '../../core/components/password-input/password-input';
import { MembershipPlanCheckoutService } from '../../core/services/membership-plan-checkout.service';
import { getLoginRegisterCta, type LoginRegisterCta } from '../../core/utils/registration-mode.util';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule, PasswordInputComponent],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly session = inject(AuthSessionService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly schemeDiscovery = inject(SchemeDiscoveryFlowService);
  private readonly planCheckout = inject(MembershipPlanCheckoutService);

  readonly submitting = signal(false);

  get registerCta(): LoginRegisterCta {
    return getLoginRegisterCta();
  }

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
      if (!res?.success || !res.userId || !res.token) {
        this.toast.error(res?.message || 'Login failed.');
        return;
      }
      this.session.setSession(res.userId, res.token, res.role);
      this.toast.success('Login successful.');

      if (await this.schemeDiscovery.resumeAfterAuth()) {
        return;
      }

      // If user came from a plan selection, start checkout directly (no /membership stop).
      if (this.planCheckout.processPendingPlanAfterAuth()) {
        return;
      }

      this.router.navigateByUrl('/profile');
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, API_USER_MESSAGES.login));
    } finally {
      this.submitting.set(false);
    }
  }
}
