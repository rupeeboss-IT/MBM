import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

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
  private readonly base = '/api/payment';

  createOrder(req: CreateOrderReq): Observable<CreateOrderRes> {
    return this.http.post<CreateOrderRes>(`${this.base}/razorpay/order`, req);
  }

  verify(req: VerifyReq): Observable<VerifyRes> {
    return this.http.post<VerifyRes>(`${this.base}/razorpay/verify`, req);
  }

  cancelCheckout(paymentOrderId: string): Observable<unknown> {
    return this.http.post(`${this.base}/razorpay/cancel/${paymentOrderId}`, null);
  }

  myPlan(): Observable<MyPlanRes> {
    return this.http.get<MyPlanRes>(`${this.base}/my-plan`);
  }

  cancelSubscription(): Observable<CancelSubscriptionRes> {
    return this.http.post<CancelSubscriptionRes>(`${this.base}/subscription/cancel`, null);
  }

  paymentHistory(): Observable<PaymentHistoryRes> {
    return this.http.get<PaymentHistoryRes>(`${this.base}/subscription/history`);
  }

  listInvoices(): Observable<InvoiceListRes> {
    return this.http.get<InvoiceListRes>(`${this.base}/invoices`);
  }

  downloadInvoice(paymentId: string): Observable<Blob> {
    return this.http.get(`${this.base}/invoices/${encodeURIComponent(paymentId)}/download`, {
      responseType: 'blob',
    });
  }
}
