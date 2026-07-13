import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
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
import { RecaptchaService } from '../../core/services/recaptcha.service';
import { AnalyticsService } from '../../core/services/analytics.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule, PasswordInputComponent],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly session = inject(AuthSessionService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly schemeDiscovery = inject(SchemeDiscoveryFlowService);
  private readonly planCheckout = inject(MembershipPlanCheckoutService);
  private readonly recaptcha = inject(RecaptchaService);
  private readonly analytics = inject(AnalyticsService);

  readonly submitting = signal(false);

  get registerCta(): LoginRegisterCta {
    return getLoginRegisterCta();
  }

  readonly form = this.fb.nonNullable.group({
    identifier: this.fb.nonNullable.control('', { validators: [Validators.required, Validators.maxLength(508)] }),
    password: this.fb.nonNullable.control('', { validators: [Validators.required, Validators.maxLength(128)] }),
  });

  ngOnInit(): void {
    this.session.refreshFromStorage();
    if (this.session.isLoggedIn() && this.planCheckout.hasPendingMembershipPlan()) {
      void this.router.navigateByUrl('/membership');
    }
  }

  async submit() {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.toast.warning('Please enter email/phone and password.');
      return;
    }

    try {
      this.submitting.set(true);
      const recaptchaToken = await this.recaptcha.execute('login');
      const res = await firstValueFrom(
        this.auth.login({
          identifier: this.form.controls.identifier.value.trim(),
          password: this.form.controls.password.value,
          recaptchaToken,
        })
      );
      if (!res?.success || !res.userId || !res.token) {
        this.toast.error(res?.message || 'Login failed.');
        return;
      }
      this.session.setSession(res.userId, res.token, res.role);
      this.analytics.trackLogin();
      this.toast.success('Login successful.');

      if (await this.schemeDiscovery.resumeAfterAuth()) {
        return;
      }

      // Resume plan checkout on the membership page (not over the login form).
      if (this.planCheckout.hasPendingMembershipPlan()) {
        await this.router.navigateByUrl('/membership');
        return;
      }

      await this.router.navigateByUrl('/profile');
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, API_USER_MESSAGES.login));
    } finally {
      this.submitting.set(false);
    }
  }
}
