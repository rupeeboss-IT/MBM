import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, EMPTY, Observable } from 'rxjs';
import { apiUrl } from '../utils/api-url';

const CONSENT_KEY = 'mbm_cookie_consent';
const SESSION_TOKEN_KEY = 'mbm_cookie_session_token';

@Injectable({ providedIn: 'root' })
export class CookieConsentService {
  private readonly http = inject(HttpClient);

  hasConsented(): boolean {
    if (typeof window === 'undefined') return true;
    return localStorage.getItem(CONSENT_KEY) === 'accepted';
  }

  accept(): Observable<unknown> {
    const token = this.getOrCreateSessionToken();
    localStorage.setItem(CONSENT_KEY, 'accepted');
    return this.http
      .post(apiUrl('/api/cookie-consent/accept'), { sessionToken: token })
      .pipe(catchError(() => EMPTY));
  }

  private getOrCreateSessionToken(): string {
    let token = localStorage.getItem(SESSION_TOKEN_KEY);
    if (!token) {
      token = typeof crypto !== 'undefined' && crypto.randomUUID
        ? crypto.randomUUID()
        : Math.random().toString(36).slice(2) + Date.now().toString(36);
      localStorage.setItem(SESSION_TOKEN_KEY, token);
    }
    return token;
  }
}
