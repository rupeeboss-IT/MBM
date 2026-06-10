import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, computed, DestroyRef, ElementRef, HostListener, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, debounceTime, distinctUntilChanged, firstValueFrom, map, of, switchMap, tap } from 'rxjs';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { AuthSessionService } from '../../core/services/auth-session.service';
import { ToastService } from '../../core/services/toast.service';
import { API_USER_MESSAGES } from '../../core/utils/api-user-messages';
import { getHttpErrorMessage, sanitizeUserMessage } from '../../core/utils/http-error-message';
import { bannedWordsNameValidator, nameNoRepeatsValidator, strictFullNameValidator } from '../../core/validators/name.validators';
import { passwordComplexityValidator } from '../../core/validators/password.validators';
import { strictEmailValidator } from '../../core/validators/email.validators';
import { indianMobileValidator } from '../../core/validators/phone.validators';

type ClearbitCompany = { name: string; domain: string; logo: string | null };

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register {
  private readonly fb = inject(FormBuilder);
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  private readonly session = inject(AuthSessionService);
  private readonly host = inject(ElementRef<HTMLElement>);

  readonly roleOpen = signal(false);

  readonly roleOptions: ReadonlyArray<{ value: 'member' | 'partner'; label: string }> = [
    { value: 'member', label: 'MSME Member / Business Owner' },
    { value: 'partner', label: 'Growth Partner / Consultant' },
  ];

  /**
   * Normalized emails that completed OTP verify this session.
   * If the user edits away and types the same email again, UI shows verified without re-OTP.
   */
  private readonly verifiedEmailsThisSession = signal<ReadonlySet<string>>(new Set());
  /** Same for mobile (normalized digits, aligned with backend). */
  private readonly verifiedPhonesThisSession = signal<ReadonlySet<string>>(new Set());

  /** Normalized email an OTP was last sent for (pending verify). Cleared after verify or if user edits email). */
  private readonly emailOtpIssuedForNorm = signal<string | null>(null);
  private readonly smsOtpIssuedForNorm = signal<string | null>(null);

  readonly step = signal<1 | 2 | 3 | 4>(1);
  readonly createdUserId = signal<string | null>(null);

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
  readonly emailOtpSending = signal(false);
  readonly emailOtpVerifying = signal(false);
  readonly smsOtpSending = signal(false);
  readonly smsOtpVerifying = signal(false);

  // Company autocomplete
  readonly companyLoading = signal(false);
  readonly companySuggestions = signal<ClearbitCompany[]>([]);
  readonly companySuggestOpen = signal(false);
  readonly companyActiveIndex = signal<number>(-1);

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
      consent: this.fb.nonNullable.control(false, { validators: [Validators.requiredTrue] })
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

  constructor() {
    this.form.controls.email.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.syncEmailVerificationUi();
    });
    this.form.controls.phone.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.syncPhoneVerificationUi();
    });

    this.form.controls.company.valueChanges
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        map((v) => (v ?? '').trim()),
        debounceTime(250),
        distinctUntilChanged(),
        switchMap((q) => {
          if (q.length < 2) {
            this.companyLoading.set(false);
            this.companySuggestions.set([]);
            this.companyActiveIndex.set(-1);
            return of([] as ClearbitCompany[]);
          }
          this.companyLoading.set(true);
          return this.http
            .get<ClearbitCompany[]>('https://autocomplete.clearbit.com/v1/companies/suggest', {
              params: { query: q },
            })
            .pipe(
              catchError(() => of([] as ClearbitCompany[])),
              tap(() => this.companyLoading.set(false))
            );
        })
      )
      .subscribe((items) => {
        const list = items.slice(0, 8);
        this.companySuggestions.set(list);
        this.companyActiveIndex.set(list.length > 0 ? 0 : -1);
        this.companySuggestOpen.set(true);
      });

    queueMicrotask(() => {
      this.syncEmailVerificationUi();
      this.syncPhoneVerificationUi();
    });
  }

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

  roleDisplayLabel(): string {
    const selected = this.roleOptions.find(o => o.value === this.form.controls.role.value);
    return selected?.label ?? 'Select role';
  }

  toggleRoleOpen(event: Event): void {
    event.stopPropagation();
    this.roleOpen.update(open => !open);
  }

  selectRole(value: 'member' | 'partner'): void {
    this.form.controls.role.setValue(value);
    this.form.controls.role.markAsDirty();
    this.roleOpen.set(false);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.roleOpen()) return;
    const root = this.host.nativeElement.querySelector('.reg-role-ddl');
    if (root && !root.contains(event.target as Node)) {
      this.roleOpen.set(false);
    }
  }

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    this.roleOpen.set(false);
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

  onCompanyFocus() {
    this.companySuggestOpen.set(true);
  }

  onCompanyBlur() {
    // allow click on suggestion before closing
    window.setTimeout(() => this.companySuggestOpen.set(false), 120);
  }

  selectCompany(s: ClearbitCompany) {
    this.form.controls.company.setValue(s.name);
    this.form.controls.company.markAsDirty();
    this.companySuggestOpen.set(false);
    this.companySuggestions.set([]);
    this.companyActiveIndex.set(-1);
  }

  onCompanyKeydown(ev: KeyboardEvent) {
    const open = this.companySuggestOpen();
    const list = this.companySuggestions();
    const hasItems = list.length > 0;

    if (ev.key === 'Escape') {
      if (open) {
        ev.preventDefault();
        this.companySuggestOpen.set(false);
      }
      return;
    }

    if (ev.key === 'ArrowDown') {
      ev.preventDefault();
      if (!open) this.companySuggestOpen.set(true);
      if (!hasItems) return;
      const next = Math.min(list.length - 1, (this.companyActiveIndex() < 0 ? 0 : this.companyActiveIndex() + 1));
      this.companyActiveIndex.set(next);
      return;
    }

    if (ev.key === 'ArrowUp') {
      ev.preventDefault();
      if (!open) this.companySuggestOpen.set(true);
      if (!hasItems) return;
      const next = Math.max(0, (this.companyActiveIndex() < 0 ? 0 : this.companyActiveIndex() - 1));
      this.companyActiveIndex.set(next);
      return;
    }

    if (ev.key === 'Enter') {
      if (!open || !hasItems) return;
      const idx = this.companyActiveIndex();
      if (idx < 0 || idx >= list.length) return;
      ev.preventDefault();
      this.selectCompany(list[idx]);
    }
  }

  async sendEmailOtp() {
    this.form.controls.email.markAsTouched();
    if (this.form.controls.email.invalid) return;
    if (this.emailOtpCooldownSec() > 0) return;

    const emailNorm = this.normalizeEmail(this.form.controls.email.value);
    if (emailNorm && this.verifiedEmailsThisSession().has(emailNorm)) {
      this.syncEmailVerificationUi();
      return;
    }

    try {
      this.emailOtpSending.set(true);
      await firstValueFrom(this.auth.sendEmailOtp(this.form.controls.email.value));
      this.emailOtpIssuedForNorm.set(this.normalizeEmail(this.form.controls.email.value));
      this.emailOtpSent.set(true);
      this.showEmailOtpInputs.set(true);
      this.startCooldown('email', 60);
      this.toast.success('OTP sent to your email. Please check your inbox or spam folder.');
    } catch (e) {
      this.toast.error(this.mapOtpSendError(e, 'Failed to send email OTP.'));
    } finally {
      this.emailOtpSending.set(false);
    }
  }

  async verifyEmailOtp() {
    this.form.controls.email.markAsTouched();
    this.form.controls.emailOtp.markAsTouched();
    if (this.form.controls.email.invalid) return;
    const code = (this.form.controls.emailOtp.value ?? '').trim();
    if (!/^\d{6}$/.test(code)) {
      this.form.controls.emailOtp.setErrors({ otpFormat: true });
      this.toast.error('Please enter a valid 6-digit OTP.');
      return;
    }

    try {
      this.emailOtpVerifying.set(true);
      await firstValueFrom(this.auth.verifyEmailOtp(this.form.controls.email.value, code));
      const norm = this.normalizeEmail(this.form.controls.email.value);
      this.verifiedEmailsThisSession.update((s) => new Set(s).add(norm));
      this.syncEmailVerificationUi();
      this.toast.success('Email verified successfully.');
    } catch (e) {
      this.syncEmailVerificationUi();
      this.toast.error(this.mapVerifyOtpFailure(e));
    } finally {
      this.emailOtpVerifying.set(false);
    }
  }

  async sendSmsOtp() {
    this.form.controls.phone.markAsTouched();
    if (this.form.controls.phone.invalid) return;
    if (this.smsOtpCooldownSec() > 0) return;

    const phoneNorm = this.normalizePhoneDigits(this.form.controls.phone.value);
    if (phoneNorm && this.verifiedPhonesThisSession().has(phoneNorm)) {
      this.syncPhoneVerificationUi();
      return;
    }

    try {
      this.smsOtpSending.set(true);
      await firstValueFrom(this.auth.sendSmsOtp(this.form.controls.phone.value));
      this.smsOtpIssuedForNorm.set(this.normalizePhoneDigits(this.form.controls.phone.value));
      this.smsOtpSent.set(true);
      this.showSmsOtpInputs.set(true);
      this.startCooldown('sms', 60);
      this.toast.success('OTP sent to your mobile via SMS.');
    } catch (e) {
      this.toast.error(this.mapOtpSendError(e, 'Failed to send SMS OTP.'));
    } finally {
      this.smsOtpSending.set(false);
    }
  }

  async verifySmsOtp() {
    this.form.controls.phone.markAsTouched();
    this.form.controls.smsOtp.markAsTouched();
    if (this.form.controls.phone.invalid) return;
    const code = (this.form.controls.smsOtp.value ?? '').trim();
    if (!/^\d{6}$/.test(code)) {
      this.form.controls.smsOtp.setErrors({ otpFormat: true });
      this.toast.error('Please enter a valid 6-digit OTP.');
      return;
    }

    try {
      this.smsOtpVerifying.set(true);
      await firstValueFrom(this.auth.verifySmsOtp(this.form.controls.phone.value, code));
      const norm = this.normalizePhoneDigits(this.form.controls.phone.value);
      this.verifiedPhonesThisSession.update((s) => new Set(s).add(norm));
      this.syncPhoneVerificationUi();
      this.toast.success('Mobile number verified successfully.');
    } catch (e) {
      this.syncPhoneVerificationUi();
      this.toast.error(this.mapVerifyOtpFailure(e));
    } finally {
      this.smsOtpVerifying.set(false);
    }
  }

  async continueToPlan() {
    this.form.markAllAsTouched();
    const emailNorm = this.normalizeEmail(this.form.controls.email.value);
    const phoneNorm = this.normalizePhoneDigits(this.form.controls.phone.value);
    if (!emailNorm || !this.verifiedEmailsThisSession().has(emailNorm)) {
      this.toast.warning('Please verify your current email address with OTP before continuing.');
      return;
    }
    if (!phoneNorm || !this.verifiedPhonesThisSession().has(phoneNorm)) {
      this.toast.warning('Please verify your current mobile number with OTP before continuing.');
      return;
    }
    if (!this.form.controls.consent.value) {
      this.form.controls.consent.markAsTouched();
      this.toast.warning('Please accept the consent to continue.');
      return;
    }
    if (this.form.invalid) {
      this.toast.warning('Please fix the highlighted errors before continuing.');
      return;
    }

    try {
      this.submitting.set(true);
      const res = await firstValueFrom(
        this.auth.register({
          role: this.form.controls.role.value,
          fullName: this.form.controls.fullName.value,
          email: emailNorm,
          phone: phoneNorm,
          companyName: this.form.controls.company.value?.trim() ? this.form.controls.company.value.trim() : null,
          password: this.form.controls.password.value,
          consentAccepted: this.form.controls.consent.value === true,
        })
      );
      if (!res?.success) {
        this.toast.error(res?.message || 'Registration failed. Please try again.');
        return;
      }
      if (typeof res.userId === 'string' && res.userId && res.token) {
        this.createdUserId.set(res.userId);
        this.session.setSession(res.userId, res.token, res.role);
      }
      // If the user came from /membership wanting to buy a plan, send them straight there so checkout opens.
      const pendingPlan =
        typeof window !== 'undefined' ? window.localStorage.getItem('mbm_pending_plan') : null;
      if (pendingPlan) {
        this.toast.success('Account created. Opening payment for your selected plan…');
        this.router.navigateByUrl('/membership');
        return;
      }
      this.toast.success('Account created. Continue to plan selection.');
      this.step.set(2);
    } catch (e) {
      this.toast.error(this.httpErr(e, API_USER_MESSAGES.register));
    } finally {
      this.submitting.set(false);
    }
  }

  backToAccount() {
    this.step.set(1);
  }

  skipPlan() {
    this.step.set(3);
  }

  skipPayment() {
    this.router.navigateByUrl('/profile');
  }

  private syncEmailVerificationUi() {
    const norm = this.normalizeEmail(this.form.controls.email.value);
    const inSet = norm.length > 0 && this.verifiedEmailsThisSession().has(norm);
    const wasVerified = this.emailVerified();

    if (inSet) {
      this.emailVerified.set(true);
      this.emailOtpIssuedForNorm.set(null);
      this.clearEmailOtpPendingState();
      return;
    }

    this.emailVerified.set(false);

    if (wasVerified) {
      this.toast.warning('Email was changed. Please verify it again with a new OTP.', 5500);
      this.emailOtpIssuedForNorm.set(null);
      this.clearEmailOtpPendingState();
      return;
    }

    const issued = this.emailOtpIssuedForNorm();
    if (issued != null && norm !== issued) {
      this.emailOtpIssuedForNorm.set(null);
      this.clearEmailOtpPendingState();
    }
  }

  private syncPhoneVerificationUi() {
    const norm = this.normalizePhoneDigits(this.form.controls.phone.value);
    const inSet = norm.length > 0 && this.verifiedPhonesThisSession().has(norm);
    const wasVerified = this.smsVerified();

    if (inSet) {
      this.smsVerified.set(true);
      this.smsOtpIssuedForNorm.set(null);
      this.clearSmsOtpPendingState();
      return;
    }

    this.smsVerified.set(false);

    if (wasVerified) {
      this.toast.warning('Mobile number was changed. Please verify it again with a new OTP.', 5500);
      this.smsOtpIssuedForNorm.set(null);
      this.clearSmsOtpPendingState();
      return;
    }

    const issued = this.smsOtpIssuedForNorm();
    if (issued != null && norm !== issued) {
      this.smsOtpIssuedForNorm.set(null);
      this.clearSmsOtpPendingState();
    }
  }

  private clearEmailOtpPendingState() {
    this.emailOtpSent.set(false);
    this.showEmailOtpInputs.set(false);
    this.form.controls.emailOtp.setValue('', { emitEvent: false });
    this.emailOtpCooldownSec.set(0);
  }

  private clearSmsOtpPendingState() {
    this.smsOtpSent.set(false);
    this.showSmsOtpInputs.set(false);
    this.form.controls.smsOtp.setValue('', { emitEvent: false });
    this.smsOtpCooldownSec.set(0);
  }

  private normalizeEmail(value: string): string {
    return (value ?? '').trim().toLowerCase();
  }

  /** Aligns with backend IndianPhone.Digits. */
  private normalizePhoneDigits(value: string): string {
    let d = (value ?? '').replace(/\D/g, '');
    if (d.length === 12 && d.startsWith('91')) d = d.slice(2);
    if (d.length === 11 && d.startsWith('0')) d = d.slice(1);
    return d;
  }

  private mapOtpSendError(e: unknown, fallback: string): string {
    if (e instanceof HttpErrorResponse && e.status === 429) {
      return (
        this.httpErr(e, '') ||
        'Too many OTP requests for this contact. Please wait 10 minutes and try again.'
      );
    }
    return this.httpErr(e, fallback);
  }

  private httpErr(e: unknown, fallback: string) {
    return getHttpErrorMessage(e, fallback);
  }

  /** Maps API errors to clear copy; wrong OTP gets an explicit message. */
  private mapVerifyOtpFailure(e: unknown): string {
    const raw = sanitizeUserMessage(this.httpErr(e, '')) ?? '';
    if (/invalid otp/i.test(raw)) {
      return 'The OTP you entered is wrong. Please try again.';
    }
    if (/expired/i.test(raw)) {
      return 'This OTP has expired. Please request a new OTP.';
    }
    return raw || 'Could not verify the OTP. Please try again.';
  }
}
