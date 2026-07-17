import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthSessionService } from './auth-session.service';
import { AuthService } from './auth.service';
import { JoinCtaService } from './join-cta.service';
import { PaymentService, type CreateOrderRes } from './payment.service';
import { RazorpayLoaderService } from './razorpay-loader.service';
import { ReferralService } from './referral.service';
import { SchemeDiscoveryFlowService } from './scheme-discovery-flow.service';
import { ToastService } from './toast.service';
import { SessionExpiryService } from './session-expiry.service';
import { hasSchemeDiscoveryIntent } from '../utils/scheme-discovery-intent';
import { shouldResumeSchemeDiscoveryAfterMembership } from '../utils/scheme-discovery-journey.util';
import {
  getMembershipSourceGuidance,
  getPlanConfirmContinueLabel,
  getPlanConfirmMessage,
  shouldConfirmPlanChoice,
  stashMembershipSource,
  type MembershipSource,
} from '../utils/membership-source.util';
import { isApiLocalDateOnOrBeforeNow } from '../utils/parse-api-local-date';
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
import { clearRegistrationMode } from '../utils/registration-mode.util';
import { clearRegistrationAdvisorCode, getRegistrationAdvisorCode } from '../utils/registration-advisor.util';

export const PENDING_MEMBERSHIP_PLAN_KEY = 'mbm_pending_plan';

export type MembershipPlanCode = 'basic' | 'premium' | 'pro';

type ReferralModalMode = 'entry' | 'confirm';

const VALID_PLAN_CODES = new Set<string>(['basic', 'premium', 'pro']);

function isMembershipPlanCode(value: string): value is MembershipPlanCode {
  return VALID_PLAN_CODES.has(value);
}

