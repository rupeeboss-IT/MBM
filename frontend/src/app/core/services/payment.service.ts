import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../utils/api-url';

export interface CreateOrderReq {
  planCode: string;
  /** Optional; omit or leave empty to use default employee after payment. */
  referralCode?: string | null;
}

export interface CreateOrderRes {
  success: boolean;
  message?: string;
  keyId?: string;
  paymentOrderId?: string;
  razorpayOrderId?: string;
  amountPaise?: number;
  currency?: string;
  planCode?: string;
  planName?: string;
  prefillName?: string;
  prefillEmail?: string;
  prefillContact?: string;
}

export interface VerifyReq {
  paymentOrderId: string;
  razorpayOrderId: string;
  razorpayPaymentId: string;
  razorpaySignature: string;
}

export interface VerifyRes {
  success: boolean;
  message?: string;
  planCode?: string;
  activeFrom?: string;
  activeTo?: string;
  activationKind?: string | null;
  previousPlanName?: string | null;
}

export interface ActivePlan {
  planCode: string;
  planName: string;
  activeFrom: string;
  activeTo?: string | null;
  status: string;
  cancelAtPeriodEnd?: boolean;
  autoRenewEnabled?: boolean;
  daysRemaining?: number | null;
}

export interface MyPlanRes {
  success: boolean;
  message?: string;
  plan?: ActivePlan | null;
}

export interface CancelSubscriptionRes {
  success: boolean;
  message?: string;
  activeTo?: string;
}

export interface PaymentHistoryItem {
  paymentOrderId: string;
  planCode: string;
  planName: string;
  amountPaise: number;
  orderStatus: string;
  paymentStatus?: string | null;
  createdAt: string;
  paidAt?: string | null;
}

export interface PaymentHistoryRes {
  success: boolean;
  message?: string;
  items?: PaymentHistoryItem[];
}

export interface InvoiceListItem {
  paymentId: string;
  invoiceNumber: string;
  planCode: string;
  planName: string;
  amountPaise: number;
  paidAt: string;
  activeTo?: string | null;
}

export interface InvoiceListRes {
  success: boolean;
  message?: string;
  items?: InvoiceListItem[];
}

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private readonly http = inject(HttpClient);

  createOrder(req: CreateOrderReq): Observable<CreateOrderRes> {
    return this.http.post<CreateOrderRes>(apiUrl('/api/payment/razorpay/order'), req);
  }

  /** Attach optional referral code when present. */
  buildCreateOrderRequest(planCode: string, referralCode?: string | null): CreateOrderReq {
    return {
      planCode,
      ...(referralCode?.trim() ? { referralCode: referralCode.trim() } : {}),
    };
  }

  verify(req: VerifyReq): Observable<VerifyRes> {
    return this.http.post<VerifyRes>(apiUrl('/api/payment/razorpay/verify'), req);
  }

  cancelCheckout(paymentOrderId: string): Observable<unknown> {
    return this.http.post(apiUrl(`/api/payment/razorpay/cancel/${paymentOrderId}`), null);
  }

  myPlan(): Observable<MyPlanRes> {
    return this.http.get<MyPlanRes>(apiUrl('/api/payment/my-plan'));
  }

  cancelSubscription(): Observable<CancelSubscriptionRes> {
    return this.http.post<CancelSubscriptionRes>(apiUrl('/api/payment/subscription/cancel'), null);
  }

  paymentHistory(): Observable<PaymentHistoryRes> {
    return this.http.get<PaymentHistoryRes>(apiUrl('/api/payment/subscription/history'));
  }

  listInvoices(): Observable<InvoiceListRes> {
    return this.http.get<InvoiceListRes>(apiUrl('/api/payment/invoices'));
  }

  downloadInvoice(paymentId: string): Observable<Blob> {
    return this.http.get(apiUrl(`/api/payment/invoices/${encodeURIComponent(paymentId)}/download`), {
      responseType: 'blob',
    });
  }
}
