import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { PasswordInputComponent } from '../../core/components/password-input/password-input';
import { passwordComplexityValidator } from '../../core/validators/password.validators';
import {
  detectIdentifierChannel,
  identifierValidationMessage,
  loginIdentifierValidator,
  normalizePhoneDigits,
  type IdentifierChannel,
} from '../../core/validators/identifier.validators';
import { API_USER_MESSAGES } from '../../core/utils/api-user-messages';
import { getHttpErrorMessage, sanitizeUserMessage } from '../../core/utils/http-error-message';
import {
  forgotPasswordAccountNotFoundHints,
  forgotPasswordAccountNotFoundMessage,
  isAccountNotFoundErrorMessage,
  isForgotPasswordAccountNotFound,
} from '../../core/utils/forgot-password-messages';

type ForgotPasswordStep = 'identifier' | 'not-found' | 'otp' | 'reset' | 'success';

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
  private readonly destroyRef = inject(DestroyRef);

  private cooldownTimer: ReturnType<typeof setInterval> | null = null;

  readonly submitting = signal(false);
  readonly otpSending = signal(false);
  readonly otpVerifying = signal(false);
  readonly step = signal<ForgotPasswordStep>('identifier');
  readonly channel = signal<IdentifierChannel | null>(null);
  readonly otpCooldownSec = signal(0);
  readonly otpSent = signal(false);
  readonly storedIdentifier = signal('');
  readonly notFoundMessage = signal('');
  readonly notFoundHints = signal<readonly string[]>([]);

  readonly identifierForm = this.fb.nonNullable.group({
    identifier: this.fb.nonNullable.control('', {
      validators: [Validators.required, Validators.maxLength(508), loginIdentifierValidator()],
    }),
  });

  readonly otpForm = this.fb.nonNullable.group({
    code: this.fb.nonNullable.control('', {
      validators: [Validators.required, Validators.minLength(6), Validators.maxLength(6)],
    }),
  });

  readonly resetForm = this.fb.nonNullable.group(
    {
      newPassword: this.fb.nonNullable.control('', {
        validators: [Validators.required, passwordComplexityValidator()],
      }),
      confirmPassword: this.fb.nonNullable.control('', {
        validators: [Validators.required],
      }),
    },
    {
      validators: [
        (group) => {
          const pass = group.get('newPassword')?.value;
          const confirm = group.get('confirmPassword')?.value;
          if (!pass || !confirm) return null;
          return pass === confirm ? null : { passwordMismatch: true };
        },
      ],
    }
  );

  constructor() {
    this.destroyRef.onDestroy(() => this.clearCooldownTimer());
  }

  identifierError(): string | null {
    const control = this.identifierForm.controls.identifier;
    if (!control.touched && !control.dirty) return null;
    return identifierValidationMessage(control.errors);
  }

  async continueFromIdentifier() {
    this.identifierForm.markAllAsTouched();
    if (this.identifierForm.invalid) {
      const msg = this.identifierError();
      this.toast.warning(msg || 'Please enter a valid email address or mobile number.');
      return;
    }

    const identifier = this.normalizeIdentifier(this.identifierForm.controls.identifier.value);
    const detected = detectIdentifierChannel(identifier);
    if (!detected) {
      this.toast.warning('Please enter a valid email address or mobile number.');
      return;
    }

    try {
      this.submitting.set(true);
      const res = await firstValueFrom(this.auth.forgotPassword(identifier));
      if (isForgotPasswordAccountNotFound(res)) {
        this.showAccountNotFound(identifier, detected, res.message);
        return;
      }
      if (!res?.success) {
        this.toast.error(res?.message || API_USER_MESSAGES.forgotPassword);
        return;
      }

      this.storedIdentifier.set(identifier);
      this.channel.set((res.channel as IdentifierChannel | undefined) ?? detected);
      this.otpSent.set(true);
      this.step.set('otp');
      this.startCooldown(60);
      this.toast.success(res.message || 'Verification code sent.');
    } catch (e: unknown) {
      const msg = getHttpErrorMessage(e, API_USER_MESSAGES.forgotPassword);
      if (isAccountNotFoundErrorMessage(msg)) {
        this.showAccountNotFound(identifier, detected, msg);
        return;
      }
      this.toast.error(this.mapOtpSendError(e, API_USER_MESSAGES.forgotPassword));
    } finally {
      this.submitting.set(false);
    }
  }

  async resendOtp() {
    if (this.otpCooldownSec() > 0 || this.otpSending()) return;

    const identifier = this.storedIdentifier();
    if (!identifier) {
      this.step.set('identifier');
      return;
    }

    try {
      this.otpSending.set(true);
      const res = await firstValueFrom(this.auth.forgotPassword(identifier));
      if (isForgotPasswordAccountNotFound(res)) {
        this.showAccountNotFound(identifier, this.channel(), res.message);
        return;
      }
      if (!res?.success) {
        this.toast.error(res?.message || API_USER_MESSAGES.forgotPassword);
        return;
      }
      this.otpSent.set(true);
      this.startCooldown(60);
      this.toast.success(res.message || 'Verification code sent.');
    } catch (e: unknown) {
      const msg = getHttpErrorMessage(e, API_USER_MESSAGES.forgotPassword);
      if (isAccountNotFoundErrorMessage(msg)) {
        this.showAccountNotFound(identifier, this.channel(), msg);
        return;
      }
      this.toast.error(this.mapOtpSendError(e, API_USER_MESSAGES.forgotPassword));
    } finally {
      this.otpSending.set(false);
    }
  }

  async verifyOtp() {
    this.otpForm.markAllAsTouched();
    const code = this.otpForm.controls.code.value.trim();
    if (!/^\d{6}$/.test(code)) {
      this.otpForm.controls.code.setErrors({ otpFormat: true });
      this.toast.error('Please enter a valid 6-digit OTP.');
      return;
    }

    const identifier = this.storedIdentifier();
    if (!identifier) {
      this.step.set('identifier');
      return;
    }

    try {
      this.otpVerifying.set(true);
      const res = await firstValueFrom(this.auth.verifyPasswordResetOtp(identifier, code));
      if (!res?.success) {
        if (isAccountNotFoundErrorMessage(res?.message)) {
          this.showAccountNotFound(identifier, this.channel(), res?.message);
          return;
        }
        this.toast.error(res?.message || API_USER_MESSAGES.verifyPasswordResetOtp);
        return;
      }
      this.toast.success('Verification successful.');
      this.step.set('reset');
    } catch (e: unknown) {
      const msg = getHttpErrorMessage(e, API_USER_MESSAGES.verifyPasswordResetOtp);
      if (isAccountNotFoundErrorMessage(msg)) {
        this.showAccountNotFound(identifier, this.channel(), msg);
        return;
      }
      this.toast.error(this.mapVerifyOtpFailure(e));
    } finally {
      this.otpVerifying.set(false);
    }
  }

  async updatePassword() {
    this.resetForm.markAllAsTouched();
    if (this.resetForm.invalid) {
      this.toast.warning('Please enter a strong password and confirm it.');
      return;
    }

    const identifier = this.storedIdentifier();
    if (!identifier) {
      this.step.set('identifier');
      return;
    }

    const newPassword = this.resetForm.controls.newPassword.value;
    const confirmPassword = this.resetForm.controls.confirmPassword.value;

    try {
      this.submitting.set(true);
      const res = await firstValueFrom(this.auth.resetPassword(identifier, newPassword, confirmPassword));
      if (!res?.success) {
        this.toast.error(res?.message || API_USER_MESSAGES.resetPassword);
        return;
      }
      this.clearCooldownTimer();
      this.step.set('success');
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, API_USER_MESSAGES.resetPassword));
    } finally {
      this.submitting.set(false);
    }
  }

  backToIdentifier() {
    this.clearCooldownTimer();
    this.otpCooldownSec.set(0);
    this.otpSent.set(false);
    this.otpForm.reset();
    this.step.set('identifier');
  }

  goToLogin() {
    void this.router.navigateByUrl('/login');
  }

  otpChannelLabel(): string {
    return this.channel() === 'sms' ? 'mobile number' : 'email address';
  }

  private showAccountNotFound(identifier: string, channel: IdentifierChannel | null, apiMessage?: string | null) {
    this.storedIdentifier.set(identifier);
    this.channel.set(channel);
    this.notFoundMessage.set(
      sanitizeUserMessage(apiMessage ?? '') ?? forgotPasswordAccountNotFoundMessage(channel)
    );
    this.notFoundHints.set(forgotPasswordAccountNotFoundHints(channel));
    this.step.set('not-found');
  }

  private normalizeIdentifier(value: string): string {
    const trimmed = (value ?? '').trim();
    if (trimmed.includes('@')) return trimmed.toLowerCase();
    return normalizePhoneDigits(trimmed);
  }

  private startCooldown(seconds: number) {
    this.clearCooldownTimer();
    this.otpCooldownSec.set(seconds);
    this.cooldownTimer = setInterval(() => {
      const next = this.otpCooldownSec() - 1;
      if (next <= 0) {
        this.clearCooldownTimer();
        this.otpCooldownSec.set(0);
        return;
      }
      this.otpCooldownSec.set(next);
    }, 1000);
  }

  private clearCooldownTimer() {
    if (this.cooldownTimer != null) {
      clearInterval(this.cooldownTimer);
      this.cooldownTimer = null;
    }
  }

  private mapOtpSendError(e: unknown, fallback: string): string {
    if (e instanceof HttpErrorResponse && e.status === 429) {
      return (
        getHttpErrorMessage(e, '') ||
        'Too many OTP requests for this contact. Please wait 10 minutes and try again.'
      );
    }
    return getHttpErrorMessage(e, fallback);
  }

  private mapVerifyOtpFailure(e: unknown): string {
    const raw = sanitizeUserMessage(getHttpErrorMessage(e, '')) ?? '';
    if (/invalid otp/i.test(raw)) return 'The OTP you entered is wrong. Please try again.';
    if (/expired/i.test(raw)) return 'This OTP has expired. Please request a new one.';
    if (/too many attempts/i.test(raw)) return 'Too many attempts. Please request a new OTP.';
    return getHttpErrorMessage(e, API_USER_MESSAGES.verifyPasswordResetOtp);
  }
}
