import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { bannedWordsNameValidator, nameNoRepeatsValidator, strictFullNameValidator } from '../../core/validators/name.validators';
import { passwordComplexityValidator } from '../../core/validators/password.validators';
import { strictEmailValidator } from '../../core/validators/email.validators';
import { indianMobileValidator } from '../../core/validators/phone.validators';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);

  readonly step = signal<1 | 2 | 4>(1);

  readonly showPass = signal(false);
  readonly showConfirmPass = signal(false);

  readonly emailOtpSent = signal(false);
  readonly emailVerified = signal(false);
  readonly showEmailOtpInputs = signal(false);
  readonly emailOtpCooldownSec = signal(0);

  readonly smsOtpSent = signal(false);
  readonly smsVerified = signal(false);
  readonly showSmsOtpInputs = signal(false);
  readonly smsOtpCooldownSec = signal(0);

  readonly submitting = signal(false);
  readonly formError = signal<string | null>(null);
  readonly formSuccess = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group(
    {
      role: this.fb.nonNullable.control<'member' | 'partner'>('member', { validators: [Validators.required] }),
      fullName: this.fb.nonNullable.control('', {
        validators: [
          Validators.required,
          Validators.minLength(3),
          Validators.maxLength(80),
          strictFullNameValidator(),
          nameNoRepeatsValidator(),
          bannedWordsNameValidator()
        ]
      }),
      email: this.fb.nonNullable.control('', {
        validators: [Validators.required, Validators.maxLength(254), strictEmailValidator()]
      }),
      emailOtp: this.fb.nonNullable.control('', {
        validators: [Validators.minLength(6), Validators.maxLength(6)]
      }),
      phone: this.fb.nonNullable.control('', {
        validators: [Validators.required, indianMobileValidator()]
      }),
      smsOtp: this.fb.nonNullable.control('', {
        validators: [Validators.minLength(6), Validators.maxLength(6)]
      }),
      password: this.fb.nonNullable.control('', {
        validators: [Validators.required, passwordComplexityValidator()]
      }),
      confirmPassword: this.fb.nonNullable.control('', {
        validators: [Validators.required]
      }),
      company: this.fb.nonNullable.control('', {
        validators: [Validators.maxLength(120)]
      }),
      consent: this.fb.nonNullable.control(true, { validators: [Validators.requiredTrue] })
    },
    {
      validators: [
        (group) => {
          const pass = group.get('password')?.value;
          const confirm = group.get('confirmPassword')?.value;
          if (!pass || !confirm) return null;
          return pass === confirm ? null : { passwordMismatch: true };
        }
      ]
    }
  );

  readonly canContinue = computed(() => {
    return (
      this.form.valid &&
      this.emailVerified() &&
      this.smsVerified() &&
      this.form.controls.consent.value === true
    );
  });

  private startCooldown(kind: 'email' | 'sms', seconds: number) {
    const target = kind === 'email' ? this.emailOtpCooldownSec : this.smsOtpCooldownSec;
    target.set(seconds);
    const tick = () => {
      const cur = target();
      if (cur <= 0) return;
      target.set(cur - 1);
      window.setTimeout(tick, 1000);
    };
    window.setTimeout(tick, 1000);
  }

  onConsentToggle(ev: Event) {
    const input = ev.target as HTMLInputElement;
    if (!input.checked) {
      this.form.controls.consent.setValue(true);
      this.form.controls.consent.markAsTouched();
      this.formError.set('⚠️ Consent is mandatory to continue. You cannot uncheck it.');
      window.setTimeout(() => this.formError.set(null), 3500);
    }
  }

  sanitizePhoneInput(ev: Event) {
    const input = ev.target as HTMLInputElement;
    const digits = (input.value ?? '').replace(/\D/g, '').slice(0, 10);
    if (digits !== input.value) {
      input.value = digits;
    }
    this.form.controls.phone.setValue(digits);
    this.form.controls.phone.markAsDirty();
  }

  preventClipboard(ev: ClipboardEvent) {
    ev.preventDefault();
  }

  async sendEmailOtp() {
    this.formError.set(null);
    this.formSuccess.set(null);
    this.emailVerified.set(false);

    this.form.controls.email.markAsTouched();
    if (this.form.controls.email.invalid) return;
    if (this.emailOtpCooldownSec() > 0) return;

    try {
      this.submitting.set(true);
      await this.auth.sendEmailOtp(this.form.controls.email.value).toPromise();
      this.emailOtpSent.set(true);
      this.showEmailOtpInputs.set(true);
      this.startCooldown('email', 60);
      this.formSuccess.set('✅ OTP sent to email. Please check your inbox/spam.');
    } catch (e) {
      this.formError.set(this.httpErr(e, 'Failed to send email OTP.'));
    } finally {
      this.submitting.set(false);
    }
  }

  async verifyEmailOtp() {
    this.formError.set(null);
    this.formSuccess.set(null);

    this.form.controls.email.markAsTouched();
    this.form.controls.emailOtp.markAsTouched();
    if (this.form.controls.email.invalid) return;
    const code = (this.form.controls.emailOtp.value ?? '').trim();
    if (!/^\d{6}$/.test(code)) {
      this.form.controls.emailOtp.setErrors({ otpFormat: true });
      return;
    }

    try {
      this.submitting.set(true);
      await this.auth.verifyEmailOtp(this.form.controls.email.value, code).toPromise();
      this.emailVerified.set(true);
      this.showEmailOtpInputs.set(false);
      this.formSuccess.set('✅ Email verified successfully.');
    } catch (e) {
      this.emailVerified.set(false);
      this.formError.set(this.httpErr(e, 'Invalid email OTP.'));
    } finally {
      this.submitting.set(false);
    }
  }

  async sendSmsOtp() {
    this.formError.set(null);
    this.formSuccess.set(null);
    this.smsVerified.set(false);

    this.form.controls.phone.markAsTouched();
    if (this.form.controls.phone.invalid) return;
    if (this.smsOtpCooldownSec() > 0) return;

    try {
      this.submitting.set(true);
      await this.auth.sendSmsOtp(this.form.controls.phone.value).toPromise();
      this.smsOtpSent.set(true);
      this.showSmsOtpInputs.set(true);
      this.startCooldown('sms', 60);
      this.formSuccess.set('✅ OTP sent via SMS.');
    } catch (e) {
      this.formError.set(this.httpErr(e, 'Failed to send SMS OTP.'));
    } finally {
      this.submitting.set(false);
    }
  }

  async verifySmsOtp() {
    this.formError.set(null);
    this.formSuccess.set(null);

    this.form.controls.phone.markAsTouched();
    this.form.controls.smsOtp.markAsTouched();
    if (this.form.controls.phone.invalid) return;
    const code = (this.form.controls.smsOtp.value ?? '').trim();
    if (!/^\d{6}$/.test(code)) {
      this.form.controls.smsOtp.setErrors({ otpFormat: true });
      return;
    }

    try {
      this.submitting.set(true);
      await this.auth.verifySmsOtp(this.form.controls.phone.value, code).toPromise();
      this.smsVerified.set(true);
      this.showSmsOtpInputs.set(false);
      this.formSuccess.set('✅ Mobile number verified successfully.');
    } catch (e) {
      this.smsVerified.set(false);
      this.formError.set(this.httpErr(e, 'Invalid SMS OTP.'));
    } finally {
      this.submitting.set(false);
    }
  }

  continueToPlan() {
    this.formError.set(null);
    this.formSuccess.set(null);

    this.form.markAllAsTouched();
    if (!this.emailVerified()) {
      this.formError.set('⚠️ Please verify your email via OTP before continuing.');
      return;
    }
    if (!this.smsVerified()) {
      this.formError.set('⚠️ Please verify your mobile number via OTP before continuing.');
      return;
    }
    if (this.form.invalid) {
      this.formError.set('⚠️ Please fix the highlighted errors before continuing.');
      return;
    }
    this.step.set(2);
  }

  backToAccount() {
    this.step.set(1);
  }

  private httpErr(e: unknown, fallback: string) {
    if (e instanceof HttpErrorResponse) {
      const msg =
        (typeof e.error === 'string' && e.error) ||
        (e.error?.message as string | undefined) ||
        e.message;
      return msg || fallback;
    }
    return fallback;
  }
}