@Injectable({ providedIn: 'root' })
export class MembershipPlanCheckoutService {
  private readonly session = inject(AuthSessionService);
  private readonly sessionExpiry = inject(SessionExpiryService);
  private readonly auth = inject(AuthService);
  private readonly payments = inject(PaymentService);
  private readonly referrals = inject(ReferralService);
  private readonly razorpayLoader = inject(RazorpayLoaderService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly joinCta = inject(JoinCtaService);
  private readonly schemeDiscovery = inject(SchemeDiscoveryFlowService);

  private debounceTimer: ReturnType<typeof setTimeout> | null = null;
  private validationSeq = 0;

  readonly busyPlan = signal<string | null>(null);
  readonly checkingPlan = signal(false);
  readonly highlightRecommended = signal(false);

  readonly referralModalOpen = signal(false);
  readonly referralModalMode = signal<ReferralModalMode>('entry');
  readonly pendingPlanCode = signal<MembershipPlanCode | null>(null);

  readonly referralCodeInput = signal('');
  readonly referredByName = signal<string | null>(null);
  readonly referralValidationState = signal<ReferralValidationState>('idle');

  readonly membershipSource = signal<MembershipSource | null>(null);
  readonly sourceGuidance = computed(() => getMembershipSourceGuidance(this.membershipSource()));
  readonly basicConfirmOpen = signal(false);
  readonly pendingBasicPlan = signal<MembershipPlanCode | null>(null);

  readonly referralInvalidMessage = REFERRAL_INVALID_MSG;
  readonly referralInactiveMessage = REFERRAL_INACTIVE_MSG;

  setMembershipSource(source: MembershipSource | null): void {
    this.membershipSource.set(source);
  }

  hasPendingMembershipPlan(): boolean {
    if (typeof window === 'undefined') return false;
    const pending = window.localStorage.getItem(PENDING_MEMBERSHIP_PLAN_KEY);
    return !!pending && isMembershipPlanCode(pending);
  }

  /** Resume checkout when a plan was stashed before login/registration. */
  processPendingPlanAfterAuth(): boolean {
    if (typeof window === 'undefined' || !this.session.isLoggedIn()) return false;
    const pending = window.localStorage.getItem(PENDING_MEMBERSHIP_PLAN_KEY);
    if (!pending || !isMembershipPlanCode(pending)) return false;
    window.localStorage.removeItem(PENDING_MEMBERSHIP_PLAN_KEY);
    setTimeout(() => void this.continueChoosePlan(pending), 50);
    return true;
  }

  async choosePlan(
    code: MembershipPlanCode,
    options?: { membershipSource?: MembershipSource | null },
  ): Promise<void> {
    const source = options?.membershipSource ?? this.membershipSource();
    if (source !== this.membershipSource()) {
      this.membershipSource.set(source);
    }

    if (source && shouldConfirmPlanChoice(code, source)) {
      this.pendingBasicPlan.set(code);
      this.basicConfirmOpen.set(true);
      return;
    }

    await this.continueChoosePlan(code, source);
  }

  dismissBasicConfirm(): void {
    this.basicConfirmOpen.set(false);
    this.pendingBasicPlan.set(null);
  }

  async confirmBasicPlan(): Promise<void> {
    const code = this.pendingBasicPlan();
    this.dismissBasicConfirm();
    if (code) await this.continueChoosePlan(code, this.membershipSource());
  }

  confirmPlanMessage(): string {
    const guidance = this.sourceGuidance();
    const code = this.pendingBasicPlan();
    if (!guidance || code !== 'basic') return '';
    return getPlanConfirmMessage(guidance, code);
  }

  confirmPlanContinueLabel(): string {
    const code = this.pendingBasicPlan();
    if (code !== 'basic') return 'Continue';
    return getPlanConfirmContinueLabel(code);
  }

  scrollToRecommendedPlans(): void {
    this.dismissBasicConfirm();
    this.highlightRecommended.set(true);
    if (!this.router.url.startsWith('/membership')) {
      void this.router.navigate(['/membership'], { fragment: 'plan-premium' });
      return;
    }
    document.getElementById('plan-premium')?.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }

  openReferralModal(
    code: MembershipPlanCode,
    options?: { mode?: ReferralModalMode; code?: string; displayName?: string },
  ): void {
    this.resetReferralFields();
    this.referralModalMode.set(options?.mode ?? 'entry');
    this.pendingPlanCode.set(code);

    if (options?.mode === 'confirm' && options.code && options.displayName) {
      applyReferralConfirmPrefill(options.code, options.displayName, {
        setCode: (v) => this.referralCodeInput.set(v),
        setName: (v) => this.referredByName.set(v),
        setState: (v) => this.referralValidationState.set(v),
      });
      this.referralModalOpen.set(true);
      return;
    }

    const stashedAdvisor = getRegistrationAdvisorCode();
    if (stashedAdvisor) {
      this.referralCodeInput.set(stashedAdvisor);
      void this.runReferralValidation();
    }

    this.referralModalOpen.set(true);
  }

  editReferralCode(): void {
    this.referralModalMode.set('entry');
    this.referralValidationState.set('idle');
    this.referredByName.set(null);
  }

  closeReferralModal(): void {
    this.clearReferralInput();
    this.referralModalMode.set('entry');
    this.referralModalOpen.set(false);
    this.pendingPlanCode.set(null);
    this.navigateOffAuthPageAfterCheckoutDismiss();
  }

  /** Avoid leaving users on login/register after they dismiss checkout modals. */
  private navigateOffAuthPageAfterCheckoutDismiss(): void {
    const path = this.router.url.split('?')[0].split('#')[0];
    if (path === '/login' || path === '/register') {
      void this.router.navigateByUrl('/membership');
    }
  }

  onReferralModelChange(value: string): void {
    this.referralCodeInput.set(value ?? '');
    this.onReferralInputChange();
  }

  onReferralInputChange(): void {
    const code = this.referralCodeInput().trim();
    this.referredByName.set(null);

    if (!code) {
      if (this.debounceTimer) clearTimeout(this.debounceTimer);
      this.validationSeq++;
      this.referralValidationState.set('idle');
      return;
    }

    this.referralValidationState.set('validating');
    if (this.debounceTimer) clearTimeout(this.debounceTimer);
    this.debounceTimer = setTimeout(() => void this.runReferralValidation(), REFERRAL_DEBOUNCE_MS);
  }

  applyReferral(): void {
    if (this.debounceTimer) clearTimeout(this.debounceTimer);
    void this.runReferralValidation();
  }

  skipReferral(): void {
    void this.proceedToPayment(undefined);
  }

  continueFromReferral(): void {
    if (!canProceedWithReferralState(this.referralValidationState())) return;
    const referralToSend = this.referralCodeInput().trim();
    void this.proceedToPayment(referralToSend || undefined);
  }

  private async continueChoosePlan(
    code: MembershipPlanCode,
    source: MembershipSource | null = this.membershipSource(),
  ): Promise<void> {
    if (!this.session.isLoggedIn()) {
      clearRegistrationMode();
      try {
        window.localStorage.setItem(PENDING_MEMBERSHIP_PLAN_KEY, code);
      } catch {
        // ignore
      }
      stashMembershipSource(source);
      this.toast.info('Please login or register to continue with your selected plan.');
      void this.router.navigateByUrl('/login');
      return;
    }

    if (this.busyPlan() || this.referralModalOpen() || this.checkingPlan()) return;

    this.checkingPlan.set(true);
    try {
      const eligibility = await this.checkPlanEligibility(code);
      if (eligibility === 'unauthorized') return;
      if (eligibility === 'already_active') {
        this.toast.info('You already have an active subscription for this plan.');
        return;
      }
      await this.beginCheckoutWithReferral(code);
    } finally {
      this.checkingPlan.set(false);
    }
  }

  private async beginCheckoutWithReferral(code: MembershipPlanCode): Promise<void> {
    const action = await resolveReferralCheckoutAction(this.auth, this.referrals);

    if (action.kind === 'skip_modal') {
      await this.startCheckout(code, action.code);
      return;
    }

    if (action.kind === 'confirm') {
      this.openReferralModal(code, {
        mode: 'confirm',
        code: action.code,
        displayName: action.displayName,
      });
      return;
    }

    this.openReferralModal(code);
  }

  private async checkPlanEligibility(
    code: MembershipPlanCode,
  ): Promise<'ok' | 'already_active' | 'unauthorized'> {
    this.session.refreshFromStorage();
    if (!this.session.isLoggedIn()) {
      this.toast.warning('Please login to continue.');
      void this.router.navigateByUrl('/login');
      return 'unauthorized';
    }

    try {
      const res = await firstValueFrom(this.payments.myPlan());
      const plan = res?.plan;
      if (!plan || plan.status !== 'Active') return 'ok';
      if (plan.planCode.toLowerCase() !== code.toLowerCase()) return 'ok';
      if (plan.activeTo && isApiLocalDateOnOrBeforeNow(plan.activeTo)) return 'ok';
      return 'already_active';
    } catch (e: any) {
      if (e?.status === 401) {
        this.sessionExpiry.handleExpiredSession('member');
        this.closeReferralModal();
        return 'unauthorized';
      }
      return 'ok';
    }
  }

  private async proceedToPayment(referralCode?: string): Promise<void> {
    const code = this.pendingPlanCode();
    if (!code) return;

    this.referralModalOpen.set(false);
    this.pendingPlanCode.set(null);
    this.resetReferralFields();

    await this.startCheckout(code, referralCode);
  }

  private ensureLoggedInForPayment(): boolean {
    this.session.refreshFromStorage();
    if (!this.session.isLoggedIn()) {
      this.toast.warning('Please login to continue.');
      void this.router.navigateByUrl('/login');
      return false;
    }
    return true;
  }

  private async startCheckout(code: MembershipPlanCode, referralCode?: string): Promise<void> {
    if (!this.ensureLoggedInForPayment()) return;

    const userId = this.session.userId();
    if (!userId) return;

    this.busyPlan.set(code);
    let createdPaymentOrderId: string | null = null;

    try {
      const order = await firstValueFrom(
        this.payments.createOrder(this.payments.buildCreateOrderRequest(code, referralCode)),
      );
      if (!order?.success || !order.razorpayOrderId || !order.keyId || !order.paymentOrderId) {
        const msg = order?.message || 'Could not start payment. Please try again.';
        if (msg.toLowerCase().includes('already have')) {
          this.toast.info('You already have an active subscription for this plan.');
        } else {
          this.toast.error(msg);
        }
        return;
      }
      createdPaymentOrderId = order.paymentOrderId;

      try {
        await this.razorpayLoader.load();
      } catch {
        this.toast.error('Could not load payment gateway. Check your internet and try again.');
        return;
      }

      await this.openRazorpay(order, userId);
    } catch (e: any) {
      if (e?.status === 401) {
        this.sessionExpiry.handleExpiredSession('member');
        this.closeReferralModal();
        return;
      }
      const msg =
        (e?.error?.message as string | undefined) ||
        (typeof e?.error === 'string' ? e.error : undefined) ||
        e?.message ||
        'Could not start payment. Please try again.';
      if (msg.toLowerCase().includes('already have')) {
        this.toast.info('You already have an active subscription for this plan.');
      } else {
        this.toast.error(msg);
      }
      if (createdPaymentOrderId) {
        this.payments.cancelCheckout(createdPaymentOrderId).subscribe({ error: () => {} });
      }
    } finally {
      this.busyPlan.set(null);
    }
  }

  private openRazorpay(order: CreateOrderRes, userId: string): Promise<void> {
    return new Promise<void>((resolve) => {
      const opts: any = {
        key: order.keyId,
        amount: order.amountPaise,
        currency: order.currency,
        name: 'MSME Bharat Manch',
        description: order.planName ? `Plan: ${order.planName}` : 'Membership',
        order_id: order.razorpayOrderId,
        prefill: {
          name: order.prefillName || '',
          email: order.prefillEmail || '',
          contact: order.prefillContact || '',
        },
        notes: {
          plan_code: order.planCode || '',
          payment_order_id: order.paymentOrderId || '',
        },
        theme: { color: '#e63946' },
        modal: {
          ondismiss: () => {
            if (order.paymentOrderId) {
              this.payments.cancelCheckout(order.paymentOrderId).subscribe({ error: () => {} });
            }
            this.toast.info('Payment cancelled.');
            resolve();
          },
        },
        handler: async (resp: any) => {
          try {
            const verify = await firstValueFrom(
              this.payments.verify({
                paymentOrderId: order.paymentOrderId!,
                razorpayOrderId: resp.razorpay_order_id,
                razorpayPaymentId: resp.razorpay_payment_id,
                razorpaySignature: resp.razorpay_signature,
              }),
            );
            if (verify?.success) {
              clearRegistrationAdvisorCode();
              void this.joinCta.refresh();
              const resumeSchemeDiscovery = shouldResumeSchemeDiscoveryAfterMembership(
                order.planCode,
                hasSchemeDiscoveryIntent(),
                this.membershipSource(),
              );
              if (!resumeSchemeDiscovery) {
                const kind = (verify.activationKind ?? '').toLowerCase();
                if (kind === 'upgrade') {
                  this.toast.success(
                    `Upgraded to ${order.planName || 'your new plan'}. Check your email for the invoice and details.`,
                  );
                } else if (kind === 'renewal') {
                  this.toast.success(
                    `${order.planName || 'Plan'} renewed. Check your email for the invoice and new expiry date.`,
                  );
                } else {
                  this.toast.success(
                    `Payment successful. ${order.planName || 'Plan'} activated. Check your email for the invoice.`,
                  );
                }
              }
              if (resumeSchemeDiscovery) {
                await this.schemeDiscovery.completeMembershipPurchaseAndResume(order.planCode ?? '');
              } else {
                void this.router.navigateByUrl('/my-plan');
              }
            } else {
              this.toast.warning(verify?.message || 'Payment received. Activation pending. Please refresh shortly.');
              void this.router.navigateByUrl('/my-plan');
            }
          } catch (err: any) {
            const msg =
              (err?.error?.message as string | undefined) ||
              (typeof err?.error === 'string' ? err.error : undefined) ||
              err?.message ||
              'Payment received. Activation pending. Please refresh shortly.';
            this.toast.warning(msg);
            void this.router.navigateByUrl('/my-plan');
          } finally {
            resolve();
          }
        },
      };

      try {
        const rzp = new (window as any).Razorpay(opts);
        rzp.on('payment.failed', (resp: any) => {
          const reason = resp?.error?.description || 'Payment failed. Please try again.';
          this.toast.error(reason);
        });
        rzp.open();
      } catch {
        this.toast.error('Could not open payment window.');
        resolve();
      }
    });
  }

  private clearReferralInput(): void {
    if (this.debounceTimer) {
      clearTimeout(this.debounceTimer);
      this.debounceTimer = null;
    }
    this.validationSeq++;
    this.referralCodeInput.set('');
    this.referredByName.set(null);
    this.referralValidationState.set('idle');
  }

  private resetReferralFields(): void {
    this.clearReferralInput();
  }

  private async runReferralValidation(): Promise<boolean> {
    const code = this.referralCodeInput().trim();
    if (!code) {
      this.referralValidationState.set('idle');
      return false;
    }

    const seq = ++this.validationSeq;
    this.referralValidationState.set('validating');
    this.referredByName.set(null);

    const validated = await validateReferralCode(this.referrals, code);
    if (seq !== this.validationSeq) return false;

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
}
