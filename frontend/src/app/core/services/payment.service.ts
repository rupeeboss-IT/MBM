import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface CreateOrderReq {
  userId: string;
  planCode: string;
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
  userId: string;
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
}

export interface ActivePlan {
  planCode: string;
  planName: string;
  activeFrom: string;
  activeTo?: string | null;
  status: string;
}

export interface MyPlanRes {
  success: boolean;
  message?: string;
  plan?: ActivePlan | null;
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

  cancel(paymentOrderId: string, userId: string): Observable<unknown> {
    return this.http.post(`${this.base}/razorpay/cancel/${paymentOrderId}`, null, {
      params: { userId },
    });
  }

  myPlan(userId: string): Observable<MyPlanRes> {
    return this.http.get<MyPlanRes>(`${this.base}/my-plan`, { params: { userId } });
  }
}
