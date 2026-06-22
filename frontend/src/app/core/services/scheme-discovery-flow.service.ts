import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom, timeout } from 'rxjs';
import { AuthSessionService } from './auth-session.service';
import { AuthService } from './auth.service';
import { CustomerReportService } from './customer-report.service';
import { PaymentService } from './payment.service';
import { RazorpayLoaderService } from './razorpay-loader.service';
import { ReferralService } from './referral.service';
import { SchemeDiscoveryApi, type SchemeDiscoveryStatus, type SchemeDiscoverySubmitRes } from './scheme-discovery.service';
import { ToastService } from './toast.service';
import { API_USER_MESSAGES } from '../utils/api-user-messages';
import { getHttpErrorMessage } from '../utils/http-error-message';
import {
  consumeSchemeDiscoveryIntent,
  setSchemeDiscoveryIntent,
} from '../utils/scheme-discovery-intent';
import {
  consumeSchemeDiscoveryReturnUrl,
  setSchemeDiscoveryReturnUrl,
} from '../utils/scheme-discovery-journey.util';
import { formatUdyamFromRaw } from '../utils/udyam-input.util';
import {
  applyReferralConfirmPrefill,
  canProceedWithReferralState,
  REFERRAL_DEBOUNCE_MS,
  REFERRAL_INACTIVE_MSG,
  REFERRAL_INVALID_MSG,
  resolveReferralCheckoutAction,
  type ReferralValidationState,
  validateReferralCode,
} from '../utils/referral-capture.util';
import { getRegistrationAdvisorCode } from '../utils/registration-advisor.util';

export type SchemeDiscoveryModal =
  | 'none'
  | 'auth'
  | 'loading'
  | 'no_membership'
  | 'plan_choice'
  | 'report_exists'
  | 'udyam'
  | 'success'
  | 'pending'
  | 'referral'
  | 'generating_report'
  | 'report_generated'
  | 'report_duplicate'
  | 'report_failed';

const ONE_TIME_PLAN = 'scheme-report-onetime';

