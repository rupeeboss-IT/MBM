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
type AdminLoginReq = { identifier: string; password: string };
type AdminLoginRes = { success: boolean; message?: string; userId?: string; role?: string; token?: string };
type ForgotPasswordReq = { email: string };
type ForgotPasswordRes = { success: boolean; message?: string };
type ResetPasswordReq = { email: string; code: string; newPassword: string };
type ResetPasswordRes = { success: boolean; message?: string };
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
  private readonly adminBaseUrl = '/api/admin';

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

  adminLogin(req: AdminLoginReq): Observable<AdminLoginRes> {
    return this.http.post<AdminLoginRes>(`${this.adminBaseUrl}/login`, req);
  }

  adminDashboardCounts(): Observable<any> {
    return this.http.get(`${this.adminBaseUrl}/dashboard/counts`);
  }

  adminListUsers(role?: string): Observable<any> {
    const params: any = {};
    if (role) params.role = role;
    return this.http.get(`${this.adminBaseUrl}/users`, { params });
  }

  adminSetUserActive(userId: string, isActive: boolean): Observable<any> {
    return this.http.patch(`${this.adminBaseUrl}/users/${encodeURIComponent(userId)}/active`, { isActive });
  }

  adminListMembers(role?: string): Observable<any> {
    const params: any = {};
    if (role) params.role = role;
    return this.http.get(`${this.adminBaseUrl}/members`, { params });
  }

  adminSetMemberActive(userId: string, isActive: boolean, reason?: string): Observable<any> {
    return this.http.patch(`${this.adminBaseUrl}/members/${encodeURIComponent(userId)}/active`, { isActive, reason });
  }

  adminCreateUser(req: { fullName: string; email: string; phone: string; password: string }): Observable<any> {
    return this.http.post(`${this.adminBaseUrl}/users`, req);
  }

  adminDeleteUser(userId: string): Observable<any> {
    return this.http.delete(`${this.adminBaseUrl}/users/${encodeURIComponent(userId)}`);
  }

  forgotPassword(email: string): Observable<ForgotPasswordRes> {
    const req: ForgotPasswordReq = { email };
    return this.http.post<ForgotPasswordRes>(`${this.userBaseUrl}/password/forgot`, req);
  }

  resetPassword(email: string, code: string, newPassword: string): Observable<ResetPasswordRes> {
    const req: ResetPasswordReq = { email, code, newPassword };
    return this.http.post<ResetPasswordRes>(`${this.userBaseUrl}/password/reset`, req);
  }

  me(userId: string): Observable<MeRes> {
    return this.http.get<MeRes>(`${this.userBaseUrl}/me`, { params: { userId } });
  }
}

