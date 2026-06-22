import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../utils/api-url';
import { appendAdminListParams, type AdminListQueryOpts } from '../utils/admin-list-params';

type ApiOk = { success: true; message?: string };
type RegisterReq = {
  role: 'member' | 'partner';
  fullName: string;
  email: string;
  phone: string;
  companyName?: string | null;
  password: string;
  consentAccepted: boolean;
  registrationSource?: string | null;
  advisorCode?: string | null;
};
type RegisterRes = { success: boolean; message?: string; userId?: string; role?: string; token?: string };
type LoginReq = { identifier: string; password: string };
type LoginRes = { success: boolean; message?: string; userId?: string; role?: string; token?: string };
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
  memberId?: string | null;
  role?: string;
  fullName?: string;
  email?: string;
  phone?: string;
  companyName?: string | null;
  emailVerifiedAt?: string | null;
  phoneVerifiedAt?: string | null;
  createdAt?: string | null;
  registrationAdvisorCode?: string | null;
  registrationAdvisorLocked?: boolean;
  registrationAdvisorDisplayName?: string | null;
};

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  sendEmailOtp(email: string): Observable<ApiOk> {
    return this.http.post<ApiOk>(apiUrl('/api/auth/otp/email/send'), { email });
  }

  verifyEmailOtp(email: string, code: string): Observable<ApiOk> {
    return this.http.post<ApiOk>(apiUrl('/api/auth/otp/email/verify'), { email, code });
  }

  sendSmsOtp(phone: string): Observable<ApiOk> {
    return this.http.post<ApiOk>(apiUrl('/api/auth/otp/sms/send'), { phone });
  }

  verifySmsOtp(phone: string, code: string): Observable<ApiOk> {
    return this.http.post<ApiOk>(apiUrl('/api/auth/otp/sms/verify'), { phone, code });
  }

  register(req: RegisterReq): Observable<RegisterRes> {
    return this.http.post<RegisterRes>(apiUrl('/api/user/register'), req);
  }

  login(req: LoginReq): Observable<LoginRes> {
    return this.http.post<LoginRes>(apiUrl('/api/user/login'), req);
  }

  adminLogin(req: AdminLoginReq): Observable<AdminLoginRes> {
    return this.http.post<AdminLoginRes>(apiUrl('/api/admin/login'), req);
  }

  adminDashboardCounts(opts?: { dateFrom?: string; dateTo?: string }): Observable<any> {
    const params: Record<string, string> = {};
    if (opts?.dateFrom) params['dateFrom'] = opts.dateFrom;
    if (opts?.dateTo) params['dateTo'] = opts.dateTo;
    return this.http.get(apiUrl('/api/admin/dashboard/counts'), { params });
  }

  adminDashboardDetail(category: string, opts?: AdminListQueryOpts): Observable<any> {
    const params = appendAdminListParams({}, opts);
    return this.http.get(apiUrl(`/api/admin/dashboard/details/${encodeURIComponent(category)}`), { params });
  }

  adminListUsers(opts?: AdminListQueryOpts): Observable<any> {
    const params = appendAdminListParams({}, opts);
    return this.http.get(apiUrl('/api/admin/users'), { params });
  }

  adminSetUserActive(userId: string, isActive: boolean): Observable<any> {
    return this.http.patch(apiUrl(`/api/admin/users/${encodeURIComponent(userId)}/active`), { isActive });
  }

  adminListMembers(opts?: AdminListQueryOpts): Observable<any> {
    const params = appendAdminListParams({}, opts);
    return this.http.get(apiUrl('/api/admin/members'), { params });
  }

  adminSetMemberActive(userId: string, isActive: boolean, reason?: string): Observable<any> {
    return this.http.patch(apiUrl(`/api/admin/members/${encodeURIComponent(userId)}/active`), { isActive, reason });
  }

  adminCreateUser(req: { fullName: string; email: string; phone: string; password: string }): Observable<any> {
    return this.http.post(apiUrl('/api/admin/users'), req);
  }

  adminDeleteUser(userId: string): Observable<any> {
    return this.http.delete(apiUrl(`/api/admin/users/${encodeURIComponent(userId)}`));
  }

  forgotPassword(email: string): Observable<ForgotPasswordRes> {
    const req: ForgotPasswordReq = { email };
    return this.http.post<ForgotPasswordRes>(apiUrl('/api/user/password/forgot'), req);
  }

  resetPassword(email: string, code: string, newPassword: string): Observable<ResetPasswordRes> {
    const req: ResetPasswordReq = { email, code, newPassword };
    return this.http.post<ResetPasswordRes>(apiUrl('/api/user/password/reset'), req);
  }

  me(): Observable<MeRes> {
    return this.http.get<MeRes>(apiUrl('/api/user/me'));
  }
}
