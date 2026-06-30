import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, computed, DestroyRef, ElementRef, HostListener, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, debounceTime, distinctUntilChanged, firstValueFrom, map, of, switchMap, tap } from 'rxjs';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { AuthSessionService } from '../../core/services/auth-session.service';
import { SchemeDiscoveryFlowService } from '../../core/services/scheme-discovery-flow.service';
import {
  MembershipPlanCheckoutService,
  type MembershipPlanCode,
} from '../../core/services/membership-plan-checkout.service';
import { ToastService } from '../../core/services/toast.service';
import { API_USER_MESSAGES } from '../../core/utils/api-user-messages';
import { hasSchemeDiscoveryIntent } from '../../core/utils/scheme-discovery-intent';
import { getHttpErrorMessage, sanitizeUserMessage } from '../../core/utils/http-error-message';
import { bannedWordsNameValidator, nameNoRepeatsValidator, strictFullNameValidator } from '../../core/validators/name.validators';
import { passwordComplexityValidator } from '../../core/validators/password.validators';
import { strictEmailValidator } from '../../core/validators/email.validators';
import { indianMobileValidator } from '../../core/validators/phone.validators';
import { PasswordInputComponent } from '../../core/components/password-input/password-input';
import { captureRegistrationLeadSourceFromUrl, clearRegistrationLeadSource, getRegistrationLeadSource } from '../../core/utils/registration-lead-source.util';
import { captureRegistrationAdvisorFromUrl, getRegistrationAdvisorCode, setRegistrationAdvisorCode } from '../../core/utils/registration-advisor.util';
import { ReferralService, referrerDisplayName } from '../../core/services/referral.service';
import {
  consumeStashedMembershipSource,
  isSchemeDiscoveryMembershipSource,
} from '../../core/utils/membership-source.util';
import {
  captureRegistrationModeFromUrl,
  clearRegistrationMode,
  isFreeRegistrationMode,
} from '../../core/utils/registration-mode.util';

type ClearbitCompany = { name: string; domain: string; logo: string | null };

type RegisterRes = {
  success: boolean;
  message?: string;
  userId?: string;
  role?: string;
  token?: string;
};

const ADVISOR_DEBOUNCE_MS = 500;
const ADVISOR_PANEL_ANIM_MS = 420;
const ADVISOR_INVALID_MSG =
  'Advisor code is incorrect. Please check and enter the correct code.';
