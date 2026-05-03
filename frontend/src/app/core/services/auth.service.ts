import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

type ApiOk = { success: true; message?: string };

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/auth';

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
}

