import { Component, OnInit, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthSessionService } from '../../../core/services/auth-session.service';
import { PaymentService, type CreateOrderRes } from '../../../core/services/payment.service';
import { RazorpayLoaderService } from '../../../core/services/razorpay-loader.service';
import { ToastService } from '../../../core/services/toast.service';

const PENDING_PLAN_KEY = 'mbm_pending_plan';

interface PlanCard {
  code: 'basic' | 'standard' | 'premium' | 'pro';
  name: string;
  baseInr: number;
  description: string;
}

@Component({
  selector: 'app-membership',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './membership.html',
  styleUrl: './membership.css',
})
export class Membership implements OnInit {
  private readonly session = inject(AuthSessionService);
  private readonly payments = inject(PaymentService);
  private readonly razorpayLoader = inject(RazorpayLoaderService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  openFaqIndex: number | null = null;
  /** Plan currently being processed (for spinner / disabling buttons). */
  readonly busyPlan = signal<string | null>(null);

  toggleFaq(index: number) {
    this.openFaqIndex = this.openFaqIndex === index ? null : index;
  }

  ngOnInit(): void {
    if (typeof window === 'undefined') return;
    const pending = window.localStorage.getItem(PENDING_PLAN_KEY);
    if (pending && this.session.isLoggedIn()) {
      window.localStorage.removeItem(PENDING_PLAN_KEY);
      // Defer so view is in place before opening checkout
      setTimeout(() => this.choosePlan(pending as PlanCard['code']), 50);
    }
  }

  choosePlan(code: PlanCard['code']) {
    if (!this.session.isLoggedIn()) {
      try {
        window.localStorage.setItem(PENDING_PLAN_KEY, code);
      } catch {
        // ignore storage errors
      }
      this.toast.info('Please login or register to continue with your selected plan.');
      this.router.navigateByUrl('/login');
      return;
    }
    if (this.busyPlan()) return;
    void this.startCheckout(code);
  }

  private async startCheckout(code: PlanCard['code']) {
    const userId = this.session.userId();
    if (!userId) {
      this.toast.warning('Please login to continue.');
      this.router.navigateByUrl('/login');
      return;
    }

    this.busyPlan.set(code);
    let createdPaymentOrderId: string | null = null;

    try {
      // 1) Create order on backend (server enforces price)
      const order = await firstValueFrom(this.payments.createOrder({ userId, planCode: code }));
      if (!order?.success || !order.razorpayOrderId || !order.keyId || !order.paymentOrderId) {
        this.toast.error(order?.message || 'Could not start payment. Please try again.');
        return;
      }
      createdPaymentOrderId = order.paymentOrderId;

      // 2) Load Razorpay checkout script
      try {
        await this.razorpayLoader.load();
      } catch {
        this.toast.error('Could not load payment gateway. Check your internet and try again.');
        return;
      }

      // 3) Open Razorpay
      await this.openRazorpay(order, userId);
    } catch (e: any) {
      const msg =
        (e?.error?.message as string | undefined) ||
        (typeof e?.error === 'string' ? e.error : undefined) ||
        e?.message ||
        'Could not start payment. Please try again.';
      this.toast.error(msg);
      // Best-effort cancel of the unpaid order
      if (createdPaymentOrderId) {
        this.payments.cancel(createdPaymentOrderId, userId).subscribe({ error: () => {} });
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
            // User closed checkout without paying
            if (order.paymentOrderId) {
              this.payments.cancel(order.paymentOrderId, userId).subscribe({ error: () => {} });
            }
            this.toast.info('Payment cancelled.');
            resolve();
          },
        },
        handler: async (resp: any) => {
          try {
            const verify = await firstValueFrom(
              this.payments.verify({
                userId,
                paymentOrderId: order.paymentOrderId!,
                razorpayOrderId: resp.razorpay_order_id,
                razorpayPaymentId: resp.razorpay_payment_id,
                razorpaySignature: resp.razorpay_signature,
              }),
            );
            if (verify?.success) {
              this.toast.success(`Payment successful. ${order.planName || 'Plan'} activated.`);
              this.router.navigateByUrl('/profile');
            } else {
              this.toast.warning(verify?.message || 'Payment received. Activation pending. Please refresh shortly.');
              this.router.navigateByUrl('/profile');
            }
          } catch (err: any) {
            const msg =
              (err?.error?.message as string | undefined) ||
              (typeof err?.error === 'string' ? err.error : undefined) ||
              err?.message ||
              'Payment received. Activation pending. Please refresh shortly.';
            this.toast.warning(msg);
            this.router.navigateByUrl('/profile');
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
      } catch (e) {
        this.toast.error('Could not open payment window.');
        resolve();
      }
    });
  }
}
