import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

type ApiOk = { success: true; message?: string };
type RegisterReq = {
  role: 'member' | 'partner';
  fullName: string;
  email: string;
  phone: string;
  companyName?: string | null;
  password: string;
  consentAccepted: boolean;
};
type RegisterRes = { success: boolean; message?: string; userId?: string };
type LoginReq = { identifier: string; password: string };
type LoginRes = { success: boolean; message?: string; userId?: string };
export type MeRes = {
  success: boolean;
  message?: string;
  userId?: string;
  role?: string;
  fullName?: string;
  email?: string;
  phone?: string;
  companyName?: string | null;
  emailVerifiedAt?: string | null;
  phoneVerifiedAt?: string | null;
  createdAt?: string | null;
};

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/auth';
  private readonly userBaseUrl = '/api/user';

  sendEmailOtp(email: string): Observable<ApiOk> {
    return this.http.post<ApiOk>(`${this.baseUrl}/otp/email/send`, { email });
  }

  verifyEmailOtp(email: string, code: string): Observable<ApiOk> {
    return this.http.post<ApiOk>(`${this.baseUrl}/otp/email/verify`, { email, code });
  }

  sendSmsOtp(phone: string): Observable<ApiOk> {
    return this.http.post<ApiOk>(`${this.baseUrl}/otp/sms/send`, { phone });
  }

  verifySmsOtp(phone: string, code: string): Observable<ApiOk> {
    return this.http.post<ApiOk>(`${this.baseUrl}/otp/sms/verify`, { phone, code });
  }

  register(req: RegisterReq): Observable<RegisterRes> {
    return this.http.post<RegisterRes>(`${this.userBaseUrl}/register`, req);
  }

  login(req: LoginReq): Observable<LoginRes> {
    return this.http.post<LoginRes>(`${this.userBaseUrl}/login`, req);
  }

  me(userId: string): Observable<MeRes> {
    return this.http.get<MeRes>(`${this.userBaseUrl}/me`, { params: { userId } });
  }
}