type AdvisorValidationState = 'idle' | 'validating' | 'valid' | 'invalid';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule, PasswordInputComponent],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly session = inject(AuthSessionService);
  private readonly schemeDiscovery = inject(SchemeDiscoveryFlowService);
  private readonly planCheckout = inject(MembershipPlanCheckoutService);
  private readonly referrals = inject(ReferralService);
  private readonly host = inject(ElementRef<HTMLElement>);

  private advisorDebounceTimer: ReturnType<typeof setTimeout> | null = null;
  private advisorPanelTimer: ReturnType<typeof setTimeout> | null = null;
  private advisorValidationSeq = 0;

  readonly advisorValidationState = signal<AdvisorValidationState>('idle');
  readonly advisorReferredByName = signal<string | null>(null);
  readonly advisorInvalidMessage = ADVISOR_INVALID_MSG;
  readonly advisorInputOpen = signal(false);
  readonly isFreeRegistration = signal(false);

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
      advisorCode: this.fb.nonNullable.control('', {
        validators: [Validators.maxLength(50)]
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

    this.form.controls.advisorCode.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.onAdvisorInputChange();
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

  ngOnInit(): void {
    if (typeof window !== 'undefined') {
      captureRegistrationLeadSourceFromUrl(window.location.search);
      captureRegistrationAdvisorFromUrl(window.location.search);
    }

    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.syncRegistrationModeFromRoute();
    });

    const stashedAdvisor = getRegistrationAdvisorCode();
    if (stashedAdvisor) {
      this.advisorInputOpen.set(true);
      this.form.controls.advisorCode.setValue(stashedAdvisor);
      void this.runAdvisorValidation();
    }
    this.session.refreshFromStorage();
    if (this.session.isLoggedIn()) {
      void this.redirectIfAlreadyAuthenticated();
    }
  }

  private syncRegistrationModeFromRoute(): void {
    const query = this.router.url.includes('?')
      ? this.router.url.slice(this.router.url.indexOf('?'))
      : '';
    captureRegistrationModeFromUrl(query);
    const isFree = isFreeRegistrationMode();
    this.isFreeRegistration.set(isFree);
    if (isFree && this.step() > 1) {
      this.step.set(1);
    }
  }

  private onAdvisorInputChange() {
    const code = this.form.controls.advisorCode.value.trim();
    this.advisorReferredByName.set(null);

    if (!code) {
      if (this.advisorDebounceTimer) clearTimeout(this.advisorDebounceTimer);
      this.advisorValidationSeq++;
      this.advisorValidationState.set('idle');
      setRegistrationAdvisorCode(null);
      return;
    }

    this.advisorValidationState.set('validating');
    if (this.advisorDebounceTimer) clearTimeout(this.advisorDebounceTimer);
    this.advisorDebounceTimer = setTimeout(() => void this.runAdvisorValidation(), ADVISOR_DEBOUNCE_MS);
  }

  private async runAdvisorValidation(): Promise<boolean> {
    const code = this.form.controls.advisorCode.value.trim();
    if (!code) {
      this.advisorValidationState.set('idle');
      setRegistrationAdvisorCode(null);
      return false;
    }

    const seq = ++this.advisorValidationSeq;
    this.advisorValidationState.set('validating');
    this.advisorReferredByName.set(null);

    try {
      const res = await firstValueFrom(this.referrals.validate({ referralCode: code }));
      if (seq !== this.advisorValidationSeq) return false;

      const name = referrerDisplayName(res);
      if (res?.success && name) {
        this.advisorReferredByName.set(name);
        this.advisorValidationState.set('valid');
        setRegistrationAdvisorCode(code);
        return true;
      }

      this.advisorValidationState.set('invalid');
      return false;
    } catch {
      if (seq !== this.advisorValidationSeq) return false;
      this.advisorValidationState.set('invalid');
      return false;
    }
  }

  private async resolveAdvisorCodeForSubmit(): Promise<string | null> {
    const code = this.form.controls.advisorCode.value.trim();
    if (!code) return null;

    if (this.advisorValidationState() !== 'valid') {
      const ok = await this.runAdvisorValidation();
      if (!ok) return null;
    }

    return code;
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

  openAdvisorInput(): void {
    if (this.advisorPanelTimer) {
      clearTimeout(this.advisorPanelTimer);
      this.advisorPanelTimer = null;
    }
    this.advisorInputOpen.set(true);
    window.setTimeout(() => {
      const input = this.host.nativeElement.querySelector('#regAdvisor') as HTMLInputElement | null;
      input?.focus();
    }, 180);
  }

  closeAdvisorInput(): void {
    this.advisorInputOpen.set(false);
    if (this.advisorPanelTimer) clearTimeout(this.advisorPanelTimer);
    this.advisorPanelTimer = setTimeout(() => {
      this.resetAdvisorFields();
      this.advisorPanelTimer = null;
    }, ADVISOR_PANEL_ANIM_MS);
  }

  private resetAdvisorFields(): void {
    if (this.advisorDebounceTimer) clearTimeout(this.advisorDebounceTimer);
    this.advisorValidationSeq++;
    this.form.controls.advisorCode.setValue('');
    this.advisorReferredByName.set(null);
    this.advisorValidationState.set('idle');
    setRegistrationAdvisorCode(null);
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
    if (this.submitting()) return;

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

    const wantsAdvisor = this.advisorInputOpen();
    const advisorCode = wantsAdvisor ? await this.resolveAdvisorCodeForSubmit() : null;
    if (wantsAdvisor && this.form.controls.advisorCode.value.trim() && !advisorCode) {
      this.form.controls.advisorCode.markAsTouched();
      this.toast.warning('Please enter a valid advisor code or leave the field empty.');
      return;
    }

    try {
      this.submitting.set(true);
      const registrationSource = getRegistrationLeadSource();
      const res = await firstValueFrom(
        this.auth.register({
          role: this.form.controls.role.value,
          fullName: this.form.controls.fullName.value,
          email: emailNorm,
          phone: phoneNorm,
          companyName: this.form.controls.company.value?.trim() ? this.form.controls.company.value.trim() : null,
          password: this.form.controls.password.value,
          consentAccepted: this.form.controls.consent.value === true,
          ...(registrationSource ? { registrationSource } : {}),
          ...(advisorCode ? { advisorCode } : {}),
        })
      );
      if (!res?.success) {
        console.error('[Register] API returned success=false', res);
        this.toast.error(res?.message || 'Registration failed. Please try again.');
        return;
      }
      await this.handleRegistrationSuccess(res);
    } catch (e) {
      if (e instanceof HttpErrorResponse && e.status === 409 && this.session.isLoggedIn()) {
        console.warn('[Register] Duplicate registration while authenticated; continuing post-registration flow');
        await this.completePostRegistrationNavigation('You are already registered. Continuing…');
        return;
      }
      console.error('[Register] Registration request failed', e);
      this.toast.error(this.httpErr(e, API_USER_MESSAGES.register));
    } finally {
      this.submitting.set(false);
    }
  }

  backToAccount() {
    this.step.set(1);
  }

  skipPlan(): void {
    try {
      if (typeof window !== 'undefined') {
        window.localStorage.removeItem('mbm_pending_plan');
      }
    } catch {
      // ignore
    }
    this.toast.success(
      'Welcome to MSME Bharat Manch! You can choose a membership plan anytime from your dashboard.',
    );
    void this.router.navigateByUrl('/profile');
  }

  selectMembershipPlan(code: MembershipPlanCode): void {
    void this.planCheckout.choosePlan(code);
  }

  selectSchemeDiscoveryReport(): void {
    this.schemeDiscovery.beginOneTimeReportCheckout();
  }

  skipPayment() {
    this.router.navigateByUrl('/profile');
  }

  private async handleRegistrationSuccess(res: RegisterRes): Promise<void> {
    clearRegistrationLeadSource();
    // Keep advisor in session for membership checkout pre-fill / payment reconciliation.
    const userId = this.coerceUserId(res.userId);
    const token = (res.token ?? '').trim();
    if (!userId || !token) {
      console.error('[Register] Success response missing userId or token', res);
      this.toast.error('Registration completed but sign-in failed. Please log in.');
      await this.router.navigateByUrl('/login');
      return;
    }

    this.createdUserId.set(userId);
    this.session.setSession(userId, token, res.role);
    await this.completePostRegistrationNavigation(res.message || 'Account created.');
  }

  private async completePostRegistrationNavigation(successMessage: string): Promise<void> {
    try {
      if (await this.schemeDiscovery.resumeAfterAuth()) {
        clearRegistrationMode();
        await this.router.navigateByUrl('/profile');
        return;
      }
    } catch (e) {
      console.error('[Register] Scheme discovery resume failed after registration', e);
    }

    const schemeDiscoverySource = consumeStashedMembershipSource();
    if (schemeDiscoverySource && isSchemeDiscoveryMembershipSource(schemeDiscoverySource)) {
      clearRegistrationMode();
      this.toast.success('Account created. Continue with your Scheme Discovery Report.');
      this.schemeDiscovery.beginOneTimeReportCheckout();
      return;
    }

    const pendingPlan =
      typeof window !== 'undefined' ? window.localStorage.getItem('mbm_pending_plan') : null;
    if (pendingPlan) {
      clearRegistrationMode();
      this.toast.success('Account created. Opening payment for your selected plan…');
      if (this.planCheckout.processPendingPlanAfterAuth()) {
        return;
      }
      await this.router.navigateByUrl('/membership');
      return;
    }

    if (this.isFreeRegistration()) {
      clearRegistrationMode();
      try {
        if (typeof window !== 'undefined') {
          window.localStorage.removeItem('mbm_pending_plan');
        }
      } catch {
        // ignore
      }
      this.toast.success(
        'Welcome to MSME Bharat Manch! You can choose a membership plan anytime from your dashboard.',
      );
      await this.router.navigateByUrl('/profile');
      return;
    }

    this.toast.success(
      successMessage.includes('already registered')
        ? successMessage
        : 'Account created. Continue to plan selection.'
    );
    this.step.set(2);
  }

  private async redirectIfAlreadyAuthenticated(): Promise<void> {
    if (hasSchemeDiscoveryIntent()) {
      await this.schemeDiscovery.tryResumeOnPageLoad();
      return;
    }

    const pendingPlan =
      typeof window !== 'undefined' ? window.localStorage.getItem('mbm_pending_plan') : null;
    if (pendingPlan) {
      await this.router.navigateByUrl('/membership');
      return;
    }

    if (this.isFreeRegistration()) {
      clearRegistrationMode();
      await this.router.navigateByUrl('/profile');
      return;
    }

    await this.router.navigateByUrl('/profile');
  }

  private coerceUserId(value: unknown): string {
    if (typeof value === 'string') return value.trim();
    if (value != null) return String(value).trim();
    return '';
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
