import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthSessionService } from '../../../core/services/auth-session.service';
import { PaymentService, type CreateOrderRes } from '../../../core/services/payment.service';
import { ReferralService } from '../../../core/services/referral.service';
import { RazorpayLoaderService } from '../../../core/services/razorpay-loader.service';
import { ToastService } from '../../../core/services/toast.service';
import { JoinCtaService } from '../../../core/services/join-cta.service';
import { OFFERING_EXPLORER_CARDS } from '../../../data/offering-explorer-cards.data';

const PENDING_PLAN_KEY = 'mbm_pending_plan';
const REFERRAL_DEBOUNCE_MS = 500;
const REFERRAL_INVALID_MSG =
  'Referral code is incorrect. Please check and enter the correct code.';

type PlanCode = 'basic' | 'standard' | 'premium' | 'pro';
type ReferralValidationState = 'idle' | 'validating' | 'valid' | 'invalid';

interface PlanCard {
  code: PlanCode;
  name: string;
  baseInr: number;
  description: string;
}

@Component({
  selector: 'app-membership',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './membership.html',
  styleUrl: './membership.css',
})
export class Membership implements OnInit, OnDestroy {
  readonly offeringExplorerCards = OFFERING_EXPLORER_CARDS;
  private readonly session = inject(AuthSessionService);
  private readonly payments = inject(PaymentService);
  private readonly referrals = inject(ReferralService);
  private readonly razorpayLoader = inject(RazorpayLoaderService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly joinCta = inject(JoinCtaService);

  private debounceTimer: ReturnType<typeof setTimeout> | null = null;
  private validationSeq = 0;

  openFaqIndex: number | null = null;
  readonly busyPlan = signal<string | null>(null);
  readonly checkingPlan = signal(false);

  readonly referralModalOpen = signal(false);
  readonly pendingPlanCode = signal<PlanCode | null>(null);

  readonly referralCodeInput = signal('');
  readonly referredByName = signal<string | null>(null);
  readonly referralValidationState = signal<ReferralValidationState>('idle');

  toggleFaq(index: number) {
    this.openFaqIndex = this.openFaqIndex === index ? null : index;
  }

  ngOnInit(): void {
    if (typeof window === 'undefined') return;
    const pending = window.localStorage.getItem(PENDING_PLAN_KEY);
    if (pending && this.session.isLoggedIn()) {
      window.localStorage.removeItem(PENDING_PLAN_KEY);
      setTimeout(() => void this.choosePlan(pending as PlanCode), 50);
    }
  }

  ngOnDestroy(): void {
    if (this.debounceTimer) clearTimeout(this.debounceTimer);
  }

  async choosePlan(code: PlanCode) {
    if (!this.session.isLoggedIn()) {
      try {
        window.localStorage.setItem(PENDING_PLAN_KEY, code);
      } catch {
        // ignore
      }
      this.toast.info('Please login or register to continue with your selected plan.');
      this.router.navigateByUrl('/login');
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
      this.openReferralModal(code);
    } finally {
      this.checkingPlan.set(false);
    }
  }

  private async checkPlanEligibility(
    code: PlanCode,
  ): Promise<'ok' | 'already_active' | 'unauthorized'> {
    this.session.refreshFromStorage();
    if (!this.session.isLoggedIn()) {
      this.toast.warning('Please login to continue.');
      this.router.navigateByUrl('/login');
      return 'unauthorized';
    }

    try {
      const res = await firstValueFrom(this.payments.myPlan());
      const plan = res?.plan;
      if (!plan || plan.status !== 'Active') return 'ok';
      if (plan.planCode.toLowerCase() !== code.toLowerCase()) return 'ok';
      if (plan.activeTo) {
        const end = new Date(plan.activeTo);
        if (!Number.isNaN(end.getTime()) && end <= new Date()) return 'ok';
      }
      return 'already_active';
    } catch (e: any) {
      if (e?.status === 401) {
        this.handleUnauthorized();
        return 'unauthorized';
      }
      return 'ok';
    }
  }

  openReferralModal(code: PlanCode) {
    this.resetReferralFields();
    this.pendingPlanCode.set(code);
    this.referralModalOpen.set(true);
  }

  closeReferralModal() {
    this.clearReferralInput();
    this.referralModalOpen.set(false);
    this.pendingPlanCode.set(null);
    this.cdr.markForCheck();
  }

  private clearReferralInput() {
    if (this.debounceTimer) {
      clearTimeout(this.debounceTimer);
      this.debounceTimer = null;
    }
    this.validationSeq++;
    this.referralCodeInput.set('');
    this.referredByName.set(null);
    this.referralValidationState.set('idle');
  }

  private resetReferralFields() {
    this.clearReferralInput();
  }

  onReferralModelChange(value: string) {
    this.referralCodeInput.set(value ?? '');
    this.onReferralInputChange();
  }

  onReferralInputChange() {
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

  applyReferral() {
    if (this.debounceTimer) clearTimeout(this.debounceTimer);
    void this.runReferralValidation();
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

    try {
      const res = await firstValueFrom(this.referrals.validate({ referralCode: code }));
      if (seq !== this.validationSeq) return false;

      if (res?.success && res.employeeName) {
        this.referredByName.set(res.employeeName);
        this.referralValidationState.set('valid');
        return true;
      }

      this.referralValidationState.set('invalid');
      return false;
    } catch {
      if (seq !== this.validationSeq) return false;
      this.referralValidationState.set('invalid');
      return false;
    }
  }

  skipReferral() {
    void this.proceedToPayment(undefined);
  }

  continueFromReferral() {
    if (this.referralValidationState() !== 'valid') return;
    const referralToSend = this.referralCodeInput().trim();
    void this.proceedToPayment(referralToSend || undefined);
  }

  private async proceedToPayment(referralCode?: string) {
    const code = this.pendingPlanCode();
    if (!code) return;

    this.referralModalOpen.set(false);
    this.pendingPlanCode.set(null);
    this.resetReferralFields();

    await this.startCheckout(code, referralCode);
  }

  private handleUnauthorized() {
    this.toast.warning('Your session has expired. Please login again.');
    this.session.logout();
    this.closeReferralModal();
    this.router.navigateByUrl('/login');
  }

  private ensureLoggedInForPayment(): boolean {
    this.session.refreshFromStorage();
    if (!this.session.isLoggedIn()) {
      this.toast.warning('Please login to continue.');
      this.router.navigateByUrl('/login');
      return false;
    }
    return true;
  }

  private async startCheckout(code: PlanCode, referralCode?: string) {
    if (!this.ensureLoggedInForPayment()) return;

    const userId = this.session.userId();
    if (!userId) return;

    this.busyPlan.set(code);
    let createdPaymentOrderId: string | null = null;

    try {
      const order = await firstValueFrom(
        this.payments.createOrder({
          planCode: code,
          referralCode: referralCode || undefined,
        }),
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
        this.handleUnauthorized();
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
              void this.joinCta.refresh();
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
              this.router.navigateByUrl('/my-plan');
            } else {
              this.toast.warning(verify?.message || 'Payment received. Activation pending. Please refresh shortly.');
              this.router.navigateByUrl('/my-plan');
            }
          } catch (err: any) {
            const msg =
              (err?.error?.message as string | undefined) ||
              (typeof err?.error === 'string' ? err.error : undefined) ||
              err?.message ||
              'Payment received. Activation pending. Please refresh shortly.';
            this.toast.warning(msg);
            this.router.navigateByUrl('/my-plan');
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

  readonly referralInvalidMessage = REFERRAL_INVALID_MSG;
}