@Injectable({ providedIn: 'root' })
export class SchemeDiscoveryFlowService {
  private readonly session = inject(AuthSessionService);
  private readonly auth = inject(AuthService);
  private readonly api = inject(SchemeDiscoveryApi);
  private readonly payments = inject(PaymentService);
  private readonly razorpayLoader = inject(RazorpayLoaderService);
  private readonly referrals = inject(ReferralService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly reportsApi = inject(CustomerReportService);

  private referralDebounceTimer: ReturnType<typeof setTimeout> | null = null;
  private referralValidationSeq = 0;
  private readonly referralReturnModal = signal<SchemeDiscoveryModal>('plan_choice');

  readonly modal = signal<SchemeDiscoveryModal>('none');
  readonly status = signal<SchemeDiscoveryStatus | null>(null);
  readonly udyam = signal('');
  readonly udyamError = signal<string | null>(null);
  readonly submitting = signal(false);
  readonly paying = signal(false);
  readonly starting = signal(false);
  readonly redirecting = signal(false);
  readonly reportCtaBusy = computed(() => this.starting() || this.redirecting());
  readonly referralCodeInput = signal('');
  readonly referredByName = signal<string | null>(null);
  readonly referralValidationState = signal<ReferralValidationState>('idle');
  readonly referralInvalidMessage = REFERRAL_INVALID_MSG;
  readonly referralInactiveMessage = REFERRAL_INACTIVE_MSG;
  readonly referralModalMode = signal<'entry' | 'confirm'>('entry');
  readonly udyamCheckoutMode = signal(false);
  readonly draftRequestId = signal<string | null>(null);
  readonly generatedReportId = signal<string | null>(null);
  readonly lastFinalizeRequestId = signal<string | null>(null);
  readonly finalizeMessage = signal<string | null>(null);
  readonly finalizeExpiryDate = signal<string | null>(null);
  readonly reportDownloading = signal(false);
  readonly reportEmailSending = signal(false);
  readonly udyamBannerMessage = signal<string | null>(null);

  async startFlow(): Promise<void> {
    if (this.starting()) return;

    this.captureReturnUrl();

    this.session.refreshFromStorage();
    if (!this.session.isLoggedIn()) {
      this.openModal('auth');
      return;
    }

    this.starting.set(true);
    try {
      await this.continueForLoggedInUser();
    } finally {
      this.starting.set(false);
    }
  }

  async resumeAfterAuth(): Promise<boolean> {
    if (!consumeSchemeDiscoveryIntent()) return false;
    this.session.refreshFromStorage();
    if (!this.session.isLoggedIn()) return false;
    if (this.starting()) return true;

    this.starting.set(true);
    try {
      await this.continueForLoggedInUser();
      return true;
    } finally {
      this.starting.set(false);
    }
  }

  async tryResumeOnPageLoad(): Promise<void> {
    if (!this.session.isLoggedIn()) return;
    if (!consumeSchemeDiscoveryIntent()) return;
    if (this.starting()) return;

    this.starting.set(true);
    try {
      await this.continueForLoggedInUser();
    } finally {
      this.starting.set(false);
    }
  }

  closeAll(): void {
    this.clearReferralTimer();
    this.resetReferralFields();
    this.udyamCheckoutMode.set(false);
    this.draftRequestId.set(null);
    this.generatedReportId.set(null);
    this.lastFinalizeRequestId.set(null);
    this.finalizeMessage.set(null);
    this.finalizeExpiryDate.set(null);
    this.reportDownloading.set(false);
    this.reportEmailSending.set(false);
    this.udyamBannerMessage.set(null);
    this.modal.set('none');
    this.udyam.set('');
    this.udyamError.set(null);
    this.unlockBody();
  }

  goToLogin(): void {
    if (this.redirecting()) return;
    this.redirecting.set(true);
    this.captureReturnUrl();
    setSchemeDiscoveryIntent();
    this.closeAll();
    void this.router.navigateByUrl('/login').finally(() => this.redirecting.set(false));
  }

  goToRegister(): void {
    if (this.redirecting()) return;
    this.redirecting.set(true);
    this.captureReturnUrl();
    setSchemeDiscoveryIntent();
    this.closeAll();
    void this.router.navigateByUrl('/register').finally(() => this.redirecting.set(false));
  }

  goToMembership(source: 'scheme-discovery' | 'premium-report' | 'government-scheme' = 'scheme-discovery'): void {
    if (this.redirecting()) return;
    this.redirecting.set(true);
    this.captureReturnUrl();
    setSchemeDiscoveryIntent();
    this.closeAll();
    void this.router
      .navigate(['/membership'], { queryParams: { source } })
      .finally(() => this.redirecting.set(false));
  }

  /** After Pro/Premium membership payment — return to origin and open Udyam form. */
  async completeMembershipPurchaseAndResume(planCode: string): Promise<void> {
    consumeSchemeDiscoveryIntent();
    const returnUrl = consumeSchemeDiscoveryReturnUrl();
    const message =
      'Your membership has been activated successfully. You can now generate your Scheme Discovery Report.';

    this.toast.success(message);
    this.udyamBannerMessage.set(message);

    await this.router.navigateByUrl(returnUrl);

    if (this.starting()) return;
    this.starting.set(true);
    try {
      await this.continueForLoggedInUser();
    } finally {
      this.starting.set(false);
    }
  }

  private captureReturnUrl(): void {
    setSchemeDiscoveryReturnUrl(this.router.url);
  }

  startOneTimePurchase(): void {
    if (this.paying() || this.redirecting() || this.submitting()) return;
    const current = this.modal();
    if (current !== 'no_membership' && current !== 'plan_choice') return;
    this.beginOneTimeReportCheckout('no_membership');
  }

  /** Start one-time ₹999 scheme discovery checkout (Udyam → referral → payment). */
  beginOneTimeReportCheckout(returnModal: SchemeDiscoveryModal = 'no_membership'): void {
    if (this.paying() || this.redirecting() || this.submitting()) return;
    this.referralReturnModal.set(returnModal);
    this.udyamCheckoutMode.set(true);
    this.draftRequestId.set(null);
    this.udyam.set('');
    this.udyamError.set(null);
    this.resetReferralFields();
    this.openModal('udyam');
  }

  editReferralCode(): void {
    this.referralModalMode.set('entry');
    this.referralValidationState.set('idle');
    this.referredByName.set(null);
  }

  private async openReferralStep(): Promise<void> {
    const action = await resolveReferralCheckoutAction(this.auth, this.referrals);

    if (action.kind === 'skip_modal') {
      await this.completeOneTimePurchase(action.code);
      return;
    }

    this.resetReferralFields();
    this.referralModalMode.set('entry');

    if (action.kind === 'confirm') {
      applyReferralConfirmPrefill(action.code, action.displayName, {
        setCode: (v) => this.referralCodeInput.set(v),
        setName: (v) => this.referredByName.set(v),
        setState: (v) => this.referralValidationState.set(v),
      });
      this.referralModalMode.set('confirm');
    } else {
      const stashed = getRegistrationAdvisorCode();
      if (stashed) {
        this.referralCodeInput.set(stashed);
        void this.runReferralValidation();
      }
    }

    this.openModal('referral');
  }

  cancelReferralStep(): void {
    this.clearReferralTimer();
    this.resetReferralFields();
    this.referralModalMode.set('entry');
    const back = this.referralReturnModal();
    if (back === 'udyam' && this.udyamCheckoutMode()) {
      this.openModal('udyam');
    } else if (back === 'no_membership' || back === 'plan_choice') {
      this.openModal(back);
    } else {
      this.closeAll();
    }
  }

  backFromReferral(): void {
    if (this.paying()) return;
    this.cancelReferralStep();
  }

  onReferralInputChange(value: string): void {
    this.referralCodeInput.set(value ?? '');
    const code = this.referralCodeInput().trim();
    this.referredByName.set(null);

    if (!code) {
      this.clearReferralTimer();
      this.referralValidationSeq++;
      this.referralValidationState.set('idle');
      return;
    }

    this.referralValidationState.set('validating');
    this.clearReferralTimer();
    this.referralDebounceTimer = setTimeout(() => void this.runReferralValidation(), REFERRAL_DEBOUNCE_MS);
  }

  applyReferral(): void {
    this.clearReferralTimer();
    void this.runReferralValidation();
  }

  skipReferralAndPay(): void {
    if (this.referralValidationState() === 'validating' || this.paying()) return;
    void this.completeOneTimePurchase();
  }

  continueReferralAndPay(): void {
    if (!canProceedWithReferralState(this.referralValidationState()) || this.paying()) return;
    const referralToSend = this.referralCodeInput().trim();
    void this.completeOneTimePurchase(referralToSend || undefined);
  }

  goToProfileReports(): void {
    this.closeAll();
    this.router.navigateByUrl('/profile#reports');
  }

  async retryReportGeneration(): Promise<void> {
    const requestId = this.lastFinalizeRequestId();
    if (!requestId || this.paying()) return;
    await this.runReportGeneration(() =>
      firstValueFrom(this.api.finalizeRequest(requestId).pipe(timeout(180000))),
    );
  }

  async emailGeneratedReport(): Promise<void> {
    const reportId = this.generatedReportId();
    if (!reportId || this.reportEmailSending()) return;

    this.reportEmailSending.set(true);
    try {
      const res = await firstValueFrom(this.api.emailReport(reportId).pipe(timeout(30000)));
      if (res?.success) {
        this.toast.success(res.message || 'Report sent to your registered email address.');
        return;
      }
      this.toast.error(res?.message || 'Could not email your report right now.');
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Could not email your report right now.'));
    } finally {
      this.reportEmailSending.set(false);
    }
  }

  viewGeneratedReport(): void {
    this.closeAll();
    void this.router.navigateByUrl('/profile#reports');
  }

  async downloadGeneratedReport(): Promise<void> {
    const reportId = this.generatedReportId();
    if (!reportId || this.reportDownloading()) return;

    this.reportDownloading.set(true);
    try {
      const blob = await firstValueFrom(
        this.reportsApi.downloadReport(reportId).pipe(timeout(120000)),
      );
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = 'Government-Scheme-Discovery-Report.pdf';
      a.click();
      URL.revokeObjectURL(url);
    } catch {
      this.toast.error(
        'Unable to download your report right now. Please try again from your profile or contact support.',
      );
    } finally {
      this.reportDownloading.set(false);
    }
  }

  private routeFinalizeOutcome(outcome: string | null | undefined, message?: string | null): void {
    switch (outcome) {
      case 'generated':
        this.finalizeMessage.set(message || 'Report Generated Successfully');
        this.openModal('report_generated');
        return;
      case 'duplicate':
        this.openModal('report_duplicate');
        return;
      case 'generation_failed':
        this.finalizeMessage.set(
          message ||
            'We were unable to generate the report right now. Please try again after some time.',
        );
        this.openModal('report_failed');
        return;
      default:
        this.finalizeMessage.set(
          message ||
            'We were unable to generate the report right now. Please try again after some time.',
        );
        this.openModal('report_failed');
    }
  }

  async downloadExistingReport(): Promise<void> {
    const reportId = this.status()?.existingReport?.reportId;
    if (!reportId) {
      this.goToProfileReports();
      return;
    }
    this.closeAll();
    this.router.navigateByUrl('/profile#reports');
  }

  async purchaseOneTimeReport(): Promise<void> {
    this.beginOneTimeReportCheckout('no_membership');
  }

  private async completeOneTimePurchase(referralCode?: string): Promise<void> {
    if (this.paying()) return;
    const draftId = this.draftRequestId();
    if (!draftId) {
      this.toast.error('Udyam number must be saved before payment. Please start again.');
      this.openModal('udyam');
      return;
    }

    this.paying.set(true);
    try {
      const paid = await this.runOneTimeCheckout(referralCode);
      this.resetReferralFields();
      if (!paid) return;

      this.openModal('generating_report');
      const res = await firstValueFrom(this.api.finalizeRequest(draftId).pipe(timeout(180000)));
      if (!res?.success) {
        this.toast.error(res?.message || 'Payment received but report generation could not be completed.');
        this.finalizeMessage.set(
          res?.message ||
            'We were unable to generate the report right now. Please try again after some time.',
        );
        this.lastFinalizeRequestId.set(draftId);
        this.openModal('report_failed');
        return;
      }

      this.udyamCheckoutMode.set(false);
      this.draftRequestId.set(null);
      this.udyam.set('');
      this.applyGenerationResult(res, draftId);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Could not complete your report request.'));
    } finally {
      this.paying.set(false);
    }
  }

  private async runReferralValidation(): Promise<boolean> {
    const code = this.referralCodeInput().trim();
    if (!code) {
      this.referralValidationState.set('idle');
      return false;
    }

    const seq = ++this.referralValidationSeq;
    this.referralValidationState.set('validating');
    this.referredByName.set(null);

    const validated = await validateReferralCode(this.referrals, code);
    if (seq !== this.referralValidationSeq) return false;

    if (validated.valid && validated.displayName) {
      this.referredByName.set(validated.displayName);
      this.referralValidationState.set('valid');
      return true;
    }

    if (validated.inactive) {
      this.referralValidationState.set('inactive');
      return true;
    }

    this.referralValidationState.set('invalid');
    return false;
  }

  private clearReferralTimer(): void {
    if (this.referralDebounceTimer) {
      clearTimeout(this.referralDebounceTimer);
      this.referralDebounceTimer = null;
    }
  }

  private resetReferralFields(): void {
    this.clearReferralTimer();
    this.referralValidationSeq++;
    this.referralCodeInput.set('');
    this.referredByName.set(null);
    this.referralValidationState.set('idle');
    this.referralModalMode.set('entry');
  }

  async submitUdyam(): Promise<void> {
    const err = this.validateUdyam(this.udyam());
    this.udyamError.set(err);
    if (err || this.submitting()) return;

    this.submitting.set(true);
    try {
      if (this.udyamCheckoutMode()) {
        const res = await firstValueFrom(this.api.saveDraft(this.udyam().trim()));
        if (!res?.success || !res.requestId) {
          this.toast.error(res?.message || 'Could not save your Udyam number.');
          return;
        }
        this.draftRequestId.set(res.requestId);
        this.referralReturnModal.set('udyam');
        await this.openReferralStep();
        return;
      }

      this.openModal('generating_report');
      const res = await firstValueFrom(this.api.submitRequest(this.udyam().trim()).pipe(timeout(180000)));
      if (!res?.success) {
        this.toast.error(res?.message || 'Could not generate your report.');
        this.finalizeMessage.set(
          res?.message ||
            'We were unable to generate the report right now. Please try again after some time.',
        );
        this.openModal('report_failed');
        return;
      }
      this.udyam.set('');
      this.applyGenerationResult(res);
    } catch (e: unknown) {
      this.finalizeMessage.set(
        'We were unable to generate the report right now. Please try again after some time.',
      );
      this.openModal('report_failed');
      this.toast.error(getHttpErrorMessage(e, API_USER_MESSAGES.schemeDiscovery));
    } finally {
      this.submitting.set(false);
    }
  }

  onUdyamInput(value: string): void {
    this.udyam.set(formatUdyamFromRaw(value));
    this.udyamError.set(null);
  }

  private async continueForLoggedInUser(): Promise<void> {
    this.openModal('loading');
    try {
      const res = await firstValueFrom(this.api.getStatus());
      this.status.set(res);
      if (!res?.success) {
        this.toast.error(res?.message || 'Unable to load scheme discovery status.');
        this.closeAll();
        return;
      }
      await this.routeByPhase(res);
    } catch (e: unknown) {
      this.toast.error(
        getHttpErrorMessage(e, API_USER_MESSAGES.schemeDiscovery, {
          url: '/api/scheme-discovery/status',
          method: 'GET',
        }),
      );
      this.closeAll();
    }
  }

  private async routeByPhase(res: SchemeDiscoveryStatus): Promise<void> {
    switch (res.phase) {
      case 'no_membership':
        this.openModal('no_membership');
        break;
      case 'plan_choice':
        this.openModal('plan_choice');
        break;
      case 'report_exists':
        this.openModal('report_exists');
        break;
      case 'request_pending':
        if (res.pendingRequestId) {
          await this.resumePendingGeneration(res.pendingRequestId);
          break;
        }
        this.openModal('report_failed');
        this.finalizeMessage.set(
          'We were unable to generate the report right now. Please try again after some time.',
        );
        break;
      case 'udyam_form':
        this.udyamCheckoutMode.set(false);
        if (!this.udyamBannerMessage()) {
          if (res.isPremiumOrPro) {
            this.udyamBannerMessage.set(
              'You already have an active membership. Generate your report below.',
            );
          }
        }
        this.openModal('udyam');
        break;
      case 'awaiting_payment':
        this.udyamCheckoutMode.set(true);
        this.draftRequestId.set(res.draftRequestId ?? null);
        if (res.savedUdyam) this.udyam.set(res.savedUdyam);
        this.referralReturnModal.set('udyam');
        this.resetReferralFields();
        this.openModal('referral');
        break;
      default:
        this.toast.error(res.message || 'Unable to continue scheme discovery.');
        this.closeAll();
    }
  }

  private applyGenerationResult(res: SchemeDiscoverySubmitRes, requestId?: string | null): void {
    this.finalizeMessage.set(res.message ?? null);
    this.finalizeExpiryDate.set(res.expiryDate ?? null);
    this.generatedReportId.set(res.reportId ?? null);
    this.lastFinalizeRequestId.set(res.requestId ?? requestId ?? null);
    this.routeFinalizeOutcome(res.outcome, res.message);
  }

  private async resumePendingGeneration(requestId: string): Promise<void> {
    await this.runReportGeneration(() =>
      firstValueFrom(this.api.finalizeRequest(requestId).pipe(timeout(180000))),
    );
  }

  private async runReportGeneration(
    request: () => Promise<SchemeDiscoverySubmitRes | undefined>,
  ): Promise<void> {
    if (this.paying()) return;

    this.paying.set(true);
    this.openModal('generating_report');
    try {
      const res = await request();
      if (!res?.success) {
        this.finalizeMessage.set(
          res?.message ||
            'We were unable to generate the report right now. Please try again after some time.',
        );
        this.openModal('report_failed');
        return;
      }
      this.applyGenerationResult(res);
    } catch (e: unknown) {
      this.finalizeMessage.set(
        'We were unable to generate the report right now. Please try again after some time.',
      );
      this.openModal('report_failed');
      this.toast.error(
        getHttpErrorMessage(e, API_USER_MESSAGES.schemeDiscovery, {
          url: '/api/scheme-discovery/finalize',
          method: 'POST',
        }),
      );
    } finally {
      this.paying.set(false);
    }
  }

  private async runOneTimeCheckout(referralCode?: string): Promise<boolean> {
    const userId = this.session.userId();
    if (!userId) {
      this.toast.warning('Please login to continue.');
      return false;
    }

    const trimmedReferral = (referralCode ?? '').trim();
    let order;
    try {
      order = await firstValueFrom(
        this.payments.createOrder(
          this.payments.buildCreateOrderRequest(
            ONE_TIME_PLAN,
            trimmedReferral || undefined,
          ),
        ),
      );
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Could not start payment.'));
      return false;
    }

    if (!order?.success || !order.razorpayOrderId || !order.keyId || !order.paymentOrderId) {
      this.toast.error(order?.message || 'Could not start payment.');
      return false;
    }

    await this.razorpayLoader.load();

    return await new Promise<boolean>((resolve) => {
      const opts: Record<string, unknown> = {
        key: order.keyId,
        amount: order.amountPaise,
        currency: order.currency,
        name: 'MSME Bharat Manch',
        description: order.planName || 'Government Scheme Discovery Report',
        order_id: order.razorpayOrderId,
        prefill: {
          name: order.prefillName || '',
          email: order.prefillEmail || '',
          contact: order.prefillContact || '',
        },
        theme: { color: '#e63946' },
        modal: {
          ondismiss: () => {
            if (order.paymentOrderId) {
              this.payments.cancelCheckout(order.paymentOrderId).subscribe({ error: () => {} });
            }
            this.toast.info('Payment cancelled.');
            resolve(false);
          },
        },
        handler: async (response: {
          razorpay_payment_id: string;
          razorpay_order_id: string;
          razorpay_signature: string;
        }) => {
          try {
            const verify = await firstValueFrom(
              this.payments.verify({
                paymentOrderId: order.paymentOrderId!,
                razorpayOrderId: response.razorpay_order_id,
                razorpayPaymentId: response.razorpay_payment_id,
                razorpaySignature: response.razorpay_signature,
              }),
            );
            if (verify?.success) {
              resolve(true);
              return;
            }
            this.toast.error(verify?.message || 'Payment verification failed.');
            resolve(false);
          } catch (e: unknown) {
            this.toast.error(getHttpErrorMessage(e, 'Payment verification failed.'));
            resolve(false);
          }
        },
      };
      const RazorpayCtor = (window as unknown as { Razorpay: new (o: Record<string, unknown>) => { open: () => void } }).Razorpay;
      new RazorpayCtor(opts).open();
    });
  }

  private validateUdyam(raw: string): string | null {
    const u = (raw ?? '').trim();
    if (!u) return 'Udyam Registration Number is required.';
    if (!/^UDYAM-[A-Z]{2}-\d{2}-\d{7}$/i.test(u)) {
      return 'Enter a valid Udyam number (e.g. UDYAM-XX-00-0000000).';
    }
    return null;
  }

  private openModal(name: SchemeDiscoveryModal): void {
    this.modal.set(name);
    this.lockBody();
  }

  private lockBody(): void {
    try {
      document.body.style.overflow = 'hidden';
    } catch {
      // ignore
    }
  }

  private unlockBody(): void {
    try {
      document.body.style.overflow = '';
    } catch {
      // ignore
    }
  }
}
